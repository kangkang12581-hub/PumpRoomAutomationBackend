using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Services;
using System.Globalization;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// 柜内温度数据控制器
/// </summary>
[ApiController]
[Route("api/data/internal-temp")]
[Authorize]
public class InternalTempController : ControllerBase
{
    private readonly IInternalTempService _service;
    private readonly ILogger<InternalTempController> _logger;

    public InternalTempController(IInternalTempService service, ILogger<InternalTempController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// 查询柜内温度数据
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<InternalTempQueryResult>>> QueryData(
        [FromQuery] int siteId,
        [FromQuery(Name = "startTime")] string? startTimeStr = null,
        [FromQuery(Name = "endTime")] string? endTimeStr = null,
        [FromQuery] string interval = "minute",
        [FromQuery] int? limit = 10000)
    {
        try
        {
            if (string.IsNullOrEmpty(startTimeStr))
                return BadRequest(ApiResponse<InternalTempQueryResult>.Fail("开始时间不能为空", "MISSING_START_TIME"));
            
            if (string.IsNullOrEmpty(endTimeStr))
                return BadRequest(ApiResponse<InternalTempQueryResult>.Fail("结束时间不能为空", "MISSING_END_TIME"));
            
            if (!DateTime.TryParse(startTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime parsedStartTime))
                return BadRequest(ApiResponse<InternalTempQueryResult>.Fail($"开始时间格式无效: {startTimeStr}", "INVALID_START_TIME"));
            
            if (!DateTime.TryParse(endTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime parsedEndTime))
                return BadRequest(ApiResponse<InternalTempQueryResult>.Fail($"结束时间格式无效: {endTimeStr}", "INVALID_END_TIME"));
            
            var request = new QueryInternalTempRequest
            {
                SiteId = siteId,
                StartTime = parsedStartTime.Kind == DateTimeKind.Utc ? parsedStartTime : parsedStartTime.ToUniversalTime(),
                EndTime = parsedEndTime.Kind == DateTimeKind.Utc ? parsedEndTime : parsedEndTime.ToUniversalTime(),
                Interval = interval,
                Limit = limit
            };

            var result = await _service.QueryDataAsync(request);
            return Ok(ApiResponse<InternalTempQueryResult>.Ok(result, "查询成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<InternalTempQueryResult>.Fail(ex.Message, "INVALID_PARAMETER"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询柜内温度数据失败");
            return StatusCode(500, ApiResponse<InternalTempQueryResult>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 获取最新柜内温度数据
    /// </summary>
    [HttpGet("latest")]
    public async Task<ActionResult<ApiResponse<InternalTempDto>>> GetLatestData([FromQuery] int siteId)
    {
        try
        {
            var result = await _service.GetLatestDataAsync(siteId);
            if (result == null)
                return NotFound(ApiResponse<InternalTempDto>.Fail("未找到数据", "NOT_FOUND"));
            return Ok(ApiResponse<InternalTempDto>.Ok(result, "查询成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最新柜内温度数据失败");
            return StatusCode(500, ApiResponse<InternalTempDto>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }
}
