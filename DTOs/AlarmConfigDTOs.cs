using System.ComponentModel.DataAnnotations;

namespace PumpRoomAutomationBackend.DTOs;

/// <summary>
/// 报警配置响应DTO
/// </summary>
public class AlarmConfigDto
{
    public int Id { get; set; }
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }
    public string? SiteCode { get; set; }
    public string AlarmCode { get; set; } = string.Empty;
    public string AlarmName { get; set; } = string.Empty;
    public string AlarmMessage { get; set; } = string.Empty;
    public string AlarmCategory { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? TriggerVariable { get; set; }
    public int? TriggerBit { get; set; }
    public bool AutoClear { get; set; }
    public bool RequireConfirmation { get; set; }
    public string? Description { get; set; }
    public string? SolutionGuide { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 创建报警配置请求DTO
/// </summary>
public class CreateAlarmConfigRequest
{
    /// <summary>
    /// 站点ID（NULL表示全局配置）
    /// </summary>
    public int? SiteId { get; set; }
    
    [Required(ErrorMessage = "报警代码不能为空")]
    [MaxLength(50)]
    public string AlarmCode { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "报警名称不能为空")]
    [MaxLength(200)]
    public string AlarmName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "报警消息不能为空")]
    public string AlarmMessage { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "报警类别不能为空")]
    [MaxLength(50)]
    public string AlarmCategory { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string Severity { get; set; } = "warning";
    
    [MaxLength(100)]
    public string? TriggerVariable { get; set; }
    
    public int? TriggerBit { get; set; }
    
    public bool AutoClear { get; set; } = false;
    
    public bool RequireConfirmation { get; set; } = true;
    
    public string? Description { get; set; }
    
    public string? SolutionGuide { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// 更新报警配置请求DTO
/// </summary>
public class UpdateAlarmConfigRequest
{
    [MaxLength(200)]
    public string? AlarmName { get; set; }
    
    public string? AlarmMessage { get; set; }
    
    [MaxLength(50)]
    public string? AlarmCategory { get; set; }
    
    [MaxLength(20)]
    public string? Severity { get; set; }
    
    [MaxLength(100)]
    public string? TriggerVariable { get; set; }
    
    public int? TriggerBit { get; set; }
    
    public bool? AutoClear { get; set; }
    
    public bool? RequireConfirmation { get; set; }
    
    public string? Description { get; set; }
    
    public string? SolutionGuide { get; set; }
    
    public bool? IsActive { get; set; }
    
    public int? DisplayOrder { get; set; }
}

/// <summary>
/// 报警配置查询参数
/// </summary>
public class AlarmConfigQueryParams
{
    /// <summary>
    /// 站点ID（NULL查询全局配置）
    /// </summary>
    public int? SiteId { get; set; }
    
    /// <summary>
    /// 是否包含全局配置（默认true）
    /// </summary>
    public bool IncludeGlobal { get; set; } = true;
    
    /// <summary>
    /// 报警类别（重量类、电机类、流体类、通讯类、控制类）
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// 严重程度（info、warning、error、critical）
    /// </summary>
    public string? Severity { get; set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// 搜索关键词（名称或消息）
    /// </summary>
    public string? SearchKeyword { get; set; }
    
    /// <summary>
    /// 页码（从1开始）
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// 分页响应DTO
/// </summary>
public class PagedAlarmConfigsResponse
{
    public List<AlarmConfigDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// 报警配置统计DTO
/// </summary>
public class AlarmConfigStatisticsDto
{
    public Dictionary<string, int> CategoryCounts { get; set; } = new();
    public Dictionary<string, int> SeverityCounts { get; set; } = new();
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}

