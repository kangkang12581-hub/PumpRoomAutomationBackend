using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using PumpRoomAutomationBackend.Models.OpcUa;

namespace PumpRoomAutomationBackend.Services.OpcUa;

/// <summary>
/// 站点专用 OPC UA 客户端
/// Site-specific OPC UA Client
/// </summary>
public class SiteOpcUaClient : IOpcUaClient, IDisposable
{
    private readonly ILogger<SiteOpcUaClient> _logger;
    private readonly SiteOpcUaConnection _config;
    private Session? _session;
    private ApplicationConfiguration? _appConfig;
    private bool _disposed;
    
    public string SiteCode => _config.SiteCode;
    public string SiteName => _config.SiteName;
    public string Endpoint => _config.Endpoint;
    public bool IsConnected => _session?.Connected ?? false;
    public DateTime? LastConnectTime { get; private set; }
    public DateTime? LastDisconnectTime { get; private set; }
    
    public SiteOpcUaClient(SiteOpcUaConnection config, ILogger<SiteOpcUaClient> logger)
    {
        _config = config;
        _logger = logger;
    }
    
    /// <summary>
    /// 连接到 OPC UA 服务器
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        try
        {
            _logger.LogInformation("🔌 [{SiteCode}] 开始连接到 OPC UA 服务器: {Endpoint}", 
                _config.SiteCode, _config.Endpoint);
            
            // 创建应用程序配置
            _appConfig = new ApplicationConfiguration
            {
                ApplicationName = $"PumpRoomClient_{_config.SiteCode}",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier(),
                    AutoAcceptUntrustedCertificates = true,
                    RejectSHA1SignedCertificates = false
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = _config.RequestTimeout },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = _config.SessionTimeout }
            };
            
            await _appConfig.Validate(ApplicationType.Client);
            
            // 选择端点
            var endpointDescription = CoreClientUtils.SelectEndpoint(_config.Endpoint, false);
            var endpointConfiguration = EndpointConfiguration.Create(_appConfig);
            var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);
            
            // 创建会话
            var userIdentity = _config.Anonymous 
                ? new UserIdentity(new AnonymousIdentityToken())
                : new UserIdentity(_config.Username, _config.Password);
            
            _session = await Session.Create(
                _appConfig,
                endpoint,
                false,
                $"PumpRoomSession_{_config.SiteCode}",
                (uint)_config.SessionTimeout,
                userIdentity,
                null
            );
            
            if (_session != null && _session.Connected)
            {
                LastConnectTime = DateTime.UtcNow;
                _logger.LogInformation("✅ [{SiteCode}] OPC UA 连接成功: {Endpoint}", 
                    _config.SiteCode, _config.Endpoint);
                return true;
            }
            
            _logger.LogWarning("⚠️ [{SiteCode}] OPC UA 连接失败", _config.SiteCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [{SiteCode}] OPC UA 连接异常: {Message}", 
                _config.SiteCode, ex.Message);
            return false;
        }
    }
    
    /// <summary>
    /// 断开连接
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            if (_session != null)
            {
                await Task.Run(() =>
                {
                    _session.Close();
                    _session.Dispose();
                });
                
                LastDisconnectTime = DateTime.UtcNow;
                _logger.LogInformation("🔌 [{SiteCode}] OPC UA 连接已断开", _config.SiteCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [{SiteCode}] 断开连接时发生错误", _config.SiteCode);
        }
        finally
        {
            _session = null;
        }
    }
    
    /// <summary>
    /// 读取节点值
    /// </summary>
    public async Task<DataValue?> ReadValueAsync(string nodeId)
    {
        if (_session == null || !_session.Connected)
        {
            _logger.LogWarning("⚠️ [{SiteCode}] 会话未连接，无法读取节点: {NodeId}", 
                _config.SiteCode, nodeId);
            return null;
        }
        
        try
        {
            var readId = new ReadValueId
            {
                NodeId = NodeId.Parse(nodeId),
                AttributeId = Attributes.Value
            };
            
            var nodes = new ReadValueIdCollection { readId };
            
            var result = await Task.Run(() =>
            {
                _session.Read(null, 0, TimestampsToReturn.Both, nodes, out DataValueCollection results, out _);
                return results.Count > 0 ? results[0] : null;
            });
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [{SiteCode}] 读取节点失败: {NodeId}", 
                _config.SiteCode, nodeId);
            return null;
        }
    }
    
    /// <summary>
    /// 批量读取节点值
    /// </summary>
    public async Task<Dictionary<string, DataValue?>> ReadValuesAsync(IEnumerable<string> nodeIds)
    {
        var result = new Dictionary<string, DataValue?>();
        
        if (_session == null || !_session.Connected)
        {
            _logger.LogWarning("⚠️ [{SiteCode}] 会话未连接，无法批量读取", _config.SiteCode);
            return result;
        }
        
        try
        {
            var nodes = new ReadValueIdCollection();
            var nodeIdList = nodeIds.ToList();
            
            foreach (var nodeId in nodeIdList)
            {
                nodes.Add(new ReadValueId
                {
                    NodeId = NodeId.Parse(nodeId),
                    AttributeId = Attributes.Value
                });
            }
            
            var results = await Task.Run(() =>
            {
                _session.Read(null, 0, TimestampsToReturn.Both, nodes, out DataValueCollection dataValues, out _);
                return dataValues;
            });
            
            for (int i = 0; i < nodeIdList.Count && i < results.Count; i++)
            {
                result[nodeIdList[i]] = results[i];
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [{SiteCode}] 批量读取失败", _config.SiteCode);
        }
        
        return result;
    }
    
    /// <summary>
    /// 写入节点值
    /// </summary>
    public async Task<StatusCode> WriteValueAsync(string nodeId, object value)
    {
        if (_session == null || !_session.Connected)
        {
            _logger.LogWarning("⚠️ [{SiteCode}] 会话未连接，无法写入节点: {NodeId}", 
                _config.SiteCode, nodeId);
            return new StatusCode(Opc.Ua.StatusCodes.BadSessionIdInvalid);
        }
        
        try
        {
            var write = new WriteValue
            {
                NodeId = NodeId.Parse(nodeId),
                AttributeId = Attributes.Value,
                Value = new DataValue(new Variant(value))
            };
            
            var collection = new WriteValueCollection { write };
            
            var statusCode = await Task.Run(() =>
            {
                _session.Write(null, collection, out StatusCodeCollection results, out _);
                return results.Count > 0 ? results[0] : new StatusCode(Opc.Ua.StatusCodes.BadUnexpectedError);
            });
            
            if (StatusCode.IsGood(statusCode))
            {
                _logger.LogDebug("✅ [{SiteCode}] 写入节点成功: {NodeId} = {Value}", 
                    _config.SiteCode, nodeId, value);
            }
            else
            {
                _logger.LogWarning("⚠️ [{SiteCode}] 写入节点失败: {NodeId}, Status: {Status}", 
                    _config.SiteCode, nodeId, statusCode);
            }
            
            return statusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ [{SiteCode}] 写入节点异常: {NodeId}", 
                _config.SiteCode, nodeId);
            return new StatusCode(Opc.Ua.StatusCodes.BadUnexpectedError);
        }
    }
    
    /// <summary>
    /// 浏览节点
    /// </summary>
    public Task<IEnumerable<NodeInfo>> BrowseNodesAsync(string? nodeId = null)
    {
        if (_session == null || !_session.Connected)
        {
            _logger.LogWarning("⚠️ [{SiteCode}] 会话未连接，无法浏览节点", _config.SiteCode);
            return Task.FromResult<IEnumerable<NodeInfo>>(new List<NodeInfo>());
        }
        
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
                        _logger.LogWarning(ex, "⚠️ [{SiteCode}] 获取子节点信息失败", _config.SiteCode);
                    }
                }
            }
            
            return Task.FromResult<IEnumerable<NodeInfo>>(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [{SiteCode}] 浏览节点失败: {NodeId}", _config.SiteCode, nodeId ?? "Root");
            return Task.FromResult<IEnumerable<NodeInfo>>(new List<NodeInfo>());
        }
    }
    
    /// <summary>
    /// 附加会话（用于兼容IOpcUaClient接口，但SiteOpcUaClient管理自己的会话）
    /// </summary>
    public void AttachSession(Session session)
    {
        // SiteOpcUaClient管理自己的会话，此方法仅用于接口兼容性
        // 如果外部尝试附加会话，我们记录警告但不执行操作
        _logger.LogWarning("⚠️ [{SiteCode}] AttachSession被调用，但SiteOpcUaClient管理自己的会话，忽略此调用", _config.SiteCode);
    }
    
    /// <summary>
    /// 检查连接状态并尝试重连
    /// </summary>
    public async Task<bool> EnsureConnectedAsync()
    {
        if (IsConnected)
            return true;
        
        _logger.LogInformation("🔄 [{SiteCode}] 尝试重新连接...", _config.SiteCode);
        return await ConnectAsync();
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        DisconnectAsync().Wait();
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }
}

