using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Services;
using System.Globalization;

namespace PumpRoomAutomationBackend.Controllers;

[ApiController]
[Route("api/data/net-weight")]
[Authorize]
public class NetWeightController : ControllerBase
{
    private readonly INetWeightService _service;
    private readonly ILogger<NetWeightController> _logger;

    public NetWeightController(INetWeightService service, ILogger<NetWeightController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<NetWeightQueryResult>>> QueryData(
        [FromQuery] int siteId,
        [FromQuery(Name = "startTime")] string? startTimeStr = null,
        [FromQuery(Name = "endTime")] string? endTimeStr = null,
        [FromQuery] string interval = "minute",
        [FromQuery] int? limit = 10000)
    {
        try
        {
            if (string.IsNullOrEmpty(startTimeStr) || string.IsNullOrEmpty(endTimeStr))
                return BadRequest(ApiResponse<NetWeightQueryResult>.Fail("时间参数不能为空", "MISSING_TIME"));

            if (!DateTime.TryParse(startTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime startTime) ||
                !DateTime.TryParse(endTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime endTime))
                return BadRequest(ApiResponse<NetWeightQueryResult>.Fail("时间格式无效", "INVALID_TIME"));

            if (startTime.Kind != DateTimeKind.Utc) startTime = startTime.ToUniversalTime();
            if (endTime.Kind != DateTimeKind.Utc) endTime = endTime.ToUniversalTime();

            var request = new QueryNetWeightRequest
            {
                SiteId = siteId,
                StartTime = startTime,
                EndTime = endTime,
                Interval = interval,
                Limit = limit
            };

            var result = await _service.QueryDataAsync(request);
            return Ok(ApiResponse<NetWeightQueryResult>.Ok(result, "查询成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<NetWeightQueryResult>.Fail(ex.Message, "INVALID_PARAMETER"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询净重数据失败");
            return StatusCode(500, ApiResponse<NetWeightQueryResult>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }

    [HttpGet("latest")]
    public async Task<ActionResult<ApiResponse<NetWeightDto>>> GetLatestData([FromQuery] int siteId)
    {
        try
        {
            var result = await _service.GetLatestDataAsync(siteId);
            if (result == null)
                return NotFound(ApiResponse<NetWeightDto>.Fail("未找到数据", "NOT_FOUND"));
            return Ok(ApiResponse<NetWeightDto>.Ok(result, "查询成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最新净重数据失败");
            return StatusCode(500, ApiResponse<NetWeightDto>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }
}
