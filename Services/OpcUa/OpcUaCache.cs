using PumpRoomAutomationBackend.Models.OpcUa;

namespace PumpRoomAutomationBackend.Services.OpcUa;

/// <summary>
/// OPC UA 缓存接口
/// OPC UA Cache Interface
/// </summary>
public interface IOpcUaCache
{
    /// <summary>
    /// 缓存锁
    /// </summary>
    object CacheLock { get; }
    
    /// <summary>
    /// 节点缓存
    /// </summary>
    Dictionary<string, NodeSnapshot> NodeCache { get; }
    
    /// <summary>
    /// PLC 数据映射
    /// </summary>
    Dictionary<string, string> PlcDataMap { get; }
}

/// <summary>
/// OPC UA 缓存服务
/// OPC UA Cache Service
/// </summary>
public class OpcUaCache : IOpcUaCache
{
    public object CacheLock { get; } = new object();
    public Dictionary<string, NodeSnapshot> NodeCache { get; } = new Dictionary<string, NodeSnapshot>();
    public Dictionary<string, string> PlcDataMap { get; } = new Dictionary<string, string>();
}

