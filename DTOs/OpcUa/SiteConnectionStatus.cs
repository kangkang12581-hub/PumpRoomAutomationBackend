namespace PumpRoomAutomationBackend.DTOs.OpcUa;

/// <summary>
/// 站点连接状态
/// </summary>
public class SiteConnectionStatus
{
    public string SiteCode { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public DateTime? LastConnectTime { get; set; }
    public DateTime? LastDisconnectTime { get; set; }
}

