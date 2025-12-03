using Opc.Ua;
using Opc.Ua.Client;
using PumpRoomAutomationBackend.Models.OpcUa;

namespace PumpRoomAutomationBackend.Services.OpcUa;

/// <summary>
/// OPC UA 客户端服务
/// OPC UA Client Service
/// </summary>
public class OpcUaClientService : IOpcUaClient
{
    private readonly ILogger<OpcUaClientService> _logger;
    private Session? _session;
    
    public OpcUaClientService(ILogger<OpcUaClientService> logger)
    {
        _logger = logger;
    }
    
    public bool IsConnected => _session != null && _session.Connected;
    
    public void AttachSession(Session session)
    {
        _session = session;
    }
    
    public Task<DataValue?> ReadValueAsync(string nodeId)
    {
        if (_session == null)
            return Task.FromResult<DataValue?>(null);
        
        try
        {
            var readId = new ReadValueId
            {
                NodeId = NodeId.Parse(nodeId),
                AttributeId = Attributes.Value
            };
            var nodes = new ReadValueIdCollection { readId };
            _session.Read(null, 0, TimestampsToReturn.Source, nodes, out DataValueCollection results, out _);
            return Task.FromResult<DataValue?>(results.Count > 0 ? results[0] : null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取节点失败 {NodeId}", nodeId);
            return Task.FromResult<DataValue?>(null);
        }
    }
    
    public Task<StatusCode> WriteValueAsync(string nodeId, object value)
    {
        if (_session == null)
            return Task.FromResult(new StatusCode(Opc.Ua.StatusCodes.BadSessionIdInvalid));
        
        try
        {
            var write = new WriteValue
            {
                NodeId = NodeId.Parse(nodeId),
                AttributeId = Attributes.Value,
                Value = new DataValue(new Variant(value))
            };
            var collection = new WriteValueCollection { write };
            _session.Write(null, collection, out StatusCodeCollection results, out _);
            return Task.FromResult(results.Count > 0 ? results[0] : new StatusCode(Opc.Ua.StatusCodes.BadUnexpectedError));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "写入节点失败 {NodeId}", nodeId);
            return Task.FromResult(new StatusCode(Opc.Ua.StatusCodes.BadUnexpectedError));
        }
    }
    
    public Task<IEnumerable<NodeInfo>> BrowseNodesAsync(string? nodeId = null)
    {
        if (_session == null)
            return Task.FromResult<IEnumerable<NodeInfo>>(new List<NodeInfo>());
        
        try
        {
            var node = string.IsNullOrWhiteSpace(nodeId)
                ? _session.NodeCache.Find(Objects.ObjectsFolder)
                : _session.NodeCache.Find(nodeId);
            
            if (node == null)
                return Task.FromResult<IEnumerable<NodeInfo>>(new List<NodeInfo>());
            
            var result = new List<NodeInfo>();
            
            var browseDescription = new BrowseDescription
            {
                NodeId = (NodeId)node.NodeId,
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                IncludeSubtypes = true,
                NodeClassMask = 0,
                ResultMask = (uint)BrowseResultMask.All
            };
            
            var browseCollection = new BrowseDescriptionCollection { browseDescription };
            _session.Browse(null, null, 0, browseCollection, out BrowseResultCollection browseResults, out _);
            
            if (browseResults.Count > 0 && browseResults[0].References != null)
            {
                foreach (var reference in browseResults[0].References)
                {
                    try
                    {
                        var nodeInfo = new NodeInfo
                        {
                            NodeId = reference.NodeId.ToString(),
                            BrowseName = reference.BrowseName.Name,
                            DisplayName = reference.DisplayName.Text,
                            NodeClass = reference.NodeClass,
                            HasValue = reference.NodeClass == NodeClass.Variable
                        };
                        result.Add(nodeInfo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "获取子节点信息失败");
                    }
                }
            }
            
            return Task.FromResult<IEnumerable<NodeInfo>>(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "浏览节点失败 {NodeId}", nodeId ?? "Root");
            return Task.FromResult<IEnumerable<NodeInfo>>(new List<NodeInfo>());
        }
    }
}

