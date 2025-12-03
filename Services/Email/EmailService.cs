using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services.Email;

/// <summary>
/// 邮件服务实现
/// Email Service Implementation
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EmailService(
        IConfiguration configuration, 
        ILogger<EmailService> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 从 SystemConfig 表获取邮件配置
    /// </summary>
    private async Task<(string smtpHost, int smtpPort, string smtpUsername, string smtpPassword, string fromEmail, string fromName, bool enableSsl)> GetEmailConfigAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // 获取激活的邮件配置（优先使用激活的配置）
            var config = await dbContext.SystemConfigs
                .Where(c => c.IsActive && 
                           !string.IsNullOrEmpty(c.SmtpServer) && 
                           !string.IsNullOrEmpty(c.EmailAccount) && 
                           !string.IsNullOrEmpty(c.EmailPassword))
                .OrderByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync();

            if (config != null)
            {
                // 根据端口判断是否启用SSL
                // QQ邮箱：587端口使用TLS（EnableSsl=true），465端口使用SSL（需要特殊处理）
                bool enableSsl = config.SmtpPort == 465 || config.SmtpPort == 587;
                
                return (
                    smtpHost: config.SmtpServer ?? "smtp.qq.com",
                    smtpPort: config.SmtpPort > 0 ? config.SmtpPort : 587,
                    smtpUsername: config.EmailAccount ?? "",
                    smtpPassword: config.EmailPassword ?? "",
                    fromEmail: config.EmailAccount ?? "",
                    fromName: "泵房自动化系统",
                    enableSsl: enableSsl
                );
            }

            // 如果没有数据库配置，回退到配置文件
            _logger.LogWarning("⚠️ 未找到数据库邮件配置，使用配置文件中的设置");
            return (
                smtpHost: _configuration["Email:SmtpHost"] ?? "smtp.qq.com",
                smtpPort: int.TryParse(_configuration["Email:SmtpPort"], out var port) ? port : 587,
                smtpUsername: _configuration["Email:Username"] ?? "",
                smtpPassword: _configuration["Email:Password"] ?? "",
                fromEmail: _configuration["Email:FromEmail"] ?? _configuration["Email:Username"] ?? "",
                fromName: _configuration["Email:FromName"] ?? "泵房自动化系统",
                enableSsl: bool.TryParse(_configuration["Email:EnableSsl"], out var ssl) && ssl
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取邮件配置失败，使用默认配置");
            return (
                smtpHost: "smtp.qq.com",
                smtpPort: 587,
                smtpUsername: "",
                smtpPassword: "",
                fromEmail: "",
                fromName: "泵房自动化系统",
                enableSsl: true
            );
        }
    }

    public async Task<bool> SendAlarmEmailAsync(
        string toEmail, 
        string subject, 
        string body, 
        byte[]? attachmentData = null, 
        string? attachmentName = null)
    {
        return await SendAlarmEmailAsync(new List<string> { toEmail }, subject, body, attachmentData, attachmentName);
    }

    public async Task<bool> SendAlarmEmailAsync(
        List<string> toEmails, 
        string subject, 
        string body, 
        byte[]? attachmentData = null, 
        string? attachmentName = null)
    {
        try
        {
            _logger.LogInformation("📧 准备发送报警邮件到 {Count} 个收件人", toEmails.Count);
            
            // 从数据库获取邮件配置
            var (smtpHost, smtpPort, smtpUsername, smtpPassword, fromEmail, fromName, enableSsl) = await GetEmailConfigAsync();
            
            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("⚠️ 邮件服务未配置（SMTP用户名或密码为空），跳过发送");
                return false;
            }

            if (string.IsNullOrEmpty(smtpHost))
            {
                _logger.LogWarning("⚠️ SMTP服务器地址未配置，跳过发送");
                return false;
            }

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail, fromName);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            // 添加收件人
            foreach (var email in toEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                message.To.Add(email);
            }

            if (message.To.Count == 0)
            {
                _logger.LogWarning("⚠️ 没有有效的收件人邮箱");
                return false;
            }

            // 添加附件（如果有）
            if (attachmentData != null && attachmentData.Length > 0)
            {
                var stream = new MemoryStream(attachmentData);
                var attachment = new Attachment(stream, attachmentName ?? "snapshot.jpg", "image/jpeg");
                message.Attachments.Add(attachment);
                _logger.LogInformation("📎 添加附件: {FileName}, 大小: {Size} bytes", attachmentName, attachmentData.Length);
            }

            // 配置 SMTP 客户端
            using var smtpClient = new SmtpClient(smtpHost, smtpPort);
            smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
            smtpClient.EnableSsl = enableSsl;
            smtpClient.Timeout = 30000; // 30秒超时

            // QQ邮箱端口说明：
            // 587端口使用TLS（STARTTLS）- 推荐使用，兼容性最好
            // 465端口使用隐式SSL - System.Net.Mail.SmtpClient 不完全支持，可能导致 "Syntax error" 错误
            // 建议：统一使用587端口
            if (smtpPort == 465)
            {
                _logger.LogWarning("⚠️ 检测到使用465端口，System.Net.Mail可能不支持隐式SSL，建议改用587端口");
            }
            
            _logger.LogInformation("📧 使用SMTP服务器 {SmtpHost}:{SmtpPort} 发送邮件（SSL={EnableSsl}）", 
                smtpHost, smtpPort, enableSsl);

            // 发送邮件
            await smtpClient.SendMailAsync(message);
            
            _logger.LogInformation("✅ 邮件发送成功！收件人: {Recipients}, SMTP服务器: {SmtpHost}:{SmtpPort}", 
                string.Join(", ", message.To), smtpHost, smtpPort);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送邮件失败: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> SendAlarmEmailWithAttachmentsAsync(
        List<string> toEmails, 
        string subject, 
        string body, 
        List<EmailAttachment>? attachments = null)
    {
        try
        {
            _logger.LogInformation("📧 准备发送报警邮件到 {Count} 个收件人，附件数: {AttachmentCount}", 
                toEmails.Count, attachments?.Count ?? 0);
            
            // 从数据库获取邮件配置
            var (smtpHost, smtpPort, smtpUsername, smtpPassword, fromEmail, fromName, enableSsl) = await GetEmailConfigAsync();
            
            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("⚠️ 邮件服务未配置（SMTP用户名或密码为空），跳过发送");
                return false;
            }

            if (string.IsNullOrEmpty(smtpHost))
            {
                _logger.LogWarning("⚠️ SMTP服务器地址未配置，跳过发送");
                return false;
            }

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail, fromName);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            // 添加收件人
            foreach (var email in toEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                message.To.Add(email);
            }

            if (message.To.Count == 0)
            {
                _logger.LogWarning("⚠️ 没有有效的收件人邮箱");
                return false;
            }

            // 添加多个附件
            if (attachments != null && attachments.Any())
            {
                foreach (var att in attachments)
                {
                    if (att.Data != null && att.Data.Length > 0)
                    {
                        var stream = new MemoryStream(att.Data);
                        var attachment = new Attachment(stream, att.FileName, att.ContentType);
                        message.Attachments.Add(attachment);
                        _logger.LogInformation("📎 添加附件: {FileName}, 大小: {Size} bytes", att.FileName, att.Data.Length);
                    }
                }
            }

            // 配置 SMTP 客户端
            using var smtpClient = new SmtpClient(smtpHost, smtpPort);
            smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
            smtpClient.EnableSsl = enableSsl;
            smtpClient.Timeout = 30000; // 30秒超时

            if (smtpPort == 465)
            {
                _logger.LogWarning("⚠️ 检测到使用465端口，System.Net.Mail可能不支持隐式SSL，建议改用587端口");
            }
            
            _logger.LogInformation("📧 使用SMTP服务器 {SmtpHost}:{SmtpPort} 发送邮件（SSL={EnableSsl}）", 
                smtpHost, smtpPort, enableSsl);

            // 发送邮件
            await smtpClient.SendMailAsync(message);
            
            _logger.LogInformation("✅ 邮件发送成功！收件人: {Recipients}, 附件数: {AttachmentCount}", 
                string.Join(", ", message.To), attachments?.Count ?? 0);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送邮件失败: {Message}", ex.Message);
            return false;
        }
    }
}

