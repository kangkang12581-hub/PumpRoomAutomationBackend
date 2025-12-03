namespace PumpRoomAutomationBackend.DTOs.OpcUa;

/// <summary>
/// 写入节点响应
/// </summary>
public class WriteNodeResponse
{
    public bool Success { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

