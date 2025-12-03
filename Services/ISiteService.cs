using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 站点服务接口
/// Site Service Interface
/// </summary>
public interface ISiteService
{
    /// <summary>
    /// 获取所有站点
    /// </summary>
    Task<List<SiteConfig>> GetAllSitesAsync();
    
    /// <summary>
    /// 获取已启用的站点
    /// </summary>
    Task<List<SiteConfig>> GetEnabledSitesAsync();
    
    /// <summary>
    /// 根据站点编码获取站点
    /// </summary>
    Task<SiteConfig?> GetSiteBySiteCodeAsync(string siteCode);
    
    /// <summary>
    /// 根据ID获取站点
    /// </summary>
    Task<SiteConfig?> GetSiteByIdAsync(int id);
    
    /// <summary>
    /// 创建站点
    /// </summary>
    Task<SiteConfig> CreateSiteAsync(SiteConfig site);
    
    /// <summary>
    /// 更新站点
    /// </summary>
    Task<SiteConfig> UpdateSiteAsync(SiteConfig site);
    
    /// <summary>
    /// 删除站点
    /// </summary>
    Task<bool> DeleteSiteAsync(int id);
    
    /// <summary>
    /// 启用/禁用站点
    /// </summary>
    Task<bool> ToggleSiteAsync(string siteCode, bool enabled);
    
    /// <summary>
    /// 更新站点连接状态
    /// </summary>
    Task UpdateSiteConnectionStatusAsync(string siteCode, bool isOnline, string connectionStatus);
    
    /// <summary>
    /// 更新站点心跳
    /// </summary>
    Task UpdateSiteHeartbeatAsync(string siteCode);
}

