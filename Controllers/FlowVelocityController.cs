using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Services;
using System.Globalization;

namespace PumpRoomAutomationBackend.Controllers;

[ApiController]
[Route("api/data/flow-velocity")]
[Authorize]
public class FlowVelocityController : ControllerBase
{
    private readonly IFlowVelocityService _service;
    private readonly ILogger<FlowVelocityController> _logger;

    public FlowVelocityController(IFlowVelocityService service, ILogger<FlowVelocityController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<FlowVelocityQueryResult>>> QueryData(
        [FromQuery] int siteId,
        [FromQuery(Name = "startTime")] string? startTimeStr = null,
        [FromQuery(Name = "endTime")] string? endTimeStr = null,
        [FromQuery] string interval = "minute",
        [FromQuery] int? limit = 10000)
    {
        try
        {
            if (string.IsNullOrEmpty(startTimeStr) || string.IsNullOrEmpty(endTimeStr))
                return BadRequest(ApiResponse<FlowVelocityQueryResult>.Fail("时间参数不能为空", "MISSING_TIME"));

            if (!DateTime.TryParse(startTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime startTime) ||
                !DateTime.TryParse(endTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime endTime))
                return BadRequest(ApiResponse<FlowVelocityQueryResult>.Fail("时间格式无效", "INVALID_TIME"));

            if (startTime.Kind != DateTimeKind.Utc) startTime = startTime.ToUniversalTime();
            if (endTime.Kind != DateTimeKind.Utc) endTime = endTime.ToUniversalTime();

            var request = new QueryFlowVelocityRequest
            {
                SiteId = siteId,
                StartTime = startTime,
                EndTime = endTime,
                Interval = interval,
                Limit = limit
            };

            var result = await _service.QueryDataAsync(request);
            return Ok(ApiResponse<FlowVelocityQueryResult>.Ok(result, "查询成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<FlowVelocityQueryResult>.Fail(ex.Message, "INVALID_PARAMETER"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询流速数据失败");
            return StatusCode(500, ApiResponse<FlowVelocityQueryResult>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }

    [HttpGet("latest")]
    public async Task<ActionResult<ApiResponse<FlowVelocityDto>>> GetLatestData([FromQuery] int siteId)
    {
        try
        {
            var result = await _service.GetLatestDataAsync(siteId);
            if (result == null)
                return NotFound(ApiResponse<FlowVelocityDto>.Fail("未找到数据", "NOT_FOUND"));
            return Ok(ApiResponse<FlowVelocityDto>.Ok(result, "查询成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最新流速数据失败");
            return StatusCode(500, ApiResponse<FlowVelocityDto>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }
}
