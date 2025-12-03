using System.ComponentModel.DataAnnotations;

namespace PumpRoomAutomationBackend.DTOs;

public class SystemConfigCreateDto
{
    public string? PhoneAlarmAddress { get; set; }
    public string? PhoneAccessId { get; set; }
    public string? PhoneAccessKey { get; set; }
    public string? SmsAccessId { get; set; }
    public string? SmsAccessKey { get; set; }
    public string? SmtpServer { get; set; }
    public int? SmtpPort { get; set; }
    public string? EmailAccount { get; set; }
    public string? EmailPassword { get; set; }
    public bool? IsActive { get; set; }
}

public class SystemConfigDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? PhoneAlarmAddress { get; set; }
    public string? PhoneAccessId { get; set; }
    public string? PhoneAccessKey { get; set; }
    public string? SmsAccessId { get; set; }
    public string? SmsAccessKey { get; set; }
    public string? SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public string? EmailAccount { get; set; }
    public string? EmailPassword { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

