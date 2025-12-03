using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Services;
using System.Globalization;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// 下游液位数据控制器
/// </summary>
[ApiController]
[Route("api/data/downstream-water-level")]
[Authorize]
public class DownstreamWaterLevelController : ControllerBase
{
    private readonly IDownstreamWaterLevelService _service;
    private readonly ILogger<DownstreamWaterLevelController> _logger;

    public DownstreamWaterLevelController(
        IDownstreamWaterLevelService service,
        ILogger<DownstreamWaterLevelController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// 添加单条液位数据
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DownstreamWaterLevelDto>>> AddData(
        [FromBody] AddDownstreamWaterLevelRequest request)
    {
        try
        {
            var result = await _service.AddDataAsync(request);
            var dto = new DownstreamWaterLevelDto
            {
                Id = result.Id,
                SiteId = result.SiteId,
                Timestamp = result.Timestamp,
                WaterLevel = result.WaterLevel,
                Status = result.Status,
                DataQuality = result.DataQuality
            };
            return Ok(ApiResponse<DownstreamWaterLevelDto>.Ok(dto, "添加成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<DownstreamWaterLevelDto>.Fail(ex.Message, "DUPLICATE_DATA"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加下游液位数据失败");
            return StatusCode(500, ApiResponse<DownstreamWaterLevelDto>.Fail("添加数据失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 查询液位数据
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<DownstreamWaterLevelQueryResult>>> QueryData(
        [FromQuery] int siteId,
        [FromQuery(Name = "startTime")] string? startTimeStr = null,
        [FromQuery(Name = "endTime")] string? endTimeStr = null,
        [FromQuery] string interval = "minute",
        [FromQuery] int? limit = 10000)
    {
        try
        {
            // 手动解析时间参数
            if (string.IsNullOrEmpty(startTimeStr))
            {
                return BadRequest(ApiResponse<DownstreamWaterLevelQueryResult>.Fail("开始时间不能为空", "MISSING_START_TIME"));
            }

            if (string.IsNullOrEmpty(endTimeStr))
            {
                return BadRequest(ApiResponse<DownstreamWaterLevelQueryResult>.Fail("结束时间不能为空", "MISSING_END_TIME"));
            }

            _logger.LogInformation("收到查询请求: SiteId={SiteId}, StartTime={StartTime}, EndTime={EndTime}, Interval={Interval}", 
                siteId, startTimeStr, endTimeStr, interval);

            // 使用 RoundtripKind 来保留时区信息
            if (!DateTime.TryParse(startTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime startTime))
            {
                return BadRequest(ApiResponse<DownstreamWaterLevelQueryResult>.Fail($"开始时间格式无效: {startTimeStr}", "INVALID_START_TIME"));
            }

            if (!DateTime.TryParse(endTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime endTime))
            {
                return BadRequest(ApiResponse<DownstreamWaterLevelQueryResult>.Fail($"结束时间格式无效: {endTimeStr}", "INVALID_END_TIME"));
            }

            // 确保转换为 UTC
            if (startTime.Kind != DateTimeKind.Utc)
            {
                startTime = startTime.ToUniversalTime();
            }
            if (endTime.Kind != DateTimeKind.Utc)
            {
                endTime = endTime.ToUniversalTime();
            }

            _logger.LogInformation("解析后的时间: StartTime={StartTime} (UTC), EndTime={EndTime} (UTC)", 
                startTime, endTime);

            var request = new QueryDownstreamWaterLevelRequest
            {
                SiteId = siteId,
                StartTime = startTime,
                EndTime = endTime,
                Interval = interval,
                Limit = limit
            };

            var result = await _service.QueryDataAsync(request);
            return Ok(ApiResponse<DownstreamWaterLevelQueryResult>.Ok(result, "查询成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<DownstreamWaterLevelQueryResult>.Fail(ex.Message, "INVALID_PARAMETER"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询下游液位数据失败: SiteId={SiteId}, Start={Start}, End={End}", 
                siteId, startTimeStr, endTimeStr);
            return StatusCode(500, ApiResponse<DownstreamWaterLevelQueryResult>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 获取指定站点的最新液位数据
    /// </summary>
    [HttpGet("latest")]
    public async Task<ActionResult<ApiResponse<DownstreamWaterLevelDto>>> GetLatestData(
        [FromQuery] int siteId)
    {
        try
        {
            var result = await _service.GetLatestDataAsync(siteId);
            
            if (result == null)
            {
                return NotFound(ApiResponse<DownstreamWaterLevelDto>.Fail("未找到数据", "NOT_FOUND"));
            }

            return Ok(ApiResponse<DownstreamWaterLevelDto>.Ok(result, "查询成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最新下游液位数据失败: SiteId={SiteId}", siteId);
            return StatusCode(500, ApiResponse<DownstreamWaterLevelDto>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }
}

