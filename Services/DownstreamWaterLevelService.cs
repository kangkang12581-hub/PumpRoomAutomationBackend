using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 下游液位数据服务实现
/// </summary>
public class DownstreamWaterLevelService : IDownstreamWaterLevelService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DownstreamWaterLevelService> _logger;

    public DownstreamWaterLevelService(
        ApplicationDbContext context,
        ILogger<DownstreamWaterLevelService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 添加单条液位数据
    /// </summary>
    public async Task<DownstreamWaterLevel> AddDataAsync(AddDownstreamWaterLevelRequest request)
    {
        var entity = new DownstreamWaterLevel
        {
            SiteId = request.SiteId,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            WaterLevel = request.WaterLevel,
            Status = request.Status ?? "normal",
            DataQuality = request.DataQuality,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.DownstreamWaterLevels.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("添加下游液位数据: SiteId={SiteId}, Time={Time}, Level={Level}", 
                entity.SiteId, entity.Timestamp, entity.WaterLevel);

            return entity;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("idx_downstream_site_timestamp") == true)
        {
            _logger.LogWarning("重复的下游液位数据记录: SiteId={SiteId}, Time={Time}", 
                request.SiteId, entity.Timestamp);
            throw new InvalidOperationException("该时间点的数据已存在");
        }
    }

    /// <summary>
    /// 查询液位数据（支持聚合）
    /// </summary>
    public async Task<DownstreamWaterLevelQueryResult> QueryDataAsync(QueryDownstreamWaterLevelRequest request)
    {
        // 验证时间范围
        _logger.LogInformation("查询下游液位数据请求: SiteId={SiteId}, StartTime={StartTime} (UTC), EndTime={EndTime} (UTC), Interval={Interval}", 
            request.SiteId, request.StartTime, request.EndTime, request.Interval);
        
        if (request.EndTime <= request.StartTime)
        {
            _logger.LogWarning("时间范围无效: StartTime={StartTime}, EndTime={EndTime}", 
                request.StartTime, request.EndTime);
            throw new ArgumentException($"结束时间必须大于开始时间: Start={request.StartTime:O}, End={request.EndTime:O}");
        }
        
        _logger.LogInformation("时间范围验证通过，开始查询下游液位数据");

        // 获取站点信息
        var site = await _context.SiteConfigs
            .Where(s => s.Id == request.SiteId)
            .Select(s => new { s.Id, s.SiteName })
            .FirstOrDefaultAsync();

        if (site == null)
        {
            throw new ArgumentException("站点不存在");
        }

        var result = new DownstreamWaterLevelQueryResult
        {
            SiteId = request.SiteId,
            SiteName = site.SiteName ?? "",
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Interval = request.Interval
        };

        // 根据间隔类型执行聚合查询
        var aggregatedData = request.Interval.ToLower() switch
        {
            "minute" => await QueryMinuteDataAsync(request),
            "hour" => await QueryAggregatedDataAsync(request, "hour"),
            "day" => await QueryAggregatedDataAsync(request, "day"),
            "month" => await QueryAggregatedDataAsync(request, "month"),
            _ => throw new ArgumentException($"不支持的聚合间隔: {request.Interval}")
        };

        result.Data = aggregatedData;
        result.TotalCount = aggregatedData.Count;

        return result;
    }

    /// <summary>
    /// 查询分钟级原始数据
    /// </summary>
    private async Task<List<AggregatedDownstreamWaterLevelDto>> QueryMinuteDataAsync(QueryDownstreamWaterLevelRequest request)
    {
        var query = _context.DownstreamWaterLevels
            .Where(w => w.SiteId == request.SiteId 
                && w.Timestamp >= request.StartTime 
                && w.Timestamp < request.EndTime)
            .OrderBy(w => w.Timestamp)
            .Select(w => new AggregatedDownstreamWaterLevelDto
            {
                TimeBucket = w.Timestamp,
                DataCount = 1,
                AvgLevel = w.WaterLevel,
                MinLevel = w.WaterLevel,
                MaxLevel = w.WaterLevel,
                StdDevLevel = 0
            });

        if (request.Limit.HasValue)
        {
            query = query.Take(request.Limit.Value);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// 查询聚合数据（小时/天/月）
    /// </summary>
    private async Task<List<AggregatedDownstreamWaterLevelDto>> QueryAggregatedDataAsync(
        QueryDownstreamWaterLevelRequest request, 
        string truncateLevel)
    {
        // 修改查询逻辑：取每个时间桶（整点/整天/整月）最接近的一条原始数据
        var sql = $@"
            WITH nearest_records AS (
                SELECT DISTINCT ON (date_trunc('{truncateLevel}', timestamp))
                    date_trunc('{truncateLevel}', timestamp) as time_bucket,
                    timestamp,
                    water_level
                FROM downstream_water_levels
                WHERE site_id = {{0}}
                    AND timestamp >= {{1}}
                    AND timestamp < {{2}}
                ORDER BY date_trunc('{truncateLevel}', timestamp), 
                         ABS(EXTRACT(EPOCH FROM (timestamp - date_trunc('{truncateLevel}', timestamp))))
            )
            SELECT 
                time_bucket,
                1 as data_count,
                water_level as avg_level,
                water_level as min_level,
                water_level as max_level,
                0 as stddev_level
            FROM nearest_records 
            ORDER BY time_bucket
            {(request.Limit.HasValue ? $"LIMIT {{3}}" : "")}
        ";

        var parameters = request.Limit.HasValue 
            ? new object[] { request.SiteId, request.StartTime, request.EndTime, request.Limit.Value }
            : new object[] { request.SiteId, request.StartTime, request.EndTime };

        var result = new List<AggregatedDownstreamWaterLevelDto>();

        using (var command = _context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = sql;
            
            // 添加参数
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = command.CreateParameter();
                param.ParameterName = $"@p{i}";
                param.Value = parameters[i];
                command.Parameters.Add(param);
            }

            // 替换占位符
            for (int i = 0; i < parameters.Length; i++)
            {
                command.CommandText = command.CommandText.Replace($"{{{i}}}", $"@p{i}");
            }

            await _context.Database.OpenConnectionAsync();
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Add(new AggregatedDownstreamWaterLevelDto
                    {
                        TimeBucket = reader.GetDateTime(0),
                        DataCount = Convert.ToInt32(reader.GetInt64(1)),
                        AvgLevel = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                        MinLevel = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                        MaxLevel = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                        StdDevLevel = reader.IsDBNull(5) ? null : (decimal?)Convert.ToDecimal(reader.GetDouble(5))
                    });
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 获取最新的液位数据
    /// </summary>
    public async Task<DownstreamWaterLevelDto?> GetLatestDataAsync(int siteId)
    {
        var latest = await _context.DownstreamWaterLevels
            .Where(w => w.SiteId == siteId)
            .OrderByDescending(w => w.Timestamp)
            .FirstOrDefaultAsync();

        return latest != null ? MapToDto(latest) : null;
    }

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    private static DownstreamWaterLevelDto MapToDto(DownstreamWaterLevel entity)
    {
        return new DownstreamWaterLevelDto
        {
            Id = entity.Id,
            SiteId = entity.SiteId,
            Timestamp = entity.Timestamp,
            WaterLevel = entity.WaterLevel,
            Status = entity.Status,
            DataQuality = entity.DataQuality
        };
    }
}

