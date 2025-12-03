using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Services;
using System.Globalization;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// 水温数据控制器
/// </summary>
[ApiController]
[Route("api/data/water-temperature")]
[Authorize]
public class WaterTemperatureController : ControllerBase
{
    private readonly IWaterTemperatureService _service;
    private readonly ILogger<WaterTemperatureController> _logger;

    public WaterTemperatureController(IWaterTemperatureService service, ILogger<WaterTemperatureController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// 添加单条水温数据
    /// </summary>
    /// <param name="request">水温数据</param>
    /// <returns>添加的数据</returns>
    [HttpPost]
    public async Task<ActionResult> AddData([FromBody] AddWaterTemperatureRequest request)
    {
        try
        {
            var result = await _service.AddDataAsync(request);
            var dto = new WaterTemperatureDto
            {
                Id = result.Id,
                SiteId = result.SiteId,
                Timestamp = result.Timestamp,
                Temperature = result.Temperature,
                Status = result.Status,
                DataQuality = (short)result.DataQuality
            };
            return Ok(ApiResponse<WaterTemperatureDto>.Ok(dto, "添加成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<WaterTemperatureDto>.Fail(ex.Message, "DUPLICATE_DATA"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加水温数据失败");
            return StatusCode(500, ApiResponse<WaterTemperatureDto>.Fail("添加数据失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 查询水温数据
    /// </summary>
    /// <param name="siteId">站点ID</param>
    /// <param name="startTimeStr">开始时间（ISO8601格式字符串）</param>
    /// <param name="endTimeStr">结束时间（ISO8601格式字符串）</param>
    /// <param name="interval">聚合间隔：minute/hour/day/month，默认minute</param>
    /// <param name="limit">最大返回数量，默认10000</param>
    /// <returns>水温数据</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<WaterTemperatureQueryResult>>> QueryData(
        [FromQuery] int siteId,
        [FromQuery(Name = "startTime")] string? startTimeStr = null,
        [FromQuery(Name = "endTime")] string? endTimeStr = null,
        [FromQuery] string interval = "minute",
        [FromQuery] int? limit = 10000)
    {
        try
        {
            _logger.LogInformation("Controller接收到查询请求: SiteId={SiteId}, StartTime={StartTime}, EndTime={EndTime}", 
                siteId, startTimeStr, endTimeStr);
            
            if (string.IsNullOrEmpty(startTimeStr))
            {
                return BadRequest(ApiResponse<WaterTemperatureQueryResult>.Fail("开始时间不能为空", "MISSING_START_TIME"));
            }
            
            if (string.IsNullOrEmpty(endTimeStr))
            {
                return BadRequest(ApiResponse<WaterTemperatureQueryResult>.Fail("结束时间不能为空", "MISSING_END_TIME"));
            }
            
            // 手动解析DateTime
            if (!DateTime.TryParse(startTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime parsedStartTime))
            {
                return BadRequest(ApiResponse<WaterTemperatureQueryResult>.Fail($"开始时间格式无效: {startTimeStr}", "INVALID_START_TIME"));
            }
            
            if (!DateTime.TryParse(endTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime parsedEndTime))
            {
                return BadRequest(ApiResponse<WaterTemperatureQueryResult>.Fail($"结束时间格式无效: {endTimeStr}", "INVALID_END_TIME"));
            }
            
            _logger.LogInformation("解析后的时间: StartTime={StartTime} (Kind={StartKind}), EndTime={EndTime} (Kind={EndKind})", 
                parsedStartTime, parsedStartTime.Kind, parsedEndTime, parsedEndTime.Kind);
            
            var request = new QueryWaterTemperatureRequest
            {
                SiteId = siteId,
                StartTime = parsedStartTime.Kind == DateTimeKind.Utc ? parsedStartTime : parsedStartTime.ToUniversalTime(),
                EndTime = parsedEndTime.Kind == DateTimeKind.Utc ? parsedEndTime : parsedEndTime.ToUniversalTime(),
                Interval = interval,
                Limit = limit
            };

            var result = await _service.QueryDataAsync(request);
            return Ok(ApiResponse<WaterTemperatureQueryResult>.Ok(result, "查询成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<WaterTemperatureQueryResult>.Fail(ex.Message, "INVALID_PARAMETER"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询水温数据失败");
            return StatusCode(500, ApiResponse<WaterTemperatureQueryResult>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 获取最新水温数据
    /// </summary>
    /// <param name="siteId">站点ID</param>
    /// <returns>最新水温数据</returns>
    [HttpGet("latest")]
    public async Task<ActionResult<ApiResponse<WaterTemperatureDto>>> GetLatestData([FromQuery] int siteId)
    {
        try
        {
            var result = await _service.GetLatestDataAsync(siteId);
            if (result == null)
                return NotFound(ApiResponse<WaterTemperatureDto>.Fail("未找到数据", "NOT_FOUND"));
            return Ok(ApiResponse<WaterTemperatureDto>.Ok(result, "查询成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最新水温数据失败");
            return StatusCode(500, ApiResponse<WaterTemperatureDto>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }
}
