using Opc.Ua;

namespace PumpRoomAutomationBackend.Models.OpcUa;

/// <summary>
/// OPC UA 节点信息
/// OPC UA Node Information
/// </summary>
public class NodeInfo
{
    /// <summary>
    /// 节点ID
    /// </summary>
    public string NodeId { get; set; } = string.Empty;
    
    /// <summary>
    /// 浏览名称
    /// </summary>
    public string BrowseName { get; set; } = string.Empty;
    
    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 节点类型
    /// </summary>
    public NodeClass NodeClass { get; set; }
    
    /// <summary>
    /// 是否有值
    /// </summary>
    public bool HasValue { get; set; }
}

