namespace PumpRoomAutomationBackend.DTOs.Site;

/// <summary>
/// 站点连接状态
/// Site Connection Status
/// </summary>
public class SiteConnectionStatusDto
{
    public string SiteCode { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string ConnectionStatus { get; set; } = "disconnected";
    public DateTime? LastConnectTime { get; set; }
    public DateTime? LastDisconnectTime { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public string Endpoint { get; set; } = string.Empty;
}

