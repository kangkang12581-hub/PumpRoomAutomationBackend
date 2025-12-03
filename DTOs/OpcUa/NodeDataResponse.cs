namespace PumpRoomAutomationBackend.DTOs.OpcUa;

/// <summary>
/// 节点数据响应
/// Node Data Response
/// </summary>
public class NodeDataResponse
{
    public string NodeId { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Timestamp { get; set; }
    public string? Type { get; set; }
}

