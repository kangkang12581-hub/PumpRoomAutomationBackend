using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs.Common;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// 通用数据查询控制器 - 支持多种时间范围快捷查询
/// </summary>
[ApiController]
[Route("api/data-query")]
[Authorize]
public class DataQueryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataQueryController> _logger;

    public DataQueryController(ApplicationDbContext context, ILogger<DataQueryController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 快捷查询数据 - 支持预定义时间范围
    /// </summary>
    /// <param name="metric">指标类型: upstream-water-level, downstream-water-level, instantaneous-flow, flow-velocity, water-temperature, net-weight, speed, current, external-temp, internal-temp, external-humidity, internal-humidity</param>
    /// <param name="siteId">站点ID</param>
    /// <param name="timeRange">时间范围: 1h(最近1小时), 6h(最近6小时), 24h(最近24小时), 7d(最近7天), 30d(最近30天)</param>
    /// <param name="startDate">自定义开始日期 (格式: yyyy-MM-dd)，用于自定义查询</param>
    /// <param name="endDate">自定义结束日期 (格式: yyyy-MM-dd)，用于自定义查询</param>
    /// <param name="limit">返回数据条数限制，默认1000</param>
    [HttpGet("{metric}/quick")]
    public async Task<ActionResult> QuickQuery(
        string metric,
        [FromQuery] int siteId,
        [FromQuery] string? timeRange = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] int limit = 1000)
    {
        try
        {
            var (tableName, columnName) = GetTableAndColumn(metric);
            if (string.IsNullOrEmpty(tableName))
            {
                return BadRequest(new { success = false, message = "不支持的指标类型", metric });
            }

            DateTime startTime, endTime;

            // 处理自定义日期范围
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                if (!DateTime.TryParse(startDate, out var start) || !DateTime.TryParse(endDate, out var end))
                {
                    return BadRequest(new { success = false, message = "日期格式无效，请使用 yyyy-MM-dd 格式" });
                }
                startTime = new DateTime(start.Year, start.Month, start.Day, 0, 0, 0, DateTimeKind.Utc);
                endTime = new DateTime(end.Year, end.Month, end.Day, 23, 59, 59, DateTimeKind.Utc);
            }
            // 处理预定义时间范围
            else if (!string.IsNullOrEmpty(timeRange))
            {
                var now = DateTime.UtcNow;
                // 将结束时间设置为下一分钟，确保包含当前分钟的数据
                endTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc).AddMinutes(1);

                startTime = timeRange.ToLower() switch
                {
                    "1h" => now.AddHours(-1),
                    "6h" => now.AddHours(-6),
                    "24h" => now.AddHours(-24),
                    "7d" => now.AddDays(-7),
                    "30d" => now.AddDays(-30),
                    _ => throw new ArgumentException($"不支持的时间范围: {timeRange}")
                };
                
                _logger.LogInformation("查询时间范围: Metric={Metric}, SiteId={SiteId}, StartTime={StartTime}, EndTime={EndTime}, TimeRange={TimeRange}",
                    metric, siteId, startTime, endTime, timeRange);
            }
            else
            {
                return BadRequest(new { success = false, message = "请提供 timeRange 或 startDate/endDate 参数" });
            }

            var sql = $@"
                SELECT 
                    timestamp,
                    {columnName} as value,
                    status,
                    data_quality
                FROM {tableName}
                WHERE site_id = @p0
                    AND timestamp >= @p1
                    AND timestamp <= @p2
                ORDER BY timestamp DESC
                LIMIT @p3
            ";

            var dataList = new List<object>();

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sql;
                
                var param1 = command.CreateParameter();
                param1.ParameterName = "@p0";
                param1.Value = siteId;
                command.Parameters.Add(param1);
                
                var param2 = command.CreateParameter();
                param2.ParameterName = "@p1";
                param2.Value = startTime;
                param2.DbType = System.Data.DbType.DateTime;
                command.Parameters.Add(param2);
                
                var param3 = command.CreateParameter();
                param3.ParameterName = "@p2";
                param3.Value = endTime;
                param3.DbType = System.Data.DbType.DateTime;
                command.Parameters.Add(param3);
                
                var param4 = command.CreateParameter();
                param4.ParameterName = "@p3";
                param4.Value = limit;
                command.Parameters.Add(param4);

                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await _context.Database.OpenConnectionAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        dataList.Add(new
                        {
                            timestamp = reader.GetDateTime(0),
                            value = reader.GetDecimal(1),
                            status = reader.IsDBNull(2) ? null : reader.GetString(2),
                            dataQuality = reader.IsDBNull(3) ? 100 : reader.GetInt32(3)
                        });
                    }
                }
            }

            _logger.LogInformation("查询结果: Metric={Metric}, SiteId={SiteId}, 找到 {Count} 条数据", 
                metric, siteId, dataList.Count);

            return Ok(new
            {
                success = true,
                data = new
                {
                    siteId,
                    metric,
                    startTime,
                    endTime,
                    timeRange,
                    dataCount = dataList.Count,
                    records = dataList
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "快捷查询失败: Metric={Metric}, SiteId={SiteId}, TimeRange={TimeRange}",
                metric, siteId, timeRange);
            return StatusCode(500, new { success = false, message = "查询失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 批量查询多个指标的最新数据
    /// </summary>
    /// <param name="siteId">站点ID</param>
    [HttpGet("latest-all")]
    public async Task<ActionResult> GetAllLatestData([FromQuery] int siteId)
    {
        try
        {
            var metrics = new[]
            {
                ("upstream-water-level", "upstream_water_levels", "water_level"),
                ("downstream-water-level", "downstream_water_levels", "water_level"),
                ("instantaneous-flow", "instantaneous_flows", "flow_rate"),
                ("flow-velocity", "flow_velocities", "velocity"),
                ("water-temperature", "water_temperatures", "temperature"),
                ("net-weight", "netweights", "netweight"),
                ("speed", "speeds", "speed"),
                ("current", "currents", "current"),
                ("external-temp", "externaltemps", "externaltemp"),
                ("internal-temp", "internaltemps", "internaltemp"),
                ("external-humidity", "externalhumiditys", "externalhumidity"),
                ("internal-humidity", "internalhumiditys", "internalhumidity")
            };

            var result = new Dictionary<string, object>();

            foreach (var (metricName, tableName, columnName) in metrics)
            {
                var sql = $@"
                    SELECT timestamp, {columnName} as value
                    FROM {tableName}
                    WHERE site_id = @p0
                    ORDER BY timestamp DESC
                    LIMIT 1
                ";

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    var param = command.CreateParameter();
                    param.ParameterName = "@p0";
                    param.Value = siteId;
                    command.Parameters.Add(param);

                    if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                        await _context.Database.OpenConnectionAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result[metricName] = new
                            {
                                timestamp = reader.GetDateTime(0),
                                value = reader.GetDecimal(1)
                            };
                        }
                        else
                        {
                            result[metricName] = null!;
                        }
                    }
                }
            }

            return Ok(new { success = true, siteId, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取最新数据失败: SiteId={SiteId}", siteId);
            return StatusCode(500, new { success = false, message = "查询失败", error = ex.Message });
        }
    }

    private (string tableName, string columnName) GetTableAndColumn(string metric)
    {
        return metric.ToLower() switch
        {
            "upstream-water-level" => ("upstream_water_levels", "water_level"),
            "downstream-water-level" => ("downstream_water_levels", "water_level"),
            "instantaneous-flow" => ("instantaneous_flows", "flow_rate"),
            "flow-velocity" => ("flow_velocities", "velocity"),
            "water-temperature" => ("water_temperatures", "temperature"),
            "net-weight" => ("netweights", "netweight"),
            "speed" => ("speeds", "speed"),
            "current" => ("currents", "current"),
            "external-temp" => ("externaltemps", "externaltemp"),
            "internal-temp" => ("internaltemps", "internaltemp"),
            "external-humidity" => ("externalhumiditys", "externalhumidity"),
            "internal-humidity" => ("internalhumiditys", "internalhumidity"),
            _ => ("", "")
        };
    }
}

