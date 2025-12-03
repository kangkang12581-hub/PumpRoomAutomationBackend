using Microsoft.Extensions.DependencyInjection;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Services.OpcUa;
using Microsoft.EntityFrameworkCore;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 上游液位数据采集后台服务
/// 每分钟自动从OPC UA读取液位数据并存储到数据库
/// </summary>
public class UpstreamWaterLevelCollectorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOpcUaCache _opcUaCache;
    private readonly IOpcUaConnectionManager _connectionManager;
    private readonly ILogger<UpstreamWaterLevelCollectorService> _logger;
    private readonly TimeSpan _collectionInterval = TimeSpan.FromMinutes(1); // 每分钟采集一次

    // 液位节点配置（可以从配置文件读取）
    private const string UPSTREAM_LEVEL_NODE_KEY = "actLevel";  // 上游液位节点
    private const string DOPPLER_LEVEL_NODE_KEY = "actLevelDoppler";  // 多普勒液位（备用）

    public UpstreamWaterLevelCollectorService(
        IServiceProvider serviceProvider,
        IOpcUaCache opcUaCache,
        IOpcUaConnectionManager connectionManager,
        ILogger<UpstreamWaterLevelCollectorService> logger)
    {
        _serviceProvider = serviceProvider;
        _opcUaCache = opcUaCache;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 启动上游液位数据采集服务，采集间隔: {Interval}", _collectionInterval);

        // 等待5秒，确保OPC UA连接已建立
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectAndSaveDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 上游液位数据采集失败");
            }

            // 等待到下一个整分钟
            var now = DateTime.UtcNow;
            var nextMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0)
                .AddMinutes(1);
            var delay = nextMinute - now;

            if (delay > TimeSpan.Zero)
            {
                _logger.LogDebug("⏰ 下次采集时间: {NextTime}, 等待: {Delay:0.0}秒", 
                    nextMinute.ToLocalTime(), delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    /// <summary>
    /// 采集并保存所有站点的液位数据
    /// </summary>
    private async Task CollectAndSaveDataAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dataService = scope.ServiceProvider.GetRequiredService<IUpstreamWaterLevelService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 获取所有已启用的站点
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
                // 检查站点连接状态
                var client = _connectionManager.GetClient(site.SiteCode);
                var isConnected = client?.IsConnected ?? false;
                
                if (!isConnected)
                {
                    _logger.LogDebug("⏭️ 站点 {SiteCode} 未连接，跳过", site.SiteCode);
                    continue;
                }

                // 从缓存读取液位数据
                var waterLevel = ReadWaterLevelFromCache(site.SiteCode);
                
                if (!waterLevel.HasValue)
                {
                    _logger.LogWarning("⚠️ 站点 {SiteCode} 无法读取液位数据", site.SiteCode);
                    failCount++;
                    continue;
                }

                // 保存到数据库
                var request = new AddUpstreamWaterLevelRequest
                {
                    SiteId = site.Id,
                    Timestamp = timestamp,
                    WaterLevel = waterLevel.Value,
                    Status = DetermineStatus(waterLevel.Value),
                    DataQuality = isConnected ? (short)100 : (short)0
                };

                await dataService.AddDataAsync(request);
                successCount++;

                _logger.LogInformation("✅ [{SiteCode}] 保存液位数据: {Level:F3}m @ {Time}", 
                    site.SiteCode, waterLevel.Value, timestamp.ToLocalTime());
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("已存在"))
            {
                _logger.LogDebug("⏭️ [{SiteCode}] 该时间点数据已存在，跳过", site.SiteCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [{SiteCode}] 保存液位数据失败", site.SiteCode);
                failCount++;
            }
        }

        _logger.LogInformation("📊 液位数据采集完成: 成功 {Success}, 失败 {Fail}, 总计 {Total}", 
            successCount, failCount, enabledSites.Count);
    }

    /// <summary>
    /// 从OPC UA缓存读取液位数据
    /// </summary>
    private decimal? ReadWaterLevelFromCache(string siteCode)
    {
        try
        {
            // 尝试读取主液位节点
            var cacheKey = $"{siteCode}:{GetNodeIdFromConfig(UPSTREAM_LEVEL_NODE_KEY)}";
            
            lock (_opcUaCache.CacheLock)
            {
                if (_opcUaCache.NodeCache.TryGetValue(cacheKey, out var snapshot))
                {
                    if (snapshot?.Value != null && decimal.TryParse(snapshot.Value.ToString(), out var level))
                    {
                        return level;
                    }
                }
            }

            // 如果主节点读取失败，尝试多普勒液位节点
            var dopplerKey = $"{siteCode}:{GetNodeIdFromConfig(DOPPLER_LEVEL_NODE_KEY)}";
            
            lock (_opcUaCache.CacheLock)
            {
                if (_opcUaCache.NodeCache.TryGetValue(dopplerKey, out var snapshot))
                {
                    if (snapshot?.Value != null && decimal.TryParse(snapshot.Value.ToString(), out var level))
                    {
                        _logger.LogDebug("📡 [{SiteCode}] 使用多普勒液位数据", siteCode);
                        return level;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ [{SiteCode}] 读取液位缓存失败", siteCode);
            return null;
        }
    }

    /// <summary>
    /// 从配置获取节点ID
    /// </summary>
    private string GetNodeIdFromConfig(string nodeKey)
    {
        // 从nodes.json配置读取
        // 这里简化处理，实际应该从配置文件动态读取
        return nodeKey switch
        {
            UPSTREAM_LEVEL_NODE_KEY => "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actLevel",
            DOPPLER_LEVEL_NODE_KEY => "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actLevelDoppler",
            _ => string.Empty
        };
    }

    /// <summary>
    /// 根据液位值判断状态
    /// </summary>
    private string DetermineStatus(decimal waterLevel)
    {
        // 可以根据实际业务需求设置阈值
        if (waterLevel < 0)
            return "offline";
        if (waterLevel > 10)  // 假设超过10米为告警
            return "alarm";
        if (waterLevel > 8)   // 假设超过8米为警告
            return "warning";
        
        return "normal";
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 停止上游液位数据采集服务");
        return base.StopAsync(cancellationToken);
    }
}

