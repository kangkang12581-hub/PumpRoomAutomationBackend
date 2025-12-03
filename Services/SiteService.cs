using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 站点服务
/// Site Service
/// </summary>
public class SiteService : ISiteService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SiteService> _logger;
    
    public SiteService(ApplicationDbContext context, ILogger<SiteService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<List<SiteConfig>> GetAllSitesAsync()
    {
        return await _context.SiteConfigs
            .OrderBy(s => s.SiteCode)
            .ToListAsync();
    }
    
    public async Task<List<SiteConfig>> GetEnabledSitesAsync()
    {
        return await _context.SiteConfigs
            .Where(s => s.IsEnabled && s.IsActive)
            .OrderBy(s => s.SiteCode)
            .ToListAsync();
    }
    
    public async Task<SiteConfig?> GetSiteBySiteCodeAsync(string siteCode)
    {
        return await _context.SiteConfigs
            .FirstOrDefaultAsync(s => s.SiteCode == siteCode);
    }
    
    public async Task<SiteConfig?> GetSiteByIdAsync(int id)
    {
        return await _context.SiteConfigs
            .FirstOrDefaultAsync(s => s.Id == id);
    }
    
    public async Task<SiteConfig> CreateSiteAsync(SiteConfig site)
    {
        // 检查站点编码是否已存在
        var existing = await _context.SiteConfigs
            .FirstOrDefaultAsync(s => s.SiteCode == site.SiteCode);
        
        if (existing != null)
        {
            throw new InvalidOperationException($"站点编码 {site.SiteCode} 已存在");
        }
        
        // 如果没有提供OpcuaEndpoint，根据IP和端口自动生成
        if (string.IsNullOrWhiteSpace(site.OpcuaEndpoint) && !string.IsNullOrWhiteSpace(site.IpAddress))
        {
            var port = site.Port ?? 4840;
            site.OpcuaEndpoint = $"opc.tcp://{site.IpAddress}:{port}";
        }
        
        site.CreatedAt = DateTime.UtcNow;
        site.UpdatedAt = DateTime.UtcNow;
        site.IsActive = true;
        
        _context.SiteConfigs.Add(site);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("站点已创建: {SiteCode} - {SiteName}", site.SiteCode, site.SiteName);
        
        return site;
    }
    
    public async Task<SiteConfig> UpdateSiteAsync(SiteConfig site)
    {
        // 如果没有提供OpcuaEndpoint，根据IP和端口自动生成
        if (string.IsNullOrWhiteSpace(site.OpcuaEndpoint) && !string.IsNullOrWhiteSpace(site.IpAddress))
        {
            var port = site.Port ?? 4840;
            site.OpcuaEndpoint = $"opc.tcp://{site.IpAddress}:{port}";
        }
        
        site.UpdatedAt = DateTime.UtcNow;
        
        _context.SiteConfigs.Update(site);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("站点已更新: {SiteCode} - {SiteName}", site.SiteCode, site.SiteName);
        
        return site;
    }
    
    public async Task<bool> DeleteSiteAsync(int id)
    {
        var site = await _context.SiteConfigs.FindAsync(id);
        
        if (site == null)
        {
            return false;
        }
        
        _context.SiteConfigs.Remove(site);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("站点已删除: {SiteCode} - {SiteName}", site.SiteCode, site.SiteName);
        
        return true;
    }
    
    public async Task<bool> ToggleSiteAsync(string siteCode, bool enabled)
    {
        var site = await GetSiteBySiteCodeAsync(siteCode);
        
        if (site == null)
        {
            return false;
        }
        
        site.IsEnabled = enabled;
        site.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("站点状态已切换: {SiteCode} - Enabled={Enabled}", siteCode, enabled);
        
        return true;
    }
    
    public async Task UpdateSiteConnectionStatusAsync(string siteCode, bool isOnline, string connectionStatus)
    {
        var site = await GetSiteBySiteCodeAsync(siteCode);
        
        if (site == null)
        {
            return;
        }
        
        site.IsOnline = isOnline;
        site.ConnectionStatus = connectionStatus;
        site.UpdatedAt = DateTime.UtcNow;
        
        if (isOnline)
        {
            site.LastHeartbeat = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateSiteHeartbeatAsync(string siteCode)
    {
        var site = await GetSiteBySiteCodeAsync(siteCode);
        
        if (site == null)
        {
            return;
        }
        
        site.LastHeartbeat = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }
}
