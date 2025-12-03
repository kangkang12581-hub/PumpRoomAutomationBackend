using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 上游液位数据服务实现
/// </summary>
public class UpstreamWaterLevelService : IUpstreamWaterLevelService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UpstreamWaterLevelService> _logger;

    public UpstreamWaterLevelService(
        ApplicationDbContext context,
        ILogger<UpstreamWaterLevelService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 添加单条液位数据
    /// </summary>
    public async Task<UpstreamWaterLevelDto> AddDataAsync(AddUpstreamWaterLevelRequest request)
    {
        var entity = new UpstreamWaterLevel
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
            _context.UpstreamWaterLevels.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("添加上游液位数据: SiteId={SiteId}, Time={Time}, Level={Level}", 
                entity.SiteId, entity.Timestamp, entity.WaterLevel);

            return MapToDto(entity);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("uq_upstream_site_timestamp") == true)
        {
            _logger.LogWarning("重复的液位数据记录: SiteId={SiteId}, Time={Time}", 
                request.SiteId, entity.Timestamp);
            throw new InvalidOperationException("该时间点的数据已存在");
        }
    }

    /// <summary>
    /// 批量添加液位数据（高性能）
    /// </summary>
    public async Task<int> BatchAddDataAsync(BatchAddUpstreamWaterLevelRequest request)
    {
        if (request.Data == null || !request.Data.Any())
        {
            return 0;
        }

        var entities = request.Data.Select(r => new UpstreamWaterLevel
        {
            SiteId = r.SiteId,
            Timestamp = r.Timestamp ?? DateTime.UtcNow,
            WaterLevel = r.WaterLevel,
            Status = r.Status ?? "normal",
            DataQuality = r.DataQuality,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        try
        {
            await _context.UpstreamWaterLevels.AddRangeAsync(entities);
            var count = await _context.SaveChangesAsync();

            _logger.LogInformation("批量添加上游液位数据: 成功 {Count} 条", count);
            return count;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "批量添加液位数据失败");
            throw new InvalidOperationException("批量添加数据失败，可能存在重复数据");
        }
    }

    /// <summary>
    /// 查询液位数据（支持聚合）
    /// </summary>
    public async Task<UpstreamWaterLevelQueryResult> QueryDataAsync(QueryUpstreamWaterLevelRequest request)
    {
        // 验证时间范围
        _logger.LogInformation("查询液位数据请求: SiteId={SiteId}, StartTime={StartTime} (UTC), EndTime={EndTime} (UTC), Interval={Interval}", 
            request.SiteId, request.StartTime, request.EndTime, request.Interval);
        
        _logger.LogInformation("时间比较: StartTime.Ticks={StartTicks}, EndTime.Ticks={EndTicks}, Diff={Diff}", 
            request.StartTime.Ticks, request.EndTime.Ticks, (request.EndTime - request.StartTime).TotalSeconds);
        
        if (request.EndTime <= request.StartTime)
        {
            _logger.LogWarning("时间范围无效: StartTime={StartTime}, EndTime={EndTime}, 差值={Diff}秒", 
                request.StartTime, request.EndTime, (request.EndTime - request.StartTime).TotalSeconds);
            throw new ArgumentException($"结束时间必须大于开始时间: Start={request.StartTime:O}, End={request.EndTime:O}");
        }
        
        _logger.LogInformation("时间范围验证通过，开始查询数据");

        // 获取站点信息
        var site = await _context.SiteConfigs
            .Where(s => s.Id == request.SiteId)
            .Select(s => new { s.Id, s.SiteName })
            .FirstOrDefaultAsync();

        if (site == null)
        {
            throw new ArgumentException("站点不存在");
        }

        var result = new UpstreamWaterLevelQueryResult
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
    private async Task<List<AggregatedWaterLevelDto>> QueryMinuteDataAsync(QueryUpstreamWaterLevelRequest request)
    {
        var query = _context.UpstreamWaterLevels
            .Where(w => w.SiteId == request.SiteId 
                && w.Timestamp >= request.StartTime 
                && w.Timestamp < request.EndTime)
            .OrderBy(w => w.Timestamp)
            .Select(w => new AggregatedWaterLevelDto
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
    private async Task<List<AggregatedWaterLevelDto>> QueryAggregatedDataAsync(
        QueryUpstreamWaterLevelRequest request, 
        string truncateLevel)
    {
        // 修改查询逻辑：取每个时间桶（整点/整天/整月）最接近的一条原始数据
        var sql = $@"
            WITH nearest_records AS (
                SELECT DISTINCT ON (date_trunc('{truncateLevel}', timestamp))
                    date_trunc('{truncateLevel}', timestamp) as time_bucket,
                    timestamp,
                    water_level
                FROM upstream_water_levels
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

        var result = new List<AggregatedWaterLevelDto>();

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
                    result.Add(new AggregatedWaterLevelDto
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
    public async Task<UpstreamWaterLevelDto?> GetLatestDataAsync(int siteId)
    {
        var latest = await _context.UpstreamWaterLevels
            .Where(w => w.SiteId == siteId)
            .OrderByDescending(w => w.Timestamp)
            .FirstOrDefaultAsync();

        return latest != null ? MapToDto(latest) : null;
    }

    /// <summary>
    /// 删除指定时间之前的数据（用于数据清理）
    /// </summary>
    public async Task<int> DeleteDataBeforeAsync(DateTime beforeDate)
    {
        var toDelete = _context.UpstreamWaterLevels
            .Where(w => w.Timestamp < beforeDate);

        var count = await toDelete.CountAsync();
        _context.UpstreamWaterLevels.RemoveRange(toDelete);
        await _context.SaveChangesAsync();

        _logger.LogInformation("删除 {Count} 条过期液位数据（早于 {Date}）", count, beforeDate);
        return count;
    }

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    private static UpstreamWaterLevelDto MapToDto(UpstreamWaterLevel entity)
    {
        return new UpstreamWaterLevelDto
        {
            Id = entity.Id,
            SiteId = entity.SiteId,
            Timestamp = entity.Timestamp,
            WaterLevel = entity.WaterLevel,
            Status = entity.Status,
            DataQuality = entity.DataQuality
        };
    }

    /// <summary>
    /// 获取时间间隔字符串
    /// </summary>
    private static string GetIntervalString(string truncateLevel)
    {
        return truncateLevel.ToLower() switch
        {
            "hour" => "1 hour",
            "day" => "1 day",
            "month" => "1 month",
            _ => "1 minute"
        };
    }
}

