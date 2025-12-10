using System.ComponentModel.DataAnnotations;

namespace PumpRoomAutomationBackend.DTOs;

/// <summary>
/// 系统配置 DTO
/// System configuration data transfer object
/// </summary>
public class SystemConfigDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    
    // 电话报警配置
    public string? PhoneAlarmAddress { get; set; }
    public string? PhoneAccessId { get; set; }
    public string? PhoneAccessKey { get; set; }
    
    // 短信配置
    public string? SmsAccessId { get; set; }
    public string? SmsAccessKey { get; set; }
    
    // 邮件服务器配置
    public string? SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string? EmailAccount { get; set; }
    public string? EmailPassword { get; set; }
    
    // 配置状态
    public bool IsActive { get; set; }
    
    // 时间戳
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 创建/更新系统配置 DTO
/// Create/Update system configuration data transfer object
/// </summary>
public class SystemConfigCreateDto
{
    // 电话报警配置
    [MaxLength(255, ErrorMessage = "电话报警地址最大长度为 255")]
    public string? PhoneAlarmAddress { get; set; }
    
    [MaxLength(100, ErrorMessage = "电话访问 ID 最大长度为 100")]
    public string? PhoneAccessId { get; set; }
    
    public string? PhoneAccessKey { get; set; }
    
    // 短信配置
    [MaxLength(100, ErrorMessage = "短信访问 ID 最大长度为 100")]
    public string? SmsAccessId { get; set; }
    
    public string? SmsAccessKey { get; set; }
    
    // 邮件服务器配置
    [MaxLength(255, ErrorMessage = "SMTP 服务器地址最大长度为 255")]
    public string? SmtpServer { get; set; }
    
    [Range(1, 65535, ErrorMessage = "SMTP 端口必须在 1-65535 之间")]
    public int? SmtpPort { get; set; }
    
    [MaxLength(255, ErrorMessage = "邮箱账号最大长度为 255")]
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string? EmailAccount { get; set; }
    
    public string? EmailPassword { get; set; }
    
    // 配置状态（可选，默认为 true）
    public bool? IsActive { get; set; }
}


