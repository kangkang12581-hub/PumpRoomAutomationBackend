using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Services;
using System.Globalization;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// 瞬时流量数据控制器
/// </summary>
[ApiController]
[Route("api/data/instantaneous-flow")]
[Authorize]
public class InstantaneousFlowController : ControllerBase
{
    private readonly IInstantaneousFlowService _service;
    private readonly ILogger<InstantaneousFlowController> _logger;

    public InstantaneousFlowController(
        IInstantaneousFlowService service,
        ILogger<InstantaneousFlowController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InstantaneousFlowDto>>> AddData(
        [FromBody] AddInstantaneousFlowRequest request)
    {
        try
        {
            var result = await _service.AddDataAsync(request);
            var dto = new InstantaneousFlowDto
            {
                Id = result.Id,
                SiteId = result.SiteId,
                Timestamp = result.Timestamp,
                FlowRate = result.FlowRate,
                Status = result.Status,
                DataQuality = result.DataQuality
            };
            return Ok(ApiResponse<InstantaneousFlowDto>.Ok(dto, "添加成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<InstantaneousFlowDto>.Fail(ex.Message, "DUPLICATE_DATA"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加瞬时流量数据失败");
            return StatusCode(500, ApiResponse<InstantaneousFlowDto>.Fail("添加数据失败", "INTERNAL_ERROR"));
        }
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<InstantaneousFlowQueryResult>>> QueryData(
        [FromQuery] int siteId,
        [FromQuery(Name = "startTime")] string? startTimeStr = null,
        [FromQuery(Name = "endTime")] string? endTimeStr = null,
        [FromQuery] string interval = "minute",
        [FromQuery] int? limit = 10000)
    {
        try
        {
            if (string.IsNullOrEmpty(startTimeStr))
            {
                return BadRequest(ApiResponse<InstantaneousFlowQueryResult>.Fail("开始时间不能为空", "MISSING_START_TIME"));
            }

            if (string.IsNullOrEmpty(endTimeStr))
            {
                return BadRequest(ApiResponse<InstantaneousFlowQueryResult>.Fail("结束时间不能为空", "MISSING_END_TIME"));
            }

            if (!DateTime.TryParse(startTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime startTime))
            {
                return BadRequest(ApiResponse<InstantaneousFlowQueryResult>.Fail($"开始时间格式无效: {startTimeStr}", "INVALID_START_TIME"));
            }

            if (!DateTime.TryParse(endTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime endTime))
            {
                return BadRequest(ApiResponse<InstantaneousFlowQueryResult>.Fail($"结束时间格式无效: {endTimeStr}", "INVALID_END_TIME"));
            }

            if (startTime.Kind != DateTimeKind.Utc)
            {
                startTime = startTime.ToUniversalTime();
            }
            if (endTime.Kind != DateTimeKind.Utc)
            {
                endTime = endTime.ToUniversalTime();
            }

            var request = new QueryInstantaneousFlowRequest
            {
                SiteId = siteId,
                StartTime = startTime,
                EndTime = endTime,
                Interval = interval,
                Limit = limit
            };

            var result = await _service.QueryDataAsync(request);
            return Ok(ApiResponse<InstantaneousFlowQueryResult>.Ok(result, "查询成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<InstantaneousFlowQueryResult>.Fail(ex.Message, "INVALID_PARAMETER"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询瞬时流量数据失败: SiteId={SiteId}", siteId);
            return StatusCode(500, ApiResponse<InstantaneousFlowQueryResult>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }

    [HttpGet("latest")]
    public async Task<ActionResult<ApiResponse<InstantaneousFlowDto>>> GetLatestData(
        [FromQuery] int siteId)
    {
        try
        {
            var result = await _service.GetLatestDataAsync(siteId);
            
            if (result == null)
            {
                return NotFound(ApiResponse<InstantaneousFlowDto>.Fail("未找到数据", "NOT_FOUND"));
            }

            return Ok(ApiResponse<InstantaneousFlowDto>.Ok(result, "查询成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最新瞬时流量数据失败: SiteId={SiteId}", siteId);
            return StatusCode(500, ApiResponse<InstantaneousFlowDto>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }
}

