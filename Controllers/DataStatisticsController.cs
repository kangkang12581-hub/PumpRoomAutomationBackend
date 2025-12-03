using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;

namespace PumpRoomAutomationBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataStatisticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataStatisticsController> _logger;

    public DataStatisticsController(ApplicationDbContext context, ILogger<DataStatisticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取指定指标的统计信息（最小值、最大值、平均值）
    /// </summary>
    /// <param name="metric">指标名称: upstream-water-level, downstream-water-level, instantaneous-flow, flow-velocity, water-temperature, net-weight, speed, current, external-temp, internal-temp, external-humidity, internal-humidity</param>
    /// <param name="siteId">站点ID</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    [HttpGet("{metric}")]
    public async Task<ActionResult> GetStatistics(
        string metric,
        [FromQuery] int siteId,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime)
    {
        try
        {
            var (tableName, columnName) = GetTableAndColumn(metric);
            if (string.IsNullOrEmpty(tableName))
            {
                return BadRequest(new { message = "不支持的指标类型", metric });
            }

            var sql = $@"
                SELECT 
                    COALESCE(MIN({columnName}), 0) as min_value,
                    COALESCE(MAX({columnName}), 0) as max_value,
                    COALESCE(AVG({columnName}), 0) as avg_value,
                    COUNT(*) as data_count
                FROM {tableName}
                WHERE site_id = @p0
                    AND timestamp >= @p1
                    AND timestamp < @p2
            ";

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
                command.Parameters.Add(param2);
                
                var param3 = command.CreateParameter();
                param3.ParameterName = "@p2";
                param3.Value = endTime;
                command.Parameters.Add(param3);

                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await _context.Database.OpenConnectionAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var result = new
                        {
                            minValue = Convert.ToDecimal(reader[0]),
                            maxValue = Convert.ToDecimal(reader[1]),
                            avgValue = Convert.ToDecimal(reader[2]),
                            dataCount = Convert.ToInt32(reader.GetInt64(3))
                        };
                        return Ok(result);
                    }
                }
            }

            return Ok(new { minValue = 0, maxValue = 0, avgValue = 0, dataCount = 0 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取{Metric}统计信息失败: SiteId={SiteId}, StartTime={StartTime}, EndTime={EndTime}",
                metric, siteId, startTime, endTime);
            return StatusCode(500, new { message = "获取统计信息失败", error = ex.Message });
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

