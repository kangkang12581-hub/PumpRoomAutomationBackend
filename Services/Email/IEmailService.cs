using System.Threading.Tasks;

namespace PumpRoomAutomationBackend.Services.Email;

/// <summary>
/// 邮件附件数据模型
/// Email attachment data model
/// </summary>
public class EmailAttachment
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>
/// 邮件服务接口
/// Email Service Interface
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// 发送报警邮件（带附件）
    /// Send alarm email with attachment
    /// </summary>
    /// <param name="toEmail">收件人邮箱</param>
    /// <param name="subject">邮件主题</param>
    /// <param name="body">邮件正文（HTML格式）</param>
    /// <param name="attachmentData">附件数据（图片字节数组）</param>
    /// <param name="attachmentName">附件名称</param>
    Task<bool> SendAlarmEmailAsync(string toEmail, string subject, string body, byte[]? attachmentData = null, string? attachmentName = null);
    
    /// <summary>
    /// 发送报警邮件给多个收件人
    /// Send alarm email to multiple recipients
    /// </summary>
    Task<bool> SendAlarmEmailAsync(List<string> toEmails, string subject, string body, byte[]? attachmentData = null, string? attachmentName = null);
    
    /// <summary>
    /// 发送报警邮件给多个收件人（支持多个附件）
    /// Send alarm email to multiple recipients with multiple attachments
    /// </summary>
    Task<bool> SendAlarmEmailWithAttachmentsAsync(List<string> toEmails, string subject, string body, List<EmailAttachment>? attachments = null);
}

