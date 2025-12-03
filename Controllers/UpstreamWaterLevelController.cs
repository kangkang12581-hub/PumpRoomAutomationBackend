using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Services;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// 上游液位数据控制器
/// </summary>
[ApiController]
[Route("api/data/upstream-water-level")]
[Authorize]
public class UpstreamWaterLevelController : ControllerBase
{
    private readonly IUpstreamWaterLevelService _service;
    private readonly ILogger<UpstreamWaterLevelController> _logger;

    public UpstreamWaterLevelController(
        IUpstreamWaterLevelService service,
        ILogger<UpstreamWaterLevelController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// 添加单条液位数据
    /// </summary>
    /// <param name="request">液位数据</param>
    /// <returns>添加的数据</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UpstreamWaterLevelDto>>> AddData(
        [FromBody] AddUpstreamWaterLevelRequest request)
    {
        try
        {
            var result = await _service.AddDataAsync(request);
            return Ok(ApiResponse<UpstreamWaterLevelDto>.Ok(result, "添加成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<UpstreamWaterLevelDto>.Fail(ex.Message, "DUPLICATE_DATA"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加上游液位数据失败");
            return StatusCode(500, ApiResponse<UpstreamWaterLevelDto>.Fail("添加数据失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 批量添加液位数据
    /// </summary>
    /// <param name="request">批量数据</param>
    /// <returns>添加的数据条数</returns>
    [HttpPost("batch")]
    public async Task<ActionResult<ApiResponse<int>>> BatchAddData(
        [FromBody] BatchAddUpstreamWaterLevelRequest request)
    {
        try
        {
            if (request.Data == null || !request.Data.Any())
            {
                return BadRequest(ApiResponse<int>.Fail("数据列表不能为空", "EMPTY_DATA"));
            }

            var count = await _service.BatchAddDataAsync(request);
            return Ok(ApiResponse<int>.Ok(count, $"成功添加 {count} 条数据"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<int>.Fail(ex.Message, "BATCH_ADD_ERROR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量添加上游液位数据失败");
            return StatusCode(500, ApiResponse<int>.Fail("批量添加失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 查询液位数据
    /// </summary>
    /// <param name="siteId">站点ID</param>
    /// <param name="startTime">开始时间（ISO8601格式字符串）</param>
    /// <param name="endTime">结束时间（ISO8601格式字符串）</param>
    /// <param name="interval">聚合间隔：minute/hour/day/month，默认minute</param>
    /// <param name="limit">最大返回数量，默认10000</param>
    /// <returns>液位数据</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<UpstreamWaterLevelQueryResult>>> QueryData(
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
                return BadRequest(ApiResponse<UpstreamWaterLevelQueryResult>.Fail("开始时间不能为空", "MISSING_START_TIME"));
            }
            
            if (string.IsNullOrEmpty(endTimeStr))
            {
                return BadRequest(ApiResponse<UpstreamWaterLevelQueryResult>.Fail("结束时间不能为空", "MISSING_END_TIME"));
            }
            
            // 手动解析DateTime
            if (!DateTime.TryParse(startTimeStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsedStartTime))
            {
                return BadRequest(ApiResponse<UpstreamWaterLevelQueryResult>.Fail($"开始时间格式无效: {startTimeStr}", "INVALID_START_TIME"));
            }
            
            if (!DateTime.TryParse(endTimeStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsedEndTime))
            {
                return BadRequest(ApiResponse<UpstreamWaterLevelQueryResult>.Fail($"结束时间格式无效: {endTimeStr}", "INVALID_END_TIME"));
            }
            
            _logger.LogInformation("解析后的时间: StartTime={StartTime} (Kind={StartKind}), EndTime={EndTime} (Kind={EndKind})", 
                parsedStartTime, parsedStartTime.Kind, parsedEndTime, parsedEndTime.Kind);
            
            var request = new QueryUpstreamWaterLevelRequest
            {
                SiteId = siteId,
                StartTime = parsedStartTime.Kind == DateTimeKind.Utc ? parsedStartTime : parsedStartTime.ToUniversalTime(),
                EndTime = parsedEndTime.Kind == DateTimeKind.Utc ? parsedEndTime : parsedEndTime.ToUniversalTime(),
                Interval = interval,
                Limit = limit
            };

            var result = await _service.QueryDataAsync(request);
            return Ok(ApiResponse<UpstreamWaterLevelQueryResult>.Ok(result, "查询成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<UpstreamWaterLevelQueryResult>.Fail(ex.Message, "INVALID_PARAMETER"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询上游液位数据失败: SiteId={SiteId}, Start={Start}, End={End}", 
                siteId, startTimeStr, endTimeStr);
            return StatusCode(500, ApiResponse<UpstreamWaterLevelQueryResult>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 获取指定站点的最新液位数据
    /// </summary>
    /// <param name="siteId">站点ID</param>
    /// <returns>最新的液位数据</returns>
    [HttpGet("latest")]
    public async Task<ActionResult<ApiResponse<UpstreamWaterLevelDto>>> GetLatestData(
        [FromQuery] int siteId)
    {
        try
        {
            var result = await _service.GetLatestDataAsync(siteId);
            
            if (result == null)
            {
                return NotFound(ApiResponse<UpstreamWaterLevelDto>.Fail("未找到数据", "NO_DATA"));
            }

            return Ok(ApiResponse<UpstreamWaterLevelDto>.Ok(result, "查询成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最新液位数据失败: SiteId={SiteId}", siteId);
            return StatusCode(500, ApiResponse<UpstreamWaterLevelDto>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 删除指定日期之前的数据（管理员功能）
    /// </summary>
    /// <param name="beforeDate">指定日期</param>
    /// <returns>删除的数据条数</returns>
    [HttpDelete]
    [Authorize(Roles = "root,admin")]
    public async Task<ActionResult<ApiResponse<int>>> DeleteOldData(
        [FromQuery] DateTime beforeDate)
    {
        try
        {
            var count = await _service.DeleteDataBeforeAsync(beforeDate);
            return Ok(ApiResponse<int>.Ok(count, $"成功删除 {count} 条数据"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除过期数据失败: BeforeDate={Date}", beforeDate);
            return StatusCode(500, ApiResponse<int>.Fail("删除失败", "INTERNAL_ERROR"));
        }
    }
}

