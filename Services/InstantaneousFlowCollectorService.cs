using Microsoft.Extensions.DependencyInjection;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Services.OpcUa;
using Microsoft.EntityFrameworkCore;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 瞬时流量数据采集后台服务
/// 每分钟自动从OPC UA读取流量数据并存储到数据库
/// </summary>
public class InstantaneousFlowCollectorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOpcUaCache _opcUaCache;
    private readonly IOpcUaConnectionManager _connectionManager;
    private readonly ILogger<InstantaneousFlowCollectorService> _logger;
    private readonly TimeSpan _collectionInterval = TimeSpan.FromMinutes(1);

    private const string FLOW_NODE_KEY = "actFlow";  // 瞬时流量节点

    public InstantaneousFlowCollectorService(
        IServiceProvider serviceProvider,
        IOpcUaCache opcUaCache,
        IOpcUaConnectionManager connectionManager,
        ILogger<InstantaneousFlowCollectorService> logger)
    {
        _serviceProvider = serviceProvider;
        _opcUaCache = opcUaCache;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 启动瞬时流量数据采集服务，采集间隔: {Interval}", _collectionInterval);

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectAndSaveDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 瞬时流量数据采集失败");
            }

            var now = DateTime.UtcNow;
            var nextMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0)
                .AddMinutes(1);
            var delay = nextMinute - now;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private async Task CollectAndSaveDataAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dataService = scope.ServiceProvider.GetRequiredService<IInstantaneousFlowService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var enabledSites = await dbContext.SiteConfigs
            .Where(s => s.IsEnabled)
            .Select(s => new { s.Id, s.SiteCode })
            .ToListAsync();

        if (!enabledSites.Any())
        {
            _logger.LogWarning("⚠️ 没有启用的站点");
            return;
        }

        var timestamp = DateTime.UtcNow;
        var successCount = 0;
        var failCount = 0;

        foreach (var site in enabledSites)
        {
            try
            {
                var client = _connectionManager.GetClient(site.SiteCode);
                var isConnected = client?.IsConnected ?? false;
                
                if (!isConnected)
                {
                    continue;
                }

                var flowRate = ReadFlowFromCache(site.SiteCode);
                
                if (!flowRate.HasValue)
                {
                    failCount++;
                    continue;
                }

                var request = new AddInstantaneousFlowRequest
                {
                    SiteId = site.Id,
                    Timestamp = timestamp,
                    FlowRate = flowRate.Value,
                    Status = DetermineStatus(flowRate.Value),
                    DataQuality = isConnected ? (short)100 : (short)0
                };

                await dataService.AddDataAsync(request);
                successCount++;

                _logger.LogInformation("✅ [{SiteCode}] 保存流量数据: {Flow:F3}m³/h @ {Time}", 
                    site.SiteCode, flowRate.Value, timestamp.ToLocalTime());
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("已存在"))
            {
                _logger.LogDebug("⏭️ [{SiteCode}] 该时间点数据已存在，跳过", site.SiteCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [{SiteCode}] 保存流量数据失败", site.SiteCode);
                failCount++;
            }
        }

        _logger.LogInformation("📊 瞬时流量数据采集完成: 成功 {Success}, 失败 {Fail}, 总计 {Total}", 
            successCount, failCount, enabledSites.Count);
    }

    private decimal? ReadFlowFromCache(string siteCode)
    {
        try
        {
            var cacheKey = $"{siteCode}:{GetNodeIdFromConfig(FLOW_NODE_KEY)}";
            
            lock (_opcUaCache.CacheLock)
            {
                if (_opcUaCache.NodeCache.TryGetValue(cacheKey, out var snapshot))
                {
                    if (snapshot?.Value != null && decimal.TryParse(snapshot.Value.ToString(), out var flow))
                    {
                        return flow;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取站点 {SiteCode} 的瞬时流量失败", siteCode);
            return null;
        }
    }

    private string GetNodeIdFromConfig(string nodeKey)
    {
        return nodeKey switch
        {
            FLOW_NODE_KEY => "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actFlow",
            _ => string.Empty
        };
    }

    private string DetermineStatus(decimal flowRate)
    {
        if (flowRate > 100.0m)
        {
            return "alarm";
        }
        else if (flowRate > 80.0m)
        {
            return "warning";
        }
        else if (flowRate < 0.1m)
        {
            return "warning";
        }
        else
        {
            return "normal";
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏹️ 停止瞬时流量数据采集服务");
        return base.StopAsync(cancellationToken);
    }
}

