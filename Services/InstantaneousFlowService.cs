using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 瞬时流量数据服务实现
/// </summary>
public class InstantaneousFlowService : IInstantaneousFlowService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InstantaneousFlowService> _logger;

    public InstantaneousFlowService(
        ApplicationDbContext context,
        ILogger<InstantaneousFlowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InstantaneousFlow> AddDataAsync(AddInstantaneousFlowRequest request)
    {
        var entity = new InstantaneousFlow
        {
            SiteId = request.SiteId,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            FlowRate = request.FlowRate,
            Status = request.Status ?? "normal",
            DataQuality = request.DataQuality,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.InstantaneousFlows.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("添加瞬时流量数据: SiteId={SiteId}, Time={Time}, Flow={Flow}", 
                entity.SiteId, entity.Timestamp, entity.FlowRate);

            return entity;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("idx_flow_site_timestamp") == true)
        {
            _logger.LogWarning("重复的流量数据记录: SiteId={SiteId}, Time={Time}", 
                request.SiteId, entity.Timestamp);
            throw new InvalidOperationException("该时间点的数据已存在");
        }
    }

    public async Task<InstantaneousFlowQueryResult> QueryDataAsync(QueryInstantaneousFlowRequest request)
    {
        _logger.LogInformation("查询瞬时流量数据请求: SiteId={SiteId}, StartTime={StartTime}, EndTime={EndTime}, Interval={Interval}", 
            request.SiteId, request.StartTime, request.EndTime, request.Interval);
        
        if (request.EndTime <= request.StartTime)
        {
            throw new ArgumentException($"结束时间必须大于开始时间: Start={request.StartTime:O}, End={request.EndTime:O}");
        }

        var site = await _context.SiteConfigs
            .Where(s => s.Id == request.SiteId)
            .Select(s => new { s.Id, s.SiteName })
            .FirstOrDefaultAsync();

        if (site == null)
        {
            throw new ArgumentException("站点不存在");
        }

        var result = new InstantaneousFlowQueryResult
        {
            SiteId = request.SiteId,
            SiteName = site.SiteName ?? "",
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Interval = request.Interval
        };

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

    private async Task<List<AggregatedFlowDto>> QueryMinuteDataAsync(QueryInstantaneousFlowRequest request)
    {
        var query = _context.InstantaneousFlows
            .Where(f => f.SiteId == request.SiteId 
                && f.Timestamp >= request.StartTime 
                && f.Timestamp < request.EndTime)
            .OrderBy(f => f.Timestamp)
            .Select(f => new AggregatedFlowDto
            {
                TimeBucket = f.Timestamp,
                DataCount = 1,
                AvgFlow = f.FlowRate,
                MinFlow = f.FlowRate,
                MaxFlow = f.FlowRate,
                StdDevFlow = 0
            });

        if (request.Limit.HasValue)
        {
            query = query.Take(request.Limit.Value);
        }

        return await query.ToListAsync();
    }

    private async Task<List<AggregatedFlowDto>> QueryAggregatedDataAsync(
        QueryInstantaneousFlowRequest request, 
        string truncateLevel)
    {
        // 修改查询逻辑：取每个时间桶（整点/整天/整月）最接近的一条原始数据
        var sql = $@"
            WITH nearest_records AS (
                SELECT DISTINCT ON (date_trunc('{truncateLevel}', timestamp))
                    date_trunc('{truncateLevel}', timestamp) as time_bucket,
                    timestamp,
                    flow_rate
                FROM instantaneous_flows
                WHERE site_id = {{0}}
                    AND timestamp >= {{1}}
                    AND timestamp < {{2}}
                ORDER BY date_trunc('{truncateLevel}', timestamp), 
                         ABS(EXTRACT(EPOCH FROM (timestamp - date_trunc('{truncateLevel}', timestamp))))
            )
            SELECT 
                time_bucket,
                1 as data_count,
                flow_rate as avg_flow,
                flow_rate as min_flow,
                flow_rate as max_flow,
                0 as stddev_flow
            FROM nearest_records 
            ORDER BY time_bucket
            {(request.Limit.HasValue ? $"LIMIT {{3}}" : "")}
        ";

        var parameters = request.Limit.HasValue 
            ? new object[] { request.SiteId, request.StartTime, request.EndTime, request.Limit.Value }
            : new object[] { request.SiteId, request.StartTime, request.EndTime };

        var result = new List<AggregatedFlowDto>();

        using (var command = _context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandText = sql;
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = command.CreateParameter();
                param.ParameterName = $"@p{i}";
                param.Value = parameters[i];
                command.Parameters.Add(param);
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                command.CommandText = command.CommandText.Replace($"{{{i}}}", $"@p{i}");
            }

            await _context.Database.OpenConnectionAsync();
            
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Add(new AggregatedFlowDto
                    {
                        TimeBucket = reader.GetDateTime(0),
                        DataCount = Convert.ToInt32(reader.GetInt64(1)),
                        AvgFlow = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                        MinFlow = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                        MaxFlow = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                        StdDevFlow = reader.IsDBNull(5) ? null : (decimal?)Convert.ToDecimal(reader.GetDouble(5))
                    });
                }
            }
        }

        return result;
    }

    public async Task<InstantaneousFlowDto?> GetLatestDataAsync(int siteId)
    {
        var latest = await _context.InstantaneousFlows
            .Where(f => f.SiteId == siteId)
            .OrderByDescending(f => f.Timestamp)
            .FirstOrDefaultAsync();

        return latest != null ? MapToDto(latest) : null;
    }

    private static InstantaneousFlowDto MapToDto(InstantaneousFlow entity)
    {
        return new InstantaneousFlowDto
        {
            Id = entity.Id,
            SiteId = entity.SiteId,
            Timestamp = entity.Timestamp,
            FlowRate = entity.FlowRate,
            Status = entity.Status,
            DataQuality = entity.DataQuality
        };
    }
}

