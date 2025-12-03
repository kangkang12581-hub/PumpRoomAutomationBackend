using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

/// <summary>
/// 用户报警通知配置表模型
/// User alarm notification configuration table model
/// </summary>
[Table("user_alarm_notifications")]
public class UserAlarmNotificationConfig
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    // 关联用户ID（配置创建者/管理者）
    [Column("user_id")]
    [Required]
    public int UserId { get; set; }
    
    // 报警基本配置
    [Column("alarm_name")]
    [Required]
    [MaxLength(100)]
    public string AlarmName { get; set; } = string.Empty;
    
    // 报警方式开关
    [Column("phone_alarm_enabled")]
    public bool PhoneAlarmEnabled { get; set; } = false;
    
    [Column("sms_alarm_enabled")]
    public bool SmsAlarmEnabled { get; set; } = false;
    
    [Column("email_alarm_enabled")]
    public bool EmailAlarmEnabled { get; set; } = false;
    
    // 拍照功能开关
    [Column("global_photo_enabled")]
    public bool GlobalPhotoEnabled { get; set; } = false;
    
    [Column("internal_photo_enabled")]
    public bool InternalPhotoEnabled { get; set; } = false;
    
    // 配置描述和备注
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("remarks")]
    public string? Remarks { get; set; }
    
    // 配置状态
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("is_default")]
    public bool IsDefault { get; set; } = false;
    
    // 应用范围（可以是全局、特定站点等）
    [Column("scope")]
    [MaxLength(50)]
    public string Scope { get; set; } = "global";
    
    [Column("scope_target")]
    [MaxLength(100)]
    public string? ScopeTarget { get; set; }
    
    // 优先级（数字越小优先级越高）
    [Column("priority")]
    public int Priority { get; set; } = 10;
    
    // 时间戳字段
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("created_by")]
    [MaxLength(100)]
    public string? CreatedBy { get; set; }
    
    [Column("updated_by")]
    [MaxLength(100)]
    public string? UpdatedBy { get; set; }
    
    // 导航属性
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

