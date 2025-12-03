using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

public class InternalHumidityService : IInternalHumidityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InternalHumidityService> _logger;

    public InternalHumidityService(ApplicationDbContext context, ILogger<InternalHumidityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InternalHumidity> AddDataAsync(AddInternalHumidityRequest request)
    {
        var entity = new InternalHumidity
        {
            SiteId = request.SiteId,
            Timestamp = request.Timestamp ?? DateTime.UtcNow,
            InternalHumidityValue = request.Value,
            Status = request.Status ?? "normal",
            DataQuality = request.DataQuality,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _context.InternalHumiditys.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("uq_internalhumidity_site_timestamp") == true)
        {
            throw new InvalidOperationException("该时间点的数据已存在");
        }
    }

    public async Task<InternalHumidityQueryResult> QueryDataAsync(QueryInternalHumidityRequest request)
    {
        _logger.LogInformation("🔍 查询柜内湿度数据: SiteId={SiteId}, StartTime={StartTime}, EndTime={EndTime}, Interval={Interval}", 
            request.SiteId, request.StartTime, request.EndTime, request.Interval);

        if (request.EndTime <= request.StartTime)
            throw new ArgumentException("结束时间必须大于开始时间");

        var site = await _context.SiteConfigs.Where(s => s.Id == request.SiteId)
            .Select(s => new { s.Id, s.SiteName }).FirstOrDefaultAsync();
        if (site == null) throw new ArgumentException("站点不存在");

        var result = new InternalHumidityQueryResult
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

        _logger.LogInformation("✅ 柜内湿度查询结果: 返回 {Count} 条聚合数据", aggregatedData.Count);

        result.Data = aggregatedData;
        result.TotalCount = aggregatedData.Count;
        return result;
    }

    private async Task<List<AggregatedInternalHumidityDto>> QueryMinuteDataAsync(QueryInternalHumidityRequest request)
    {
        var query = _context.InternalHumiditys
            .Where(f => f.SiteId == request.SiteId && f.Timestamp >= request.StartTime && f.Timestamp < request.EndTime)
            .OrderBy(f => f.Timestamp)
            .Select(f => new AggregatedInternalHumidityDto
            {
                TimeBucket = f.Timestamp,
                DataCount = 1,
                AvgValue = f.InternalHumidityValue,
                MinValue = f.InternalHumidityValue,
                MaxValue = f.InternalHumidityValue,
                StdDevValue = 0
            });

        if (request.Limit.HasValue) query = query.Take(request.Limit.Value);
        return await query.ToListAsync();
    }

    private async Task<List<AggregatedInternalHumidityDto>> QueryAggregatedDataAsync(QueryInternalHumidityRequest request, string truncateLevel)
    {
        // 修改查询逻辑：取每个时间桶（整点/整天/整月）最接近的一条原始数据
        var sql = $@"
            WITH nearest_records AS (
                SELECT DISTINCT ON (date_trunc('{truncateLevel}', timestamp))
                    date_trunc('{truncateLevel}', timestamp) as time_bucket,
                    timestamp,
                    internalhumidity as value
                FROM internalhumiditys
                WHERE site_id = {{0}} AND timestamp >= {{1}} AND timestamp < {{2}}
                ORDER BY date_trunc('{truncateLevel}', timestamp), 
                         ABS(EXTRACT(EPOCH FROM (timestamp - date_trunc('{truncateLevel}', timestamp))))
            )
            SELECT 
                time_bucket,
                1 as data_count,
                value as avg_value,
                value as min_value,
                value as max_value,
                0 as stddev_value
            FROM nearest_records 
            ORDER BY time_bucket
            {(request.Limit.HasValue ? $"LIMIT {{3}}" : "")}";

        var parameters = request.Limit.HasValue 
            ? new object[] { request.SiteId, request.StartTime, request.EndTime, request.Limit.Value }
            : new object[] { request.SiteId, request.StartTime, request.EndTime };

        var result = new List<AggregatedInternalHumidityDto>();
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
                    result.Add(new AggregatedInternalHumidityDto
                    {
                        TimeBucket = reader.GetDateTime(0),
                        DataCount = Convert.ToInt32(reader.GetInt64(1)),
                        AvgValue = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                        MinValue = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                        MaxValue = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4),
                        StdDevValue = reader.IsDBNull(5) ? null : (decimal?)Convert.ToDecimal(reader.GetDouble(5))
                    });
                }
            }
        }
        return result;
    }

    public async Task<InternalHumidityDto?> GetLatestDataAsync(int siteId)
    {
        var latest = await _context.InternalHumiditys.Where(f => f.SiteId == siteId)
            .OrderByDescending(f => f.Timestamp).FirstOrDefaultAsync();
        return latest != null ? new InternalHumidityDto
        {
            Id = latest.Id,
            SiteId = latest.SiteId,
            Timestamp = latest.Timestamp,
            Value = latest.InternalHumidityValue,
            Status = latest.Status,
            DataQuality = latest.DataQuality
        } : null;
    }
}

