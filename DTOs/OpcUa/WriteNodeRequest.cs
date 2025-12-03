namespace PumpRoomAutomationBackend.DTOs.OpcUa;

/// <summary>
/// 写入节点请求
/// </summary>
public class WriteNodeRequest
{
    public string NodeId { get; set; } = string.Empty;
    public string? Type { get; set; }
    public object? Value { get; set; }
}

