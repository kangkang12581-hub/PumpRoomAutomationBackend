namespace PumpRoomAutomationBackend.Models.OpcUa;

/// <summary>
/// OPC UA 节点快照
/// OPC UA Node Snapshot
/// </summary>
public class NodeSnapshot
{
    /// <summary>
    /// 节点值
    /// </summary>
    public object? Value { get; set; }
    
    /// <summary>
    /// 状态码
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public string Timestamp { get; set; } = string.Empty;
    
    /// <summary>
    /// 数据类型
    /// </summary>
    public string? Type { get; set; }
}

