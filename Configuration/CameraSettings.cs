namespace PumpRoomAutomationBackend.Configuration;

/// <summary>
/// 摄像头配置设置
/// Camera Configuration Settings
/// </summary>
public class CameraSettings
{
    public const string SectionName = "CameraSettings";
    
    /// <summary>
    /// 摄像头 IP 地址
    /// </summary>
    public string Ip { get; set; } = "192.168.30.102";
    
    /// <summary>
    /// 摄像头用户名
    /// </summary>
    public string Username { get; set; } = "admin";
    
    /// <summary>
    /// 摄像头密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// RTSP 端口
    /// </summary>
    public int RtspPort { get; set; } = 554;
    
    /// <summary>
    /// HTTP 端口
    /// </summary>
    public int HttpPort { get; set; } = 80;
}

