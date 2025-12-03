using Microsoft.Extensions.DependencyInjection;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Services.OpcUa;
using Microsoft.EntityFrameworkCore;

namespace PumpRoomAutomationBackend.Services;

public class FlowVelocityCollectorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOpcUaCache _opcUaCache;
    private readonly IOpcUaConnectionManager _connectionManager;
    private readonly ILogger<FlowVelocityCollectorService> _logger;
    private const string VELOCITY_NODE_KEY = "actFlowVelocity";

    public FlowVelocityCollectorService(IServiceProvider serviceProvider, IOpcUaCache opcUaCache, 
        IOpcUaConnectionManager connectionManager, ILogger<FlowVelocityCollectorService> logger)
    {
        _serviceProvider = serviceProvider;
        _opcUaCache = opcUaCache;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 启动流速数据采集服务");
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await CollectAndSaveDataAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "❌ 流速数据采集失败"); }

            var now = DateTime.UtcNow;
            var nextMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
            var delay = nextMinute - now;
            if (delay > TimeSpan.Zero) await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task CollectAndSaveDataAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dataService = scope.ServiceProvider.GetRequiredService<IFlowVelocityService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var enabledSites = await dbContext.SiteConfigs.Where(s => s.IsEnabled)
            .Select(s => new { s.Id, s.SiteCode }).ToListAsync();
        if (!enabledSites.Any()) return;

        var timestamp = DateTime.UtcNow;
        var successCount = 0;

        foreach (var site in enabledSites)
        {
            try
            {
                var client = _connectionManager.GetClient(site.SiteCode);
                if (client?.IsConnected != true) continue;

                var velocity = ReadVelocityFromCache(site.SiteCode);
                if (!velocity.HasValue) continue;

                var request = new AddFlowVelocityRequest
                {
                    SiteId = site.Id,
                    Timestamp = timestamp,
                    Velocity = velocity.Value,
                    Status = "normal",
                    DataQuality = 100
                };

                await dataService.AddDataAsync(request);
                successCount++;
                _logger.LogInformation("✅ [{SiteCode}] 保存流速: {Velocity:F3}m/s", site.SiteCode, velocity.Value);
            }
            catch (InvalidOperationException) { }
            catch (Exception ex) { _logger.LogError(ex, "❌ [{SiteCode}] 保存流速失败", site.SiteCode); }
        }
        _logger.LogInformation("📊 流速采集完成: {Success}/{Total}", successCount, enabledSites.Count);
    }

    private decimal? ReadVelocityFromCache(string siteCode)
    {
        try
        {
            var cacheKey = $"{siteCode}:{GetNodeIdFromConfig(VELOCITY_NODE_KEY)}";
            lock (_opcUaCache.CacheLock)
            {
                if (_opcUaCache.NodeCache.TryGetValue(cacheKey, out var snapshot))
                {
                    if (snapshot?.Value != null && decimal.TryParse(snapshot.Value.ToString(), out var velocity))
                        return velocity;
                }
            }
            return null;
        }
        catch { return null; }
    }

    private string GetNodeIdFromConfig(string nodeKey) => nodeKey switch
    {
        VELOCITY_NODE_KEY => "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actFlowVelocity",
        _ => string.Empty
    };
}
