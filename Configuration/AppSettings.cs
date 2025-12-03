namespace PumpRoomAutomationBackend.Configuration;

/// <summary>
/// 应用程序配置设置
/// Application Configuration Settings
/// </summary>
public class AppSettings
{
    public const string SectionName = "AppSettings";
    
    /// <summary>
    /// 应用程序名称
    /// </summary>
    public string AppName { get; set; } = "泵房自动化系统";
    
    /// <summary>
    /// 应用程序版本
    /// </summary>
    public string AppVersion { get; set; } = "1.0.0";
}

