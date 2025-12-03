using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Services;
using System.Globalization;

namespace PumpRoomAutomationBackend.Controllers;

[ApiController]
[Route("api/data/motor-winding-temp")]
[Authorize]
public class MotorWindingTempController : ControllerBase
{
    private readonly IMotorWindingTempService _service;
    private readonly ILogger<MotorWindingTempController> _logger;

    public MotorWindingTempController(IMotorWindingTempService service, ILogger<MotorWindingTempController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<MotorWindingTempQueryResult>>> QueryData(
        [FromQuery] int siteId,
        [FromQuery(Name = "startTime")] string? startTimeStr = null,
        [FromQuery(Name = "endTime")] string? endTimeStr = null,
        [FromQuery] string interval = "minute",
        [FromQuery] int? limit = 10000)
    {
        try
        {
            if (string.IsNullOrEmpty(startTimeStr) || string.IsNullOrEmpty(endTimeStr))
                return BadRequest(ApiResponse<MotorWindingTempQueryResult>.Fail("时间参数不能为空", "MISSING_TIME"));

            if (!DateTime.TryParse(startTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime startTime) ||
                !DateTime.TryParse(endTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime endTime))
                return BadRequest(ApiResponse<MotorWindingTempQueryResult>.Fail("时间格式无效", "INVALID_TIME"));

            if (startTime.Kind != DateTimeKind.Utc) startTime = startTime.ToUniversalTime();
            if (endTime.Kind != DateTimeKind.Utc) endTime = endTime.ToUniversalTime();

            var request = new QueryMotorWindingTempRequest
            {
                SiteId = siteId,
                StartTime = startTime,
                EndTime = endTime,
                Interval = interval,
                Limit = limit
            };

            var result = await _service.QueryDataAsync(request);
            return Ok(ApiResponse<MotorWindingTempQueryResult>.Ok(result, "查询成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<MotorWindingTempQueryResult>.Fail(ex.Message, "INVALID_PARAMETER"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询绕组温度数据失败");
            return StatusCode(500, ApiResponse<MotorWindingTempQueryResult>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }

    [HttpGet("latest")]
    public async Task<ActionResult<ApiResponse<MotorWindingTempDto>>> GetLatestData([FromQuery] int siteId)
    {
        try
        {
            var result = await _service.GetLatestDataAsync(siteId);
            if (result == null)
                return NotFound(ApiResponse<MotorWindingTempDto>.Fail("未找到数据", "NOT_FOUND"));
            return Ok(ApiResponse<MotorWindingTempDto>.Ok(result, "查询成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最新绕组温度数据失败");
            return StatusCode(500, ApiResponse<MotorWindingTempDto>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }
}
