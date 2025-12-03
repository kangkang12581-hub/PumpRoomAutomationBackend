using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PumpRoomAutomationBackend.DTOs;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.Services;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// 报警配置管理控制器
/// </summary>
[ApiController]
[Route("api/alarm-configs")]
[Authorize]
public class AlarmConfigController : ControllerBase
{
    private readonly IAlarmConfigService _alarmConfigService;
    private readonly ILogger<AlarmConfigController> _logger;

    public AlarmConfigController(
        IAlarmConfigService alarmConfigService,
        ILogger<AlarmConfigController> logger)
    {
        _alarmConfigService = alarmConfigService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有报警配置
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AlarmConfigDto>>>> GetAll()
    {
        try
        {
            var alarmConfigs = await _alarmConfigService.GetAllAsync();
            return Ok(ApiResponse<List<AlarmConfigDto>>.Ok(alarmConfigs, $"获取报警配置成功，共 {alarmConfigs.Count} 条"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取报警配置列表失败");
            return StatusCode(500, ApiResponse<List<AlarmConfigDto>>.Fail("获取报警配置列表失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 分页查询报警配置
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedAlarmConfigsResponse>>> GetPaged(
        [FromQuery] AlarmConfigQueryParams queryParams)
    {
        try
        {
            var result = await _alarmConfigService.GetPagedAsync(queryParams);
            return Ok(ApiResponse<PagedAlarmConfigsResponse>.Ok(result, 
                $"查询成功，共 {result.TotalCount} 条记录，当前第 {result.Page}/{result.TotalPages} 页"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 分页查询报警配置失败");
            return StatusCode(500, ApiResponse<PagedAlarmConfigsResponse>.Fail("查询失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 根据ID获取报警配置
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AlarmConfigDto>>> GetById(int id)
    {
        try
        {
            var alarmConfig = await _alarmConfigService.GetByIdAsync(id);
            if (alarmConfig == null)
            {
                return NotFound(ApiResponse<AlarmConfigDto>.Fail($"报警配置 ID {id} 不存在", "NOT_FOUND"));
            }

            return Ok(ApiResponse<AlarmConfigDto>.Ok(alarmConfig));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取报警配置失败: ID={Id}", id);
            return StatusCode(500, ApiResponse<AlarmConfigDto>.Fail("获取报警配置失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 根据报警代码获取报警配置
    /// </summary>
    [HttpGet("code/{alarmCode}")]
    public async Task<ActionResult<ApiResponse<AlarmConfigDto>>> GetByCode(string alarmCode)
    {
        try
        {
            var alarmConfig = await _alarmConfigService.GetByCodeAsync(alarmCode);
            if (alarmConfig == null)
            {
                return NotFound(ApiResponse<AlarmConfigDto>.Fail($"报警代码 {alarmCode} 不存在", "NOT_FOUND"));
            }

            return Ok(ApiResponse<AlarmConfigDto>.Ok(alarmConfig));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取报警配置失败: Code={Code}", alarmCode);
            return StatusCode(500, ApiResponse<AlarmConfigDto>.Fail("获取报警配置失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 根据类别获取报警配置
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<ActionResult<ApiResponse<List<AlarmConfigDto>>>> GetByCategory(string category)
    {
        try
        {
            var alarmConfigs = await _alarmConfigService.GetByCategoryAsync(category);
            return Ok(ApiResponse<List<AlarmConfigDto>>.Ok(alarmConfigs, 
                $"获取 {category} 类别报警成功，共 {alarmConfigs.Count} 条"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取报警配置失败: Category={Category}", category);
            return StatusCode(500, ApiResponse<List<AlarmConfigDto>>.Fail("获取报警配置失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 根据严重程度获取报警配置
    /// </summary>
    [HttpGet("severity/{severity}")]
    public async Task<ActionResult<ApiResponse<List<AlarmConfigDto>>>> GetBySeverity(string severity)
    {
        try
        {
            var alarmConfigs = await _alarmConfigService.GetBySeverityAsync(severity);
            return Ok(ApiResponse<List<AlarmConfigDto>>.Ok(alarmConfigs, 
                $"获取 {severity} 级别报警成功，共 {alarmConfigs.Count} 条"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取报警配置失败: Severity={Severity}", severity);
            return StatusCode(500, ApiResponse<List<AlarmConfigDto>>.Fail("获取报警配置失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 根据站点ID获取报警配置（包含全局配置）
    /// </summary>
    [HttpGet("site/{siteId}")]
    public async Task<ActionResult<ApiResponse<List<AlarmConfigDto>>>> GetBySiteId(
        int siteId, 
        [FromQuery] bool includeGlobal = true)
    {
        try
        {
            var alarmConfigs = await _alarmConfigService.GetBySiteIdAsync(siteId, includeGlobal);
            var message = includeGlobal 
                ? $"获取站点 {siteId} 的报警配置成功（包含全局配置），共 {alarmConfigs.Count} 条"
                : $"获取站点 {siteId} 的报警配置成功，共 {alarmConfigs.Count} 条";
            return Ok(ApiResponse<List<AlarmConfigDto>>.Ok(alarmConfigs, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取站点报警配置失败: SiteId={SiteId}", siteId);
            return StatusCode(500, ApiResponse<List<AlarmConfigDto>>.Fail("获取站点报警配置失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 创建报警配置
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ROOT,ADMIN")]
    public async Task<ActionResult<ApiResponse<AlarmConfigDto>>> Create([FromBody] CreateAlarmConfigRequest request)
    {
        try
        {
            var alarmConfig = await _alarmConfigService.CreateAsync(request);
            _logger.LogInformation("✅ 创建报警配置: {Code} - {Name}", alarmConfig.AlarmCode, alarmConfig.AlarmName);
            return CreatedAtAction(nameof(GetById), new { id = alarmConfig.Id }, 
                ApiResponse<AlarmConfigDto>.Ok(alarmConfig, "创建报警配置成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AlarmConfigDto>.Fail(ex.Message, "DUPLICATE_CODE"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建报警配置失败");
            return StatusCode(500, ApiResponse<AlarmConfigDto>.Fail("创建报警配置失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 更新报警配置
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "ROOT,ADMIN")]
    public async Task<ActionResult<ApiResponse<AlarmConfigDto>>> Update(
        int id, 
        [FromBody] UpdateAlarmConfigRequest request)
    {
        try
        {
            var alarmConfig = await _alarmConfigService.UpdateAsync(id, request);
            _logger.LogInformation("✅ 更新报警配置: ID={Id}", id);
            return Ok(ApiResponse<AlarmConfigDto>.Ok(alarmConfig, "更新报警配置成功"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<AlarmConfigDto>.Fail(ex.Message, "NOT_FOUND"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新报警配置失败: ID={Id}", id);
            return StatusCode(500, ApiResponse<AlarmConfigDto>.Fail("更新报警配置失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 删除报警配置
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "ROOT,ADMIN")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        try
        {
            var result = await _alarmConfigService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(ApiResponse<object>.Fail($"报警配置 ID {id} 不存在", "NOT_FOUND"));
            }

            _logger.LogInformation("🗑️ 删除报警配置: ID={Id}", id);
            return Ok(ApiResponse<object>.Ok(default(object)!, "删除报警配置成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除报警配置失败: ID={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("删除报警配置失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 启用/禁用报警配置
    /// </summary>
    [HttpPatch("{id}/toggle")]
    [Authorize(Roles = "ROOT,ADMIN")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleActive(
        int id, 
        [FromBody] ToggleActiveRequest request)
    {
        try
        {
            var result = await _alarmConfigService.ToggleActiveAsync(id, request.IsActive);
            if (!result)
            {
                return NotFound(ApiResponse<object>.Fail($"报警配置 ID {id} 不存在", "NOT_FOUND"));
            }

            _logger.LogInformation("🔄 切换报警配置状态: ID={Id}, IsActive={IsActive}", id, request.IsActive);
            return Ok(ApiResponse<object>.Ok(default(object)!, $"报警配置已{(request.IsActive ? "启用" : "禁用")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 切换报警配置状态失败: ID={Id}", id);
            return StatusCode(500, ApiResponse<object>.Fail("操作失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 获取报警配置统计信息
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<AlarmConfigStatisticsDto>>> GetStatistics()
    {
        try
        {
            var statistics = await _alarmConfigService.GetStatisticsAsync();
            return Ok(ApiResponse<AlarmConfigStatisticsDto>.Ok(statistics, "获取统计信息成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取统计信息失败");
            return StatusCode(500, ApiResponse<AlarmConfigStatisticsDto>.Fail("获取统计信息失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 获取所有报警类别
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetCategories()
    {
        try
        {
            var categories = await _alarmConfigService.GetAllCategoriesAsync();
            return Ok(ApiResponse<List<string>>.Ok(categories, $"获取类别成功，共 {categories.Count} 个"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取报警类别失败");
            return StatusCode(500, ApiResponse<List<string>>.Fail("获取类别失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 获取所有严重程度级别
    /// </summary>
    [HttpGet("severities")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetSeverities()
    {
        try
        {
            var severities = await _alarmConfigService.GetAllSeverityLevelsAsync();
            return Ok(ApiResponse<List<string>>.Ok(severities, $"获取严重程度级别成功，共 {severities.Count} 个"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取严重程度级别失败");
            return StatusCode(500, ApiResponse<List<string>>.Fail("获取级别失败", "INTERNAL_ERROR"));
        }
    }
}

/// <summary>
/// 切换激活状态请求
/// </summary>
public class ToggleActiveRequest
{
    public bool IsActive { get; set; }
}

