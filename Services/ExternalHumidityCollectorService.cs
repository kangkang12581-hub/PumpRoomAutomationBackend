using Microsoft.Extensions.DependencyInjection;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Services.OpcUa;
using Microsoft.EntityFrameworkCore;

namespace PumpRoomAutomationBackend.Services;

public class ExternalHumidityCollectorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOpcUaCache _opcUaCache;
    private readonly IOpcUaConnectionManager _connectionManager;
    private readonly ILogger<ExternalHumidityCollectorService> _logger;
    private const string NODE_KEY = "actExternalHumidity";

    public ExternalHumidityCollectorService(IServiceProvider serviceProvider, IOpcUaCache opcUaCache, 
        IOpcUaConnectionManager connectionManager, ILogger<ExternalHumidityCollectorService> logger)
    {
        _serviceProvider = serviceProvider;
        _opcUaCache = opcUaCache;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 启动柜外湿度数据采集服务");
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await CollectAndSaveDataAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "❌ 柜外湿度数据采集失败"); }

            var now = DateTime.UtcNow;
            var nextMinute = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
            var delay = nextMinute - now;
            if (delay > TimeSpan.Zero) await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task CollectAndSaveDataAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dataService = scope.ServiceProvider.GetRequiredService<IExternalHumidityService>();
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

                var value = ReadValueFromCache(site.SiteCode);
                if (!value.HasValue) continue;

                var request = new AddExternalHumidityRequest
                {
                    SiteId = site.Id,
                    Timestamp = timestamp,
                    Value = value.Value,
                    Status = "normal",
                    DataQuality = 100
                };

                await dataService.AddDataAsync(request);
                successCount++;
                _logger.LogInformation("✅ [{SiteCode}] 保存柜外湿度: {Value:F3}%", site.SiteCode, value.Value);
            }
            catch (InvalidOperationException) { }
            catch (Exception ex) { _logger.LogError(ex, "❌ [{SiteCode}] 保存柜外湿度失败", site.SiteCode); }
        }

        if (successCount > 0)
            _logger.LogInformation("📊 本次采集: 成功保存 {Count} 个站点的柜外湿度数据", successCount);
    }

    private decimal? ReadValueFromCache(string siteCode)
    {
        try
        {
            var nodeId = GetNodeIdFromConfig(NODE_KEY);
            if (string.IsNullOrEmpty(nodeId)) return null;

            var cacheKey = $"{siteCode}:{nodeId}";
            lock (_opcUaCache.CacheLock)
            {
                if (_opcUaCache.NodeCache.TryGetValue(cacheKey, out var snapshot))
                {
                    if (snapshot?.Value != null && decimal.TryParse(snapshot.Value.ToString(), out var value))
                        return value;
                }
            }
            return null;
        }
        catch { return null; }
    }

    private string GetNodeIdFromConfig(string nodeKey) => nodeKey switch
    {
        NODE_KEY => "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.Ghr_actExternalHumidity",
        _ => string.Empty
    };
}
