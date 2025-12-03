using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

/// <summary>
/// 系统配置表模型
/// System configuration table model
/// </summary>
[Table("system_configs")]
public class SystemConfig
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    // 关联用户ID（配置创建者/管理者）
    [Column("user_id")]
    [Required]
    public int UserId { get; set; }
    
    // 电话报警配置
    [Column("phone_alarm_address")]
    [MaxLength(255)]
    public string? PhoneAlarmAddress { get; set; }
    
    [Column("phone_access_id")]
    [MaxLength(100)]
    public string? PhoneAccessId { get; set; }
    
    [Column("phone_access_key")]
    public string? PhoneAccessKey { get; set; }
    
    // 短信配置
    [Column("sms_access_id")]
    [MaxLength(100)]
    public string? SmsAccessId { get; set; }
    
    [Column("sms_access_key")]
    public string? SmsAccessKey { get; set; }
    
    // 邮件服务器配置
    [Column("smtp_server")]
    [MaxLength(255)]
    public string? SmtpServer { get; set; }
    
    [Column("smtp_port")]
    public int SmtpPort { get; set; } = 587;
    
    [Column("email_account")]
    [MaxLength(255)]
    public string? EmailAccount { get; set; }
    
    [Column("email_password")]
    public string? EmailPassword { get; set; }
    
    // 配置状态
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    // 时间戳字段
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

