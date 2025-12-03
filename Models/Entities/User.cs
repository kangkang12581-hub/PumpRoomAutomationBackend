using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using PumpRoomAutomationBackend.Models.Enums;

namespace PumpRoomAutomationBackend.Models.Entities;

/// <summary>
/// 用户表模型
/// User table model
/// </summary>
[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    // 基本信息
    [Column("username")]
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Column("display_name")]
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;
    
    [Column("email")]
    [MaxLength(100)]
    public string? Email { get; set; }
    
    [Column("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }
    
    [Column("hashed_password")]
    [Required]
    [MaxLength(255)]
    public string HashedPassword { get; set; } = string.Empty;
    
    // 用户分组和级别
    [Column("user_group")]
    [Required]
    public UserGroup UserGroup { get; set; } = UserGroup.OPERATOR;
    
    [Column("user_level")]
    [Required]
    public UserLevel UserLevel { get; set; } = UserLevel.LEVEL_3;
    
    // 权限和状态
    [Column("operation_timeout")]
    public int OperationTimeout { get; set; } = 3600;
    
    [Column("operation_permissions")]
    public string? OperationPermissions { get; set; }
    
    [Column("audit_permissions")]
    public string? AuditPermissions { get; set; }
    
    [Column("status")]
    [Required]
    public UserStatus Status { get; set; } = UserStatus.ACTIVE;
    
    // 兼容字段（保持向后兼容）
    [Column("full_name")]
    [MaxLength(100)]
    public string? FullName { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [Column("is_admin")]
    public bool IsAdmin { get; set; } = false;
    
    // 时间戳字段
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("last_login")]
    public DateTime? LastLogin { get; set; }
    
    // 额外信息
    [Column("description")]
    public string? Description { get; set; }
    
    // 导航属性
    public virtual ICollection<SystemConfig> SystemConfigs { get; set; } = new List<SystemConfig>();
    public virtual ICollection<SiteConfig> SiteConfigs { get; set; } = new List<SiteConfig>();
    public virtual ICollection<UserAlarmNotificationConfig> AlarmConfigs { get; set; } = new List<UserAlarmNotificationConfig>();
    public virtual UserSettings? Settings { get; set; }
    public virtual ICollection<OperationalParameters> OperationalParameters { get; set; } = new List<OperationalParameters>();
    
    // 辅助属性
    [NotMapped]
    public bool IsRoot => UserGroup == UserGroup.ROOT;
    
    [NotMapped]
    public bool IsManager => UserGroup == UserGroup.ADMIN;
    
    [NotMapped]
    public bool IsOperator => UserGroup == UserGroup.OPERATOR;
    
    [NotMapped]
    public bool IsObserver => UserGroup == UserGroup.OBSERVER;
    
    [NotMapped]
    public bool IsActiveStatus => Status == UserStatus.ACTIVE;
    
    /// <summary>
    /// 检查用户是否有指定权限
    /// </summary>
    public bool HasPermission(string permissionKey)
    {
        if (string.IsNullOrEmpty(OperationPermissions))
            return false;
        
        try
        {
            var permissions = JsonSerializer.Deserialize<Dictionary<string, bool>>(OperationPermissions);
            return permissions?.ContainsKey(permissionKey) == true && permissions[permissionKey];
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 检查用户是否有指定审核权限
    /// </summary>
    public bool HasAuditPermission(string auditKey)
    {
        if (string.IsNullOrEmpty(AuditPermissions))
            return false;
        
        try
        {
            var permissions = JsonSerializer.Deserialize<Dictionary<string, bool>>(AuditPermissions);
            return permissions?.ContainsKey(auditKey) == true && permissions[auditKey];
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// 登录日志表模型
/// Login log table model
/// </summary>
[Table("login_logs")]
public class LoginLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("user_id")]
    public int? UserId { get; set; }
    
    [Column("username")]
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Column("ip_address")]
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [Column("user_agent")]
    public string? UserAgent { get; set; }
    
    [Column("login_time")]
    public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    
    [Column("success")]
    [Required]
    public bool Success { get; set; }
    
    [Column("error_message")]
    public string? ErrorMessage { get; set; }
}

