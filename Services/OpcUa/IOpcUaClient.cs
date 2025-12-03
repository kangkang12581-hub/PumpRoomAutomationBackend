using Opc.Ua;
using Opc.Ua.Client;
using PumpRoomAutomationBackend.Models.OpcUa;

namespace PumpRoomAutomationBackend.Services.OpcUa;

/// <summary>
/// OPC UA 客户端接口
/// OPC UA Client Interface
/// </summary>
public interface IOpcUaClient
{
    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// 读取节点值
    /// </summary>
    Task<DataValue?> ReadValueAsync(string nodeId);
    
    /// <summary>
    /// 写入节点值
    /// </summary>
    Task<StatusCode> WriteValueAsync(string nodeId, object value);
    
    /// <summary>
    /// 浏览节点
    /// </summary>
    Task<IEnumerable<NodeInfo>> BrowseNodesAsync(string? nodeId = null);
    
    /// <summary>
    /// 附加会话
    /// </summary>
    void AttachSession(Session session);
}

