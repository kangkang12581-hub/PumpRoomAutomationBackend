using System.Text.Json;
using PumpRoomAutomationBackend.Models.OpcUa;

namespace PumpRoomAutomationBackend.Services.OpcUa;

/// <summary>
/// 多站点 OPC UA 后台服务
/// Multi-site OPC UA Hosted Service
/// </summary>
public class OpcUaHostedServiceMulti : BackgroundService
{
    private readonly IOpcUaConnectionManager _connectionManager;
    private readonly IOpcUaCache _cache;
    private readonly ILogger<OpcUaHostedServiceMulti> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    public OpcUaHostedServiceMulti(
        IOpcUaConnectionManager connectionManager,
        IOpcUaCache cache,
        ILogger<OpcUaHostedServiceMulti> logger,
        IServiceProvider serviceProvider)
    {
        _connectionManager = connectionManager;
        _cache = cache;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("🚀 启动多站点 OPC UA 后台服务...");
            
            // 加载节点配置
            LoadNodesConfig();
            
            // 初始化所有站点连接
            await _connectionManager.InitializeAsync();
            
            // 启动定时任务
            await StartPeriodicTasksAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 多站点 OPC UA 后台服务启动失败");
        }
    }
    
    private void LoadNodesConfig()
    {
        try
        {
            const string configPath = "nodes.json";
            if (!File.Exists(configPath))
            {
                _logger.LogWarning("⚠️  节点配置文件不存在: {Path}", configPath);
                return;
            }
            
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<NodesConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            if (config?.PlcData != null)
            {
                lock (_cache.CacheLock)
                {
                    _cache.PlcDataMap.Clear();
                    foreach (var kv in config.PlcData)
                    {
                        _cache.PlcDataMap[kv.Key] = kv.Value;
                    }
                }
                _logger.LogInformation("✅ 加载节点配置成功，共 {Count} 个节点", config.PlcData.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️  加载节点配置失败");
        }
    }
    
    private async Task StartPeriodicTasksAsync(CancellationToken stoppingToken)
    {
        // 任务1：数据采集（每10秒）
        var dataPollingTask = Task.Run(async () =>
        {
            const int pollingInterval = 10000; // 10秒
            _logger.LogInformation("📊 启动数据轮询任务，间隔 {Interval}ms", pollingInterval);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollAllSitesDataAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 数据轮询异常");
                }
                
                await Task.Delay(pollingInterval, stoppingToken);
            }
        }, stoppingToken);
        
        // 任务2：连接状态检查和自动重连（每30秒）
        var connectionCheckTask = Task.Run(async () =>
        {
            const int checkInterval = 30000; // 30秒
            _logger.LogInformation("🔍 启动连接检查任务，间隔 {Interval}ms", checkInterval);
            
            // 首次延迟30秒后开始
            await Task.Delay(checkInterval, stoppingToken);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndReconnectSitesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 连接检查异常");
                }
                
                await Task.Delay(checkInterval, stoppingToken);
            }
        }, stoppingToken);
        
        // 等待所有任务完成或取消
        await Task.WhenAny(dataPollingTask, connectionCheckTask);
    }
    
    /// <summary>
    /// 轮询所有站点的数据
    /// </summary>
    private async Task PollAllSitesDataAsync()
    {
        var connectionStatus = _connectionManager.GetAllConnectionStatus();
        
        List<string> nodeIds;
        lock (_cache.CacheLock)
        {
            nodeIds = new List<string>(_cache.PlcDataMap.Values);
        }
        
        if (nodeIds.Count == 0)
        {
            return;
        }
        
        // 并发从所有站点读取数据
        var pollTasks = connectionStatus
            .Where(kvp => kvp.Value) // 只读取已连接的站点
            .Select(async kvp =>
            {
                var siteCode = kvp.Key;
                var client = _connectionManager.GetClient(siteCode);
                
                if (client == null || !client.IsConnected)
                    return;
                
                try
                {
                    // 批量读取节点
                    var results = await client.ReadValuesAsync(nodeIds);
                    
                    // 更新缓存
                    lock (_cache.CacheLock)
                    {
                        foreach (var (nodeId, dataValue) in results)
                        {
                            if (dataValue == null)
                                continue;
                            
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
                    
                    _logger.LogDebug("📊 [{SiteCode}] 读取 {Count} 个节点成功", 
                        siteCode, results.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️  [{SiteCode}] 数据轮询失败", siteCode);
                }
            });
        
        await Task.WhenAll(pollTasks);
    }
    
    /// <summary>
    /// 检查并重连断开的站点
    /// </summary>
    private async Task CheckAndReconnectSitesAsync()
    {
        var connectionStatus = _connectionManager.GetAllConnectionStatus();
        
        var disconnectedSites = connectionStatus
            .Where(kvp => !kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();
        
        if (disconnectedSites.Count == 0)
        {
            _logger.LogDebug("✅ 所有站点连接正常");
            return;
        }
        
        _logger.LogWarning("⚠️  发现 {Count} 个站点断开连接，尝试重连...", disconnectedSites.Count);
        
        var reconnectTasks = disconnectedSites.Select(async siteCode =>
        {
            try
            {
                var client = _connectionManager.GetClient(siteCode);
                if (client != null)
                {
                    var reconnected = await client.EnsureConnectedAsync();
                    if (reconnected)
                    {
                        _logger.LogInformation("✅ [{SiteCode}] 重连成功", siteCode);
                    }
                    else
                    {
                        _logger.LogWarning("❌ [{SiteCode}] 重连失败", siteCode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [{SiteCode}] 重连异常", siteCode);
            }
        });
        
        await Task.WhenAll(reconnectTasks);
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 停止多站点 OPC UA 后台服务...");
        
        await _connectionManager.DisconnectAllAsync();
        
        await base.StopAsync(cancellationToken);
        
        _logger.LogInformation("✅ 多站点 OPC UA 后台服务已停止");
    }
}

