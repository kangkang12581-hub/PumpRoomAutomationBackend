using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

public class FlowVelocityService : IFlowVelocityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FlowVelocityService> _logger;

    public FlowVelocityService(ApplicationDbContext context, ILogger<FlowVelocityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FlowVelocity> AddDataAsync(AddFlowVelocityRequest request)
    {
        var entity = new FlowVelocity
        {
            SiteId = request.SiteId,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            Velocity = request.Velocity,
            Status = request.Status ?? "normal",
            DataQuality = request.DataQuality,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.FlowVelocities.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("idx_velocity_site_timestamp") == true)
        {
            throw new InvalidOperationException("该时间点的数据已存在");
        }
    }

    public async Task<FlowVelocityQueryResult> QueryDataAsync(QueryFlowVelocityRequest request)
    {
        if (request.EndTime <= request.StartTime)
            throw new ArgumentException("结束时间必须大于开始时间");

        var site = await _context.SiteConfigs.Where(s => s.Id == request.SiteId)
            .Select(s => new { s.Id, s.SiteName }).FirstOrDefaultAsync();
        if (site == null) throw new ArgumentException("站点不存在");

        var result = new FlowVelocityQueryResult
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

    private async Task<List<AggregatedVelocityDto>> QueryMinuteDataAsync(QueryFlowVelocityRequest request)
    {
        var query = _context.FlowVelocities
            .Where(f => f.SiteId == request.SiteId && f.Timestamp >= request.StartTime && f.Timestamp < request.EndTime)
            .OrderBy(f => f.Timestamp)
            .Select(f => new AggregatedVelocityDto
            {
                TimeBucket = f.Timestamp,
                DataCount = 1,
                AvgVelocity = f.Velocity,
                MinVelocity = f.Velocity,
                MaxVelocity = f.Velocity,
                StdDevVelocity = 0
            });

        if (request.Limit.HasValue) query = query.Take(request.Limit.Value);
        return await query.ToListAsync();
    }

    private async Task<List<AggregatedVelocityDto>> QueryAggregatedDataAsync(QueryFlowVelocityRequest request, string truncateLevel)
    {
        // 修改查询逻辑：取每个时间桶（整点/整天/整月）最接近的一条原始数据
        var sql = $@"
            WITH nearest_records AS (
                SELECT DISTINCT ON (date_trunc('{truncateLevel}', timestamp))
                    date_trunc('{truncateLevel}', timestamp) as time_bucket,
                    timestamp,
                    velocity
                FROM flow_velocities
                WHERE site_id = {{0}} AND timestamp >= {{1}} AND timestamp < {{2}}
                ORDER BY date_trunc('{truncateLevel}', timestamp), 
                         ABS(EXTRACT(EPOCH FROM (timestamp - date_trunc('{truncateLevel}', timestamp))))
            )
            SELECT 
                time_bucket,
                1 as data_count,
                velocity as avg_velocity,
                velocity as min_velocity,
                velocity as max_velocity,
                0 as stddev_velocity
            FROM nearest_records 
            ORDER BY time_bucket
            {(request.Limit.HasValue ? $"LIMIT {{3}}" : "")}";

        var parameters = request.Limit.HasValue 
            ? new object[] { request.SiteId, request.StartTime, request.EndTime, request.Limit.Value }
            : new object[] { request.SiteId, request.StartTime, request.EndTime };

        var result = new List<AggregatedVelocityDto>();
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
                command.CommandText = command.CommandText.Replace($"{{{i}}}", $"@p{i}");

            await _context.Database.OpenConnectionAsync();
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Add(new AggregatedVelocityDto
                    {
                        TimeBucket = reader.GetDateTime(0),
                        DataCount = Convert.ToInt32(reader.GetInt64(1)),
                        AvgVelocity = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                        MinVelocity = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                        MaxVelocity = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                        StdDevVelocity = reader.IsDBNull(5) ? null : (decimal?)Convert.ToDecimal(reader.GetDouble(5))
                    });
                }
            }
        }
        return result;
    }

    public async Task<FlowVelocityDto?> GetLatestDataAsync(int siteId)
    {
        var latest = await _context.FlowVelocities.Where(f => f.SiteId == siteId)
            .OrderByDescending(f => f.Timestamp).FirstOrDefaultAsync();
        return latest != null ? new FlowVelocityDto
        {
            Id = latest.Id,
            SiteId = latest.SiteId,
            Timestamp = latest.Timestamp,
            Velocity = latest.Velocity,
            Status = latest.Status,
            DataQuality = latest.DataQuality
        } : null;
    }
}
