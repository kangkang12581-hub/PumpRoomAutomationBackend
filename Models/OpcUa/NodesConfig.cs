namespace PumpRoomAutomationBackend.Models.OpcUa;

/// <summary>
/// 节点配置
/// Nodes Configuration
/// </summary>
public class NodesConfig
{
    /// <summary>
    /// PLC 数据节点映射
    /// </summary>
    public Dictionary<string, string>? PlcData { get; set; }
}

