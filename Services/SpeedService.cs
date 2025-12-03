using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

public class SpeedService : ISpeedService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SpeedService> _logger;

    public SpeedService(ApplicationDbContext context, ILogger<SpeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Speed> AddDataAsync(AddSpeedRequest request)
    {
        var entity = new Speed
        {
            SiteId = request.SiteId,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            SpeedValue = request.Speed,
            Status = request.Status ?? "normal",
            DataQuality = request.DataQuality,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Speeds.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("uq_speed_site_timestamp") == true)
        {
            throw new InvalidOperationException("该时间点的数据已存在");
        }
    }

    public async Task<SpeedQueryResult> QueryDataAsync(QuerySpeedRequest request)
    {
        _logger.LogInformation("🔍 查询速度数据: SiteId={SiteId}, StartTime={StartTime}, EndTime={EndTime}, Interval={Interval}", 
            request.SiteId, request.StartTime, request.EndTime, request.Interval);

        if (request.EndTime <= request.StartTime)
            throw new ArgumentException("结束时间必须大于开始时间");

        var site = await _context.SiteConfigs.Where(s => s.Id == request.SiteId)
            .Select(s => new { s.Id, s.SiteName }).FirstOrDefaultAsync();
        if (site == null) throw new ArgumentException("站点不存在");

        var result = new SpeedQueryResult
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

        _logger.LogInformation("✅ 速度查询结果: 返回 {Count} 条聚合数据", aggregatedData.Count);

        result.Data = aggregatedData;
        result.TotalCount = aggregatedData.Count;
        return result;
    }

    private async Task<List<AggregatedSpeedDto>> QueryMinuteDataAsync(QuerySpeedRequest request)
    {
        var query = _context.Speeds
            .Where(f => f.SiteId == request.SiteId && f.Timestamp >= request.StartTime && f.Timestamp < request.EndTime)
            .OrderBy(f => f.Timestamp)
            .Select(f => new AggregatedSpeedDto
            {
                TimeBucket = f.Timestamp,
                DataCount = 1,
                AvgSpeed = f.SpeedValue,
                MinSpeed = f.SpeedValue,
                MaxSpeed = f.SpeedValue,
                StdDevSpeed = 0
            });

        if (request.Limit.HasValue) query = query.Take(request.Limit.Value);
        return await query.ToListAsync();
    }

    private async Task<List<AggregatedSpeedDto>> QueryAggregatedDataAsync(QuerySpeedRequest request, string truncateLevel)
    {
        // 修改查询逻辑：取每个时间桶（整点/整天/整月）最接近的一条原始数据
        var sql = $@"
            WITH time_buckets AS (
                SELECT DISTINCT date_trunc('{truncateLevel}', timestamp) as bucket
                FROM speeds 
                WHERE site_id = {{0}} AND timestamp >= {{1}} AND timestamp < {{2}}
            ),
            nearest_records AS (
                SELECT DISTINCT ON (date_trunc('{truncateLevel}', s.timestamp))
                    date_trunc('{truncateLevel}', s.timestamp) as time_bucket,
                    s.timestamp,
                    s.speed
                FROM speeds s
                WHERE s.site_id = {{0}} AND s.timestamp >= {{1}} AND s.timestamp < {{2}}
                ORDER BY date_trunc('{truncateLevel}', s.timestamp), 
                         ABS(EXTRACT(EPOCH FROM (s.timestamp - date_trunc('{truncateLevel}', s.timestamp))))
            )
            SELECT 
                time_bucket,
                1 as data_count,
                speed as avg_speed,
                speed as min_speed,
                speed as max_speed,
                0 as stddev_speed
            FROM nearest_records 
            ORDER BY time_bucket
            {(request.Limit.HasValue ? $"LIMIT {{3}}" : "")}";

        var parameters = request.Limit.HasValue 
            ? new object[] { request.SiteId, request.StartTime, request.EndTime, request.Limit.Value }
            : new object[] { request.SiteId, request.StartTime, request.EndTime };

        var result = new List<AggregatedSpeedDto>();
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
                    result.Add(new AggregatedSpeedDto
                    {
                        TimeBucket = reader.GetDateTime(0),
                        DataCount = Convert.ToInt32(reader.GetInt64(1)),
                        AvgSpeed = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                        MinSpeed = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                        MaxSpeed = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                        StdDevSpeed = reader.IsDBNull(5) ? null : (decimal?)Convert.ToDecimal(reader.GetDouble(5))
                    });
                }
            }
        }
        return result;
    }

    public async Task<SpeedDto?> GetLatestDataAsync(int siteId)
    {
        var latest = await _context.Speeds.Where(f => f.SiteId == siteId)
            .OrderByDescending(f => f.Timestamp).FirstOrDefaultAsync();
        return latest != null ? new SpeedDto
        {
            Id = latest.Id,
            SiteId = latest.SiteId,
            Timestamp = latest.Timestamp,
            Speed = latest.SpeedValue,
            Status = latest.Status,
            DataQuality = latest.DataQuality
        } : null;
    }

    public async Task<SpeedStatisticsDto> GetStatisticsAsync(int siteId, DateTime startTime, DateTime endTime)
    {
        var query = _context.Speeds
            .Where(s => s.SiteId == siteId && s.Timestamp >= startTime && s.Timestamp < endTime);

        var stats = await query
            .GroupBy(s => s.SiteId)
            .Select(g => new SpeedStatisticsDto
            {
                MinValue = g.Min(s => s.SpeedValue),
                MaxValue = g.Max(s => s.SpeedValue),
                AvgValue = g.Average(s => s.SpeedValue),
                DataCount = g.Count()
            })
            .FirstOrDefaultAsync();

        return stats ?? new SpeedStatisticsDto { MinValue = 0, MaxValue = 0, AvgValue = 0, DataCount = 0 };
    }
}

