using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.Models.OpcUa;

namespace PumpRoomAutomationBackend.Services.OpcUa;

/// <summary>
/// 多站点 OPC UA 连接管理器
/// Multi-site OPC UA Connection Manager
/// </summary>
public interface IOpcUaConnectionManager
{
    Task InitializeAsync();
    Task<bool> ConnectSiteAsync(string siteCode);
    Task DisconnectSiteAsync(string siteCode);
    Task DisconnectAllAsync();
    SiteOpcUaClient? GetClient(string siteCode);
    Dictionary<string, bool> GetAllConnectionStatus();
    Task ReloadSiteConfigurationsAsync();
}

public class OpcUaConnectionManager : IOpcUaConnectionManager, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OpcUaConnectionManager> _logger;
    private readonly ConcurrentDictionary<string, SiteOpcUaClient> _clients;
    private bool _disposed;
    
    public OpcUaConnectionManager(
        IServiceProvider serviceProvider,
        ILogger<OpcUaConnectionManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _clients = new ConcurrentDictionary<string, SiteOpcUaClient>();
    }
    
    /// <summary>
    /// 初始化所有站点连接
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("🚀 初始化多站点 OPC UA 连接管理器...");
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // 加载所有启用的站点配置
            var sites = await dbContext.SiteConfigs
                .Where(s => s.IsEnabled && s.IsActive)
                .ToListAsync();
            
            _logger.LogInformation("📋 发现 {Count} 个启用的站点配置", sites.Count);
            
            // 并发连接所有站点
            var connectTasks = sites.Select(async site =>
            {
                var config = SiteOpcUaConnection.FromSiteConfig(site);
                var clientLogger = _serviceProvider.GetRequiredService<ILogger<SiteOpcUaClient>>();
                var client = new SiteOpcUaClient(config, clientLogger);
                
                if (_clients.TryAdd(site.SiteCode, client))
                {
                    var connected = await client.ConnectAsync();
                    
                    // 更新数据库中的连接状态
                    await UpdateSiteConnectionStatus(site.SiteCode, connected);
                    
                    return (site.SiteCode, connected);
                }
                
                return (site.SiteCode, false);
            });
            
            var results = await Task.WhenAll(connectTasks);
            
            var successCount = results.Count(r => r.Item2);
            _logger.LogInformation("✅ 站点连接完成: {Success}/{Total} 成功", 
                successCount, results.Length);
            
            foreach (var (siteCode, connected) in results)
            {
                var status = connected ? "✅ 已连接" : "❌ 连接失败";
                _logger.LogInformation("   [{SiteCode}] {Status}", siteCode, status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 初始化连接管理器失败: {Message}", ex.Message);
        }
    }
    
    /// <summary>
    /// 连接指定站点
    /// </summary>
    public async Task<bool> ConnectSiteAsync(string siteCode)
    {
        try
        {
            // 如果已存在客户端，先断开
            if (_clients.TryGetValue(siteCode, out var existingClient))
            {
                await existingClient.DisconnectAsync();
                _clients.TryRemove(siteCode, out _);
                existingClient.Dispose();
            }
            
            // 从数据库加载站点配置
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var site = await dbContext.SiteConfigs
                .FirstOrDefaultAsync(s => s.SiteCode == siteCode);
            
            if (site == null)
            {
                _logger.LogWarning("⚠️ 站点配置不存在: {SiteCode}", siteCode);
                return false;
            }
            
            if (!site.IsEnabled || !site.IsActive)
            {
                _logger.LogWarning("⚠️ 站点未启用或未激活: {SiteCode}", siteCode);
                return false;
            }
            
            // 创建新客户端并连接
            var config = SiteOpcUaConnection.FromSiteConfig(site);
            var clientLogger = _serviceProvider.GetRequiredService<ILogger<SiteOpcUaClient>>();
            var client = new SiteOpcUaClient(config, clientLogger);
            
            var connected = await client.ConnectAsync();
            
            if (connected && _clients.TryAdd(siteCode, client))
            {
                await UpdateSiteConnectionStatus(siteCode, true);
                return true;
            }
            
            client.Dispose();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 连接站点失败: {SiteCode}", siteCode);
            return false;
        }
    }
    
    /// <summary>
    /// 断开指定站点连接
    /// </summary>
    public async Task DisconnectSiteAsync(string siteCode)
    {
        if (_clients.TryRemove(siteCode, out var client))
        {
            await client.DisconnectAsync();
            client.Dispose();
            await UpdateSiteConnectionStatus(siteCode, false);
            
            _logger.LogInformation("🔌 [{SiteCode}] 站点已断开", siteCode);
        }
    }
    
    /// <summary>
    /// 断开所有站点连接
    /// </summary>
    public async Task DisconnectAllAsync()
    {
        _logger.LogInformation("🔌 断开所有站点连接...");
        
        var disconnectTasks = _clients.Values.Select(async client =>
        {
            await client.DisconnectAsync();
            client.Dispose();
        });
        
        await Task.WhenAll(disconnectTasks);
        _clients.Clear();
        
        _logger.LogInformation("✅ 所有站点已断开");
    }
    
    /// <summary>
    /// 获取指定站点的客户端
    /// </summary>
    public SiteOpcUaClient? GetClient(string siteCode)
    {
        return _clients.TryGetValue(siteCode, out var client) ? client : null;
    }
    
    /// <summary>
    /// 获取所有站点的连接状态
    /// </summary>
    public Dictionary<string, bool> GetAllConnectionStatus()
    {
        return _clients.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.IsConnected
        );
    }
    
    /// <summary>
    /// 重新加载站点配置
    /// </summary>
    public async Task ReloadSiteConfigurationsAsync()
    {
        _logger.LogInformation("🔄 重新加载站点配置...");
        
        await DisconnectAllAsync();
        await InitializeAsync();
    }
    
    /// <summary>
    /// 更新数据库中的站点连接状态
    /// </summary>
    private async Task UpdateSiteConnectionStatus(string siteCode, bool connected)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var site = await dbContext.SiteConfigs
                .FirstOrDefaultAsync(s => s.SiteCode == siteCode);
            
            if (site != null)
            {
                site.IsOnline = connected;
                site.ConnectionStatus = connected ? "connected" : "disconnected";
                site.LastHeartbeat = connected ? DateTime.UtcNow : site.LastHeartbeat;
                site.UpdatedAt = DateTime.UtcNow;
                
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 更新站点连接状态失败: {SiteCode}", siteCode);
        }
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        DisconnectAllAsync().Wait();
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }
}

