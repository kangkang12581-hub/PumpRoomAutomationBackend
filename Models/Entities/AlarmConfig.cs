using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

/// <summary>
/// 报警配置定义模型（支持多站点配置）
/// Alarm config definition model (multi-site support)
/// </summary>
[Table("alarm_configs")]
public class AlarmConfig
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    /// <summary>
    /// 站点ID（NULL表示全局配置）
    /// </summary>
    [Column("site_id")]
    public int? SiteId { get; set; }
    
    /// <summary>
    /// 报警代码（在同一站点内唯一）
    /// </summary>
    [Column("alarm_code")]
    [Required]
    [MaxLength(50)]
    public string AlarmCode { get; set; } = string.Empty;
    
    /// <summary>
    /// 报警名称
    /// </summary>
    [Column("alarm_name")]
    [Required]
    [MaxLength(200)]
    public string AlarmName { get; set; } = string.Empty;
    
    /// <summary>
    /// 报警消息内容
    /// </summary>
    [Column("alarm_message")]
    [Required]
    public string AlarmMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// 报警类别（重量类、电机类、流体类、通讯类、控制类）
    /// </summary>
    [Column("alarm_category")]
    [Required]
    [MaxLength(50)]
    public string AlarmCategory { get; set; } = string.Empty;
    
    /// <summary>
    /// 严重程度：info, warning, error, critical
    /// </summary>
    [Column("severity")]
    [MaxLength(20)]
    public string Severity { get; set; } = "warning";
    
    /// <summary>
    /// 触发变量（OPC UA节点）
    /// </summary>
    [Column("trigger_variable")]
    [MaxLength(100)]
    public string? TriggerVariable { get; set; }
    
    /// <summary>
    /// 触发位（用于布尔位变量）
    /// </summary>
    [Column("trigger_bit")]
    public int? TriggerBit { get; set; }
    
    /// <summary>
    /// 是否自动清除
    /// </summary>
    [Column("auto_clear")]
    public bool AutoClear { get; set; } = false;
    
    /// <summary>
    /// 是否需要确认
    /// </summary>
    [Column("require_confirmation")]
    public bool RequireConfirmation { get; set; } = true;
    
    /// <summary>
    /// 详细描述
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 解决方案指南
    /// </summary>
    [Column("solution_guide")]
    public string? SolutionGuide { get; set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 显示顺序
    /// </summary>
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    [ForeignKey("SiteId")]
    public virtual SiteConfig? Site { get; set; }
}
