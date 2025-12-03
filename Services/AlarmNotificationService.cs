using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.Models.Entities;
using PumpRoomAutomationBackend.Models.Enums;
using PumpRoomAutomationBackend.Services;
using PumpRoomAutomationBackend.Services.Email;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 报警通知服务
/// Alarm Notification Service
/// </summary>
public interface IAlarmNotificationService
{
    Task SendAlarmNotificationAsync(AlarmRecord alarmRecord);
}

public class AlarmNotificationService : IAlarmNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ICameraService _cameraService;
    private readonly ISmsService _smsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AlarmNotificationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public AlarmNotificationService(
        ApplicationDbContext context,
        IEmailService emailService,
        ICameraService cameraService,
        ISmsService smsService,
        IConfiguration configuration,
        ILogger<AlarmNotificationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _emailService = emailService;
        _cameraService = cameraService;
        _smsService = smsService;
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendAlarmNotificationAsync(AlarmRecord alarmRecord)
    {
        try
        {
            _logger.LogInformation("🔔 开始处理报警通知: {AlarmId}, 站点: {SiteId}", alarmRecord.Id, alarmRecord.SiteId);
            Console.WriteLine($"[AlarmNotificationService] 开始处理报警 AlarmId={alarmRecord.Id}, SiteId={alarmRecord.SiteId}, AlarmName={alarmRecord.AlarmName}");

            // 1. 获取负责该站点的用户邮箱
            var recipientEmails = await GetSiteResponsibleUsersEmailsAsync(alarmRecord.SiteId);
            Console.WriteLine($"[AlarmNotificationService] 负责站点 {alarmRecord.SiteId} 的邮箱: {string.Join(",", recipientEmails)}");
            
            if (!recipientEmails.Any())
            {
                _logger.LogWarning("⚠️ 站点 {SiteId} 没有配置负责人邮箱", alarmRecord.SiteId);
                Console.WriteLine($"[AlarmNotificationService] 站点 {alarmRecord.SiteId} 没有负责人邮箱，终止发送");
                return;
            }

            // 2. 获取站点信息
            var site = await _context.SiteConfigs
                .FirstOrDefaultAsync(s => s.Id == alarmRecord.SiteId);
            
            string siteName = site?.SiteName ?? $"站点 {alarmRecord.SiteId}";

            // 3. 摄像头截图功能（使用新的站点截图接口）
            var attachments = new List<EmailAttachment>();
            _logger.LogInformation("📷 开始获取站点 {SiteId} 的摄像头截图...", alarmRecord.SiteId);
            
            // 记录站点摄像头配置信息（用于调试）
            if (site != null)
            {
                _logger.LogInformation("📷 站点 {SiteId} 摄像头配置: 机内IP={InternalIp}, 全局IP={GlobalIp}", 
                    alarmRecord.SiteId, 
                    site.InternalCameraIp ?? "未配置", 
                    site.GlobalCameraIp ?? "未配置");
            }
            else
            {
                _logger.LogWarning("⚠️ 站点 {SiteId} 配置不存在", alarmRecord.SiteId);
            }
            
            try
            {
                if (site != null)
                {
                    // 检查是否有摄像头配置
                    bool hasCamera = !string.IsNullOrEmpty(site.InternalCameraIp) || !string.IsNullOrEmpty(site.GlobalCameraIp);
                    
                    if (!hasCamera)
                    {
                        _logger.LogWarning("⚠️ 站点 {SiteId} ({SiteName}) 没有配置摄像头，跳过截图", 
                            alarmRecord.SiteId, siteName);
                    }
                    else
                    {
                        // 使用新的站点截图接口（一次性获取所有摄像头截图）
                        _logger.LogInformation("📷 调用摄像头服务获取站点 {SiteId} 的截图...", alarmRecord.SiteId);
                        var siteSnapshotResult = await _cameraService.GetSiteSnapshotsAsync(
                            siteId: site.Id,
                            internalCameraIp: site.InternalCameraIp,
                            internalCameraUsername: site.InternalCameraUsername,
                            internalCameraPassword: site.InternalCameraPassword,
                            globalCameraIp: site.GlobalCameraIp,
                            globalCameraUsername: site.GlobalCameraUsername,
                            globalCameraPassword: site.GlobalCameraPassword
                        );
                        
                        _logger.LogInformation("📷 站点 {SiteId} 截图结果: 总数={Total}, 成功={Success}, 失败={Failed}", 
                            alarmRecord.SiteId, 
                            siteSnapshotResult.TotalCameras, 
                            siteSnapshotResult.SuccessfulSnapshots, 
                            siteSnapshotResult.FailedSnapshots);
                    
                        // 将成功的截图添加到附件列表
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        foreach (var snapshot in siteSnapshotResult.Snapshots.Where(s => s.Success && s.ImageData != null))
                        {
                            attachments.Add(new EmailAttachment
                            {
                                Data = snapshot.ImageData!,
                                FileName = $"{snapshot.CameraType}_camera_{snapshot.CameraIp}_{timestamp}.jpg",
                                ContentType = "image/jpeg"
                            });
                            _logger.LogInformation("✅ 成功添加 {CameraType} 摄像头截图: IP={CameraIp}, 大小={Size} bytes", 
                                snapshot.CameraType, snapshot.CameraIp, snapshot.SizeBytes);
                        }
                        
                        // 记录失败的截图
                        foreach (var snapshot in siteSnapshotResult.Snapshots.Where(s => !s.Success))
                        {
                            _logger.LogWarning("⚠️ {CameraType} 摄像头截图失败: IP={CameraIp}, 错误={Error}", 
                                snapshot.CameraType, snapshot.CameraIp, snapshot.Error ?? "未知错误");
                        }
                        
                        if (attachments.Any())
                        {
                            _logger.LogInformation("✅ 站点 {SiteId} 共获取 {Count}/{Total} 个摄像头截图", 
                                alarmRecord.SiteId, siteSnapshotResult.SuccessfulSnapshots, siteSnapshotResult.TotalCameras);
                        }
                        else
                        {
                            _logger.LogInformation("ℹ️ 站点 {SiteId} 未获取到摄像头截图，将发送纯文字邮件", alarmRecord.SiteId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 获取站点 {SiteId} 摄像头截图失败，继续发送纯文字邮件", alarmRecord.SiteId);
            }

            // 4. 构建邮件内容
            string subject = $"【报警通知】{siteName} - {alarmRecord.AlarmName}";
            string body = BuildAlarmEmailBody(alarmRecord, siteName, attachments.Count);
            Console.WriteLine($"[AlarmNotificationService] 邮件主题: {subject}");
            Console.WriteLine($"[AlarmNotificationService] 附件数量: {attachments.Count}");

            // 5. 发送邮件（带多个附件）
            bool success = await _emailService.SendAlarmEmailWithAttachmentsAsync(
                recipientEmails,
                subject,
                body,
                attachments.Any() ? attachments : null
            );

            if (success)
            {
                _logger.LogInformation("✅ 报警通知邮件发送成功！收件人数: {Count}, 附件数: {AttachmentCount}", 
                    recipientEmails.Count, attachments.Count);
                Console.WriteLine("[AlarmNotificationService] 报警通知邮件发送成功");
            }
            else
            {
                _logger.LogError("❌ 报警通知邮件发送失败");
                Console.WriteLine("[AlarmNotificationService] 报警通知邮件发送失败");
            }

            // 6. 发送短信通知（获取负责该站点的用户手机号码）
            try
            {
                var recipientPhones = await GetSiteResponsibleUsersPhonesAsync(alarmRecord.SiteId);
                
                if (recipientPhones.Any())
                {
                    // 构建短信内容
                    string smsContent = BuildAlarmSmsContent(alarmRecord, siteName);
                    
                    // 合并手机号码（逗号分隔，去除空格）
                    string mobiles = string.Join(",", recipientPhones.Where(p => !string.IsNullOrWhiteSpace(p)));
                    
                    _logger.LogInformation("📱 开始发送报警短信: 号码数量={Count}, 号码列表={Mobiles}, 内容长度={Length}", 
                        recipientPhones.Count, mobiles, smsContent.Length);
                    Console.WriteLine($"[AlarmNotificationService] 开始发送报警短信，收件人数量: {recipientPhones.Count}, 号码: {mobiles}");
                    
                    // 发送短信（一次性发送给所有相关用户）
                    // 短信服务会将逗号分隔的手机号列表发送给所有用户
                    bool smsSuccess = await _smsService.SendSmsAsync(mobiles, smsContent);
                    
                    if (smsSuccess)
                    {
                        _logger.LogInformation("✅ 报警短信发送成功！收件人数: {Count}, 号码: {Mobiles}", 
                            recipientPhones.Count, mobiles);
                        Console.WriteLine($"[AlarmNotificationService] 报警短信发送成功，收件人: {mobiles}");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ 报警短信发送失败，号码: {Mobiles}", mobiles);
                        Console.WriteLine($"[AlarmNotificationService] 报警短信发送失败，号码: {mobiles}");
                    }
                }
                else
                {
                    _logger.LogInformation("ℹ️ 站点 {SiteId} 没有配置负责人手机号码，跳过短信发送", alarmRecord.SiteId);
                    Console.WriteLine($"[AlarmNotificationService] 站点 {alarmRecord.SiteId} 没有负责人手机号码，跳过短信发送");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 发送报警短信异常，继续处理其他通知");
                Console.WriteLine($"[AlarmNotificationService] 发送报警短信异常: {ex.Message}");
            }

            // 7. 发送语音通知（获取负责该站点的用户手机号码）
            try
            {
                var recipientPhones = await GetSiteResponsibleUsersPhonesAsync(alarmRecord.SiteId);
                
                if (recipientPhones.Any())
                {
                    // 构建语音内容（语音内容通常需要更简洁）
                    string voiceContent = BuildAlarmVoiceContent(alarmRecord, siteName);
                    
                    // 合并手机号码（逗号分隔，去除空格）
                    // 注意：语音通知最多支持30个号码
                    string mobiles = string.Join(",", recipientPhones.Where(p => !string.IsNullOrWhiteSpace(p)));
                    
                    _logger.LogInformation("📞 开始发送报警语音: 号码数量={Count}, 号码列表={Mobiles}, 内容长度={Length}", 
                        recipientPhones.Count, mobiles, voiceContent.Length);
                    Console.WriteLine($"[AlarmNotificationService] 开始发送报警语音，收件人数量: {recipientPhones.Count}, 号码: {mobiles}");
                    
                    // 发送语音通知（一次性发送给所有相关用户）
                    // 语音服务会将逗号分隔的手机号列表发送给所有用户（最多30个）
                    bool voiceSuccess = await _smsService.SendVoiceAsync(mobiles, voiceContent);
                    
                    if (voiceSuccess)
                    {
                        _logger.LogInformation("✅ 报警语音发送成功！收件人数: {Count}, 号码: {Mobiles}", 
                            recipientPhones.Count, mobiles);
                        Console.WriteLine($"[AlarmNotificationService] 报警语音发送成功，收件人: {mobiles}");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ 报警语音发送失败，号码: {Mobiles}", mobiles);
                        Console.WriteLine($"[AlarmNotificationService] 报警语音发送失败，号码: {mobiles}");
                    }
                }
                else
                {
                    _logger.LogInformation("ℹ️ 站点 {SiteId} 没有配置负责人手机号码，跳过语音发送", alarmRecord.SiteId);
                    Console.WriteLine($"[AlarmNotificationService] 站点 {alarmRecord.SiteId} 没有负责人手机号码，跳过语音发送");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 发送报警语音异常，继续处理其他通知");
                Console.WriteLine($"[AlarmNotificationService] 发送报警语音异常: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送报警通知时发生错误");
            Console.WriteLine($"[AlarmNotificationService] 发送报警通知失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取负责该站点的用户邮箱列表
    /// </summary>
    private async Task<List<string>> GetSiteResponsibleUsersEmailsAsync(int siteId)
    {
        try
        {
            // 通过 UserSite 关联表获取该站点的负责用户
            var userEmails = await _context.UserSites
                .Where(us => us.SiteId == siteId)
                .Include(us => us.User)
                .Where(us => us.User != null && !string.IsNullOrEmpty(us.User.Email))
                .Select(us => us.User!.Email!)
                .Distinct()
                .ToListAsync();

            _logger.LogInformation("📧 找到 {Count} 个负责站点 {SiteId} 的用户邮箱", userEmails.Count, siteId);
            return userEmails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取站点负责人邮箱失败");
            return new List<string>();
        }
    }

    /// <summary>
    /// 获取负责该站点的用户手机号码列表
    /// </summary>
    private async Task<List<string>> GetSiteResponsibleUsersPhonesAsync(int siteId)
    {
        try
        {
            // 通过 UserSite 关联表获取该站点的负责用户
            var userPhones = await _context.UserSites
                .Where(us => us.SiteId == siteId)
                .Include(us => us.User)
                .Where(us => us.User != null && !string.IsNullOrEmpty(us.User.Phone))
                .Select(us => new { 
                    Phone = us.User!.Phone!.Trim(),
                    Username = us.User.Username
                })
                .ToListAsync();

            // 过滤空值、去除空格、去重
            var validPhones = userPhones
                .Where(up => !string.IsNullOrWhiteSpace(up.Phone))
                .Select(up => up.Phone)
                .Distinct()
                .ToList();

            if (validPhones.Any())
            {
                _logger.LogInformation("📱 找到 {Count} 个负责站点 {SiteId} 的用户手机号码: {Phones}", 
                    validPhones.Count, siteId, string.Join(", ", validPhones));
                
                // 记录每个手机号对应的用户名（用于调试）
                var phoneUserMap = userPhones
                    .Where(up => !string.IsNullOrWhiteSpace(up.Phone))
                    .GroupBy(up => up.Phone.Trim())
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Username).ToList());
                
                foreach (var kvp in phoneUserMap)
                {
                    _logger.LogDebug("📱 手机号 {Phone} 对应用户: {Users}", kvp.Key, string.Join(", ", kvp.Value));
                }
            }
            else
            {
                _logger.LogWarning("⚠️ 站点 {SiteId} 没有找到有效的用户手机号码", siteId);
            }

            return validPhones;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取站点 {SiteId} 负责人手机号码失败", siteId);
            return new List<string>();
        }
    }

    /// <summary>
    /// 构建报警短信内容
    /// </summary>
    private string BuildAlarmSmsContent(AlarmRecord alarm, string siteName)
    {
        // 将枚举转换为中文显示
        string severityText = alarm.Severity switch
        {
            AlarmSeverity.Critical => "严重",
            AlarmSeverity.High => "高",
            AlarmSeverity.Medium => "中",
            AlarmSeverity.Low => "低",
            _ => "未知"
        };

        var sb = new StringBuilder();
        sb.Append($"【报警通知】");
        sb.Append($"站点：{siteName}，");
        sb.Append($"报警：{alarm.AlarmName}，");
        sb.Append($"严重程度：{severityText}，");
        sb.Append($"时间：{alarm.AlarmStartTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        
        if (!string.IsNullOrEmpty(alarm.AlarmDescription))
        {
            sb.Append($"，描述：{alarm.AlarmDescription}");
        }
        
        if (!string.IsNullOrEmpty(alarm.CurrentValue))
        {
            sb.Append($"，当前值：{alarm.CurrentValue} {alarm.Unit}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 构建报警语音内容（语音内容需要更简洁，适合语音播放）
    /// </summary>
    private string BuildAlarmVoiceContent(AlarmRecord alarm, string siteName)
    {
        // 将枚举转换为中文显示
        string severityText = alarm.Severity switch
        {
            AlarmSeverity.Critical => "严重",
            AlarmSeverity.High => "高",
            AlarmSeverity.Medium => "中",
            AlarmSeverity.Low => "低",
            _ => "未知"
        };

        // 语音内容需要简洁明了，适合语音播放
        // 格式：报警通知，站点名称，报警名称，严重程度
        var sb = new StringBuilder();
        sb.Append($"报警通知。");
        sb.Append($"站点：{siteName}。");
        sb.Append($"报警：{alarm.AlarmName}。");
        sb.Append($"严重程度：{severityText}。");
        sb.Append($"时间：{alarm.AlarmStartTime.ToLocalTime():yyyy年MM月dd日HH点mm分}。");
        
        // 语音内容可以包含简要描述，但不要太长
        if (!string.IsNullOrEmpty(alarm.AlarmDescription) && alarm.AlarmDescription.Length <= 50)
        {
            sb.Append($"描述：{alarm.AlarmDescription}。");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 构建报警邮件HTML内容
    /// </summary>
    private string BuildAlarmEmailBody(AlarmRecord alarm, string siteName, int attachmentCount = 0)
    {
        var sb = new StringBuilder();
        
        // 将枚举转换为中文显示
        string severityText = alarm.Severity switch
        {
            AlarmSeverity.Critical => "严重",
            AlarmSeverity.High => "高",
            AlarmSeverity.Medium => "中",
            AlarmSeverity.Low => "低",
            _ => "未知"
        };
        
        string severityClass = alarm.Severity switch
        {
            AlarmSeverity.Critical => "critical",
            AlarmSeverity.High => "critical",
            AlarmSeverity.Medium => "warning",
            AlarmSeverity.Low => "info",
            _ => "info"
        };
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='zh-CN'>");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset='UTF-8'>");
        sb.AppendLine("  <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        sb.AppendLine("  <title>报警通知</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: 'Microsoft YaHei', Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }");
        sb.AppendLine("    .container { max-width: 600px; margin: 0 auto; background: white; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); overflow: hidden; }");
        sb.AppendLine("    .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }");
        sb.AppendLine("    .header h1 { margin: 0; font-size: 24px; }");
        sb.AppendLine("    .header .alarm-icon { font-size: 48px; margin-bottom: 10px; }");
        sb.AppendLine("    .content { padding: 30px; }");
        sb.AppendLine("    .info-row { margin-bottom: 15px; padding: 12px; background: #f8f9fa; border-radius: 4px; border-left: 3px solid #667eea; }");
        sb.AppendLine("    .info-label { font-weight: bold; color: #333; display: inline-block; min-width: 100px; }");
        sb.AppendLine("    .info-value { color: #666; }");
        sb.AppendLine("    .severity { display: inline-block; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: bold; }");
        sb.AppendLine("    .severity-critical { background: #fee; color: #c00; }");
        sb.AppendLine("    .severity-warning { background: #ffc; color: #c60; }");
        sb.AppendLine("    .severity-info { background: #e6f2ff; color: #0066cc; }");
        sb.AppendLine("    .footer { padding: 20px; text-align: center; background: #f8f9fa; color: #999; font-size: 12px; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class='container'>");
        sb.AppendLine("    <div class='header'>");
        sb.AppendLine("      <div class='alarm-icon'>⚠️</div>");
        sb.AppendLine("      <h1>报警通知</h1>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class='content'>");
        sb.AppendLine($"      <div class='info-row'>");
        sb.AppendLine($"        <span class='info-label'>站点名称：</span>");
        sb.AppendLine($"        <span class='info-value'>{siteName}</span>");
        sb.AppendLine($"      </div>");
        sb.AppendLine($"      <div class='info-row'>");
        sb.AppendLine($"        <span class='info-label'>报警名称：</span>");
        sb.AppendLine($"        <span class='info-value'>{alarm.AlarmName}</span>");
        sb.AppendLine($"      </div>");
        sb.AppendLine($"      <div class='info-row'>");
        sb.AppendLine($"        <span class='info-label'>严重程度：</span>");
        sb.AppendLine($"        <span class='severity severity-{severityClass}'>{severityText}</span>");
        sb.AppendLine($"      </div>");
        sb.AppendLine($"      <div class='info-row'>");
        sb.AppendLine($"        <span class='info-label'>报警时间：</span>");
        sb.AppendLine($"        <span class='info-value'>{alarm.AlarmStartTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}</span>");
        sb.AppendLine($"      </div>");
        
        if (!string.IsNullOrEmpty(alarm.AlarmDescription))
        {
            sb.AppendLine($"      <div class='info-row'>");
            sb.AppendLine($"        <span class='info-label'>报警描述：</span>");
            sb.AppendLine($"        <span class='info-value'>{alarm.AlarmDescription}</span>");
            sb.AppendLine($"      </div>");
        }
        
        if (!string.IsNullOrEmpty(alarm.CurrentValue))
        {
            sb.AppendLine($"      <div class='info-row'>");
            sb.AppendLine($"        <span class='info-label'>当前值：</span>");
            sb.AppendLine($"        <span class='info-value'>{alarm.CurrentValue} {alarm.Unit}</span>");
            sb.AppendLine($"      </div>");
        }
        
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div class='footer'>");
        
        if (attachmentCount > 0)
        {
            sb.AppendLine($"      <p>📷 本邮件包含 {attachmentCount} 张现场摄像头截图，请查看附件。</p>");
        }
        
        sb.AppendLine("      <p>此邮件由泵房自动化系统自动发送，请勿直接回复。</p>");
        sb.AppendLine($"      <p>发送时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }
}

