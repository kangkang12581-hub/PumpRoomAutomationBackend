using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PumpRoomAutomationBackend.DTOs;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.Models.Enums;
using PumpRoomAutomationBackend.Services;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// 报警记录控制器
/// </summary>
[ApiController]
[Route("api/alarm-records")]
[Authorize]
public class AlarmRecordController : ControllerBase
{
    private readonly IAlarmRecordService _alarmRecordService;
    private readonly ILogger<AlarmRecordController> _logger;

    public AlarmRecordController(
        IAlarmRecordService alarmRecordService,
        ILogger<AlarmRecordController> logger)
    {
        _alarmRecordService = alarmRecordService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有报警记录
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AlarmRecordDto>>>> GetAll()
    {
        try
        {
            var records = await _alarmRecordService.GetAllAsync();
            return Ok(ApiResponse<List<AlarmRecordDto>>.Ok(records, $"获取报警记录成功，共 {records.Count} 条"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取报警记录失败");
            return StatusCode(500, ApiResponse<List<AlarmRecordDto>>.Fail("获取报警记录失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 分页获取报警记录
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedAlarmRecordsResponse>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? siteId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? severity = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var queryParams = new AlarmRecordQueryParams
            {
                Page = page,
                PageSize = pageSize,
                SiteId = siteId,
                Status = !string.IsNullOrEmpty(status) && Enum.TryParse<AlarmStatus>(status, true, out var statusEnum) 
                    ? statusEnum 
                    : null,
                Severity = !string.IsNullOrEmpty(severity) && Enum.TryParse<AlarmSeverity>(severity, true, out var severityEnum) 
                    ? severityEnum 
                    : null,
                StartTime = startTime,
                EndTime = endTime,
                Search = search
            };

            var result = await _alarmRecordService.GetPagedAsync(queryParams);
            return Ok(ApiResponse<PagedAlarmRecordsResponse>.Ok(result, "获取报警记录成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取分页报警记录失败");
            return StatusCode(500, ApiResponse<PagedAlarmRecordsResponse>.Fail("获取报警记录失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 根据ID获取报警记录
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AlarmRecordDto>>> GetById(int id)
    {
        try
        {
            var record = await _alarmRecordService.GetByIdAsync(id);
            if (record == null)
            {
                return NotFound(ApiResponse<AlarmRecordDto>.Fail("报警记录不存在", "NOT_FOUND"));
            }

            return Ok(ApiResponse<AlarmRecordDto>.Ok(record, "获取报警记录成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取报警记录失败: ID={Id}", id);
            return StatusCode(500, ApiResponse<AlarmRecordDto>.Fail("获取报警记录失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 根据站点ID获取报警记录
    /// </summary>
    [HttpGet("site/{siteId}")]
    public async Task<ActionResult<ApiResponse<List<AlarmRecordDto>>>> GetBySiteId(int siteId)
    {
        try
        {
            var records = await _alarmRecordService.GetBySiteIdAsync(siteId);
            return Ok(ApiResponse<List<AlarmRecordDto>>.Ok(records, $"获取站点 {siteId} 的报警记录成功，共 {records.Count} 条"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取站点报警记录失败: SiteId={SiteId}", siteId);
            return StatusCode(500, ApiResponse<List<AlarmRecordDto>>.Fail("获取报警记录失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 确认报警
    /// </summary>
    [HttpPost("{id}/acknowledge")]
    [Authorize(Roles = "ROOT,ADMIN,OPERATOR")]
    public async Task<ActionResult<ApiResponse<bool>>> Acknowledge(int id, [FromBody] AcknowledgeAlarmRequest request)
    {
        try
        {
            var userInfo = HttpContext.User.Identity?.Name ?? "System";
            var success = await _alarmRecordService.AcknowledgeAsync(id, request.AcknowledgedBy);
            
            if (!success)
            {
                return NotFound(ApiResponse<bool>.Fail("报警记录不存在", "NOT_FOUND"));
            }

            _logger.LogInformation("✅ 报警记录已确认: ID={Id}, User={User}", id, request.AcknowledgedBy);
            return Ok(ApiResponse<bool>.Ok(true, "报警确认成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 确认报警失败: ID={Id}", id);
            return StatusCode(500, ApiResponse<bool>.Fail("确认报警失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 清除报警
    /// </summary>
    [HttpPost("{id}/clear")]
    [Authorize(Roles = "ROOT,ADMIN,OPERATOR")]
    public async Task<ActionResult<ApiResponse<bool>>> Clear(int id)
    {
        try
        {
            var success = await _alarmRecordService.ClearAsync(id);
            
            if (!success)
            {
                return NotFound(ApiResponse<bool>.Fail("报警记录不存在", "NOT_FOUND"));
            }

            _logger.LogInformation("✅ 报警记录已清除: ID={Id}", id);
            return Ok(ApiResponse<bool>.Ok(true, "报警清除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 清除报警失败: ID={Id}", id);
            return StatusCode(500, ApiResponse<bool>.Fail("清除报警失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 清除站点所有活跃报警
    /// </summary>
    [HttpPost("site/{siteId}/clear")]
    [Authorize(Roles = "ROOT,ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> ClearBySiteId(int siteId)
    {
        try
        {
            var success = await _alarmRecordService.ClearBySiteIdAsync(siteId);
            
            if (!success)
            {
                return BadRequest(ApiResponse<bool>.Fail("清除报警失败", "OPERATION_FAILED"));
            }

            _logger.LogInformation("✅ 站点 {SiteId} 的所有活跃报警已清除", siteId);
            return Ok(ApiResponse<bool>.Ok(true, "清除报警成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 清除站点报警失败: SiteId={SiteId}", siteId);
            return StatusCode(500, ApiResponse<bool>.Fail("清除报警失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 获取报警统计
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<AlarmRecordStatisticsDto>>> GetStatistics([FromQuery] int? siteId = null)
    {
        try
        {
            var statistics = await _alarmRecordService.GetStatisticsAsync(siteId);
            return Ok(ApiResponse<AlarmRecordStatisticsDto>.Ok(statistics, "获取报警统计成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取报警统计失败");
            return StatusCode(500, ApiResponse<AlarmRecordStatisticsDto>.Fail("获取报警统计失败", "INTERNAL_ERROR"));
        }
    }
}

