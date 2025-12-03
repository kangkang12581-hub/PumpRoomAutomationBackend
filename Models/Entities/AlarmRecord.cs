using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PumpRoomAutomationBackend.Models.Enums;

namespace PumpRoomAutomationBackend.Models.Entities;

/// <summary>
/// 报警记录表模型
/// Alarm record table model
/// </summary>
[Table("alarm_records")]
public class AlarmRecord
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    // 关联站点ID
    [Column("site_id")]
    [Required]
    public int SiteId { get; set; }
    
    // 报警基本信息
    [Column("alarm_name")]
    [Required]
    [MaxLength(100)]
    public string AlarmName { get; set; } = string.Empty;
    
    [Column("alarm_description")]
    public string? AlarmDescription { get; set; }
    
    [Column("node_id")]
    [Required]
    [MaxLength(200)]
    public string NodeId { get; set; } = string.Empty;
    
    [Column("node_name")]
    [Required]
    [MaxLength(100)]
    public string NodeName { get; set; } = string.Empty;
    
    // 报警级别和状态
    [Column("severity")]
    [Required]
    public AlarmSeverity Severity { get; set; } = AlarmSeverity.Medium;
    
    [Column("status")]
    [Required]
    public AlarmStatus Status { get; set; } = AlarmStatus.Active;
    
    // 报警值信息
    [Column("current_value")]
    [MaxLength(100)]
    public string? CurrentValue { get; set; }
    
    [Column("alarm_value")]
    [MaxLength(100)]
    public string? AlarmValue { get; set; }
    
    [Column("unit")]
    [MaxLength(20)]
    public string? Unit { get; set; }
    
    // 时间信息
    [Column("alarm_start_time")]
    [Required]
    public DateTime AlarmStartTime { get; set; }
    
    [Column("alarm_end_time")]
    public DateTime? AlarmEndTime { get; set; }
    
    [Column("acknowledged_time")]
    public DateTime? AcknowledgedTime { get; set; }
    
    [Column("acknowledged_by")]
    [MaxLength(100)]
    public string? AcknowledgedBy { get; set; }
    
    // 备注信息
    [Column("remarks")]
    public string? Remarks { get; set; }
    
    // 时间戳字段
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    [ForeignKey("SiteId")]
    public virtual SiteConfig Site { get; set; } = null!;
}

