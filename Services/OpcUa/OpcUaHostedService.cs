using System.Text.Json;
using PumpRoomAutomationBackend.Models.OpcUa;

namespace PumpRoomAutomationBackend.Services.OpcUa;

/// <summary>
/// OPC UA 后台服务（多站点架构）
/// OPC UA Hosted Service (Multi-site Architecture)
/// </summary>
public class OpcUaHostedService : BackgroundService
{
    private readonly IOpcUaCache _cache;
    private readonly IOpcUaConnectionManager _connectionManager;
    private readonly ILogger<OpcUaHostedService> _logger;
    private Dictionary<string, string> _nodeMap = new();
    
    public OpcUaHostedService(
        IOpcUaCache cache,
        IOpcUaConnectionManager connectionManager,
        ILogger<OpcUaHostedService> logger)
    {
        _cache = cache;
        _connectionManager = connectionManager;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("🚀 启动 OPC UA 多站点后台服务...");
            
            // 加载节点配置
            LoadNodesConfig();
            
            // 初始化并连接所有站点
            await _connectionManager.InitializeAsync();
            
            // 开始定期读取所有站点的数据
            _ = Task.Run(async () => await StartMultiSitePollingAsync(stoppingToken), stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ OPC UA 后台服务启动失败");
        }
    }
    
    private void LoadNodesConfig()
    {
        try
        {
            const string configPath = "nodes.json";
            if (!File.Exists(configPath))
            {
                _logger.LogWarning("⚠️ 节点配置文件不存在: {Path}", configPath);
                return;
            }
            
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<NodesConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (config?.PlcData != null)
            {
                _nodeMap = new Dictionary<string, string>(config.PlcData);
                _logger.LogInformation("✅ 加载节点配置成功，共 {Count} 个节点", config.PlcData.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 加载节点配置失败");
        }
    }
    
    /// <summary>
    /// 多站点数据轮询
    /// </summary>
    private async Task StartMultiSitePollingAsync(CancellationToken token)
    {
        const int pollingInterval = 3000; // 3秒
        _logger.LogInformation("📊 开始多站点数据轮询，间隔 {Interval}ms", pollingInterval);
        
        while (!token.IsCancellationRequested)
        {
            try
            {
                // 获取所有连接状态
                var allStatus = _connectionManager.GetAllConnectionStatus();
                
                foreach (var (siteCode, isConnected) in allStatus)
                {
                    if (!isConnected)
                    {
                        _logger.LogDebug("⏭️ 跳过未连接的站点: {SiteCode}", siteCode);
                        continue;
                    }
                    
                    var client = _connectionManager.GetClient(siteCode);
                    if (client == null)
                    {
                        continue;
                    }
                    
                    // 读取该站点的所有节点
                    await ReadSiteNodesAsync(siteCode, client);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ 多站点数据轮询错误");
            }
            
            await Task.Delay(pollingInterval, token);
        }
    }
    
    /// <summary>
    /// 读取单个站点的所有节点数据
    /// </summary>
    private async Task ReadSiteNodesAsync(string siteCode, SiteOpcUaClient client)
    {
        try
        {
            if (_nodeMap.Count == 0)
            {
                return;
            }
            
            // 批量读取所有节点
            var nodeIds = _nodeMap.Values.ToList();
            var results = await client.ReadValuesAsync(nodeIds);
            
            // 更新缓存
            lock (_cache.CacheLock)
            {
                foreach (var (nodeId, dataValue) in results)
                {
                    if (dataValue == null)
                        continue;
                    
                    // 缓存键格式: {siteCode}:{nodeId}
                    var cacheKey = $"{siteCode}:{nodeId}";
                    
                    var snapshot = new NodeSnapshot
                    {
                        Value = dataValue.Value,
                        Status = dataValue.StatusCode.ToString(),
                        Timestamp = dataValue.SourceTimestamp.ToLocalTime().ToString("O"),
                        Type = dataValue.Value?.GetType()?.Name
                    };
                    
                    _cache.NodeCache[cacheKey] = snapshot;
                }
            }
            
            _logger.LogDebug("✅ [{SiteCode}] 更新 {Count} 个节点到缓存", siteCode, results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ [{SiteCode}] 读取节点数据失败", siteCode);
        }
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 停止 OPC UA 多站点后台服务...");
        
        // 断开所有站点连接
        await _connectionManager.DisconnectAllAsync();
        
        await base.StopAsync(cancellationToken);
    }
}

