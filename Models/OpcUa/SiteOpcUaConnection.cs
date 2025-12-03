using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Models.OpcUa;

/// <summary>
/// 站点 OPC UA 连接配置
/// Site OPC UA Connection Configuration
/// </summary>
public class SiteOpcUaConnection
{
    /// <summary>
    /// 站点编码
    /// </summary>
    public string SiteCode { get; set; } = string.Empty;
    
    /// <summary>
    /// 站点名称
    /// </summary>
    public string SiteName { get; set; } = string.Empty;
    
    /// <summary>
    /// OPC UA 服务器端点地址
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// 安全策略
    /// </summary>
    public string SecurityPolicy { get; set; } = "None";
    
    /// <summary>
    /// 安全模式
    /// </summary>
    public string SecurityMode { get; set; } = "None";
    
    /// <summary>
    /// 是否匿名连接
    /// </summary>
    public bool Anonymous { get; set; } = true;
    
    /// <summary>
    /// 用户名
    /// </summary>
    public string? Username { get; set; }
    
    /// <summary>
    /// 密码
    /// </summary>
    public string? Password { get; set; }
    
    /// <summary>
    /// 会话超时时间（毫秒）
    /// </summary>
    public int SessionTimeout { get; set; } = 30000;
    
    /// <summary>
    /// 请求超时时间（毫秒）
    /// </summary>
    public int RequestTimeout { get; set; } = 10000;
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 从 SiteConfig 实体创建连接配置
    /// </summary>
    public static SiteOpcUaConnection FromSiteConfig(SiteConfig site)
    {
        return new SiteOpcUaConnection
        {
            SiteCode = site.SiteCode,
            SiteName = site.SiteName,
            Endpoint = site.OpcuaEndpoint ?? $"opc.tcp://{site.IpAddress}:{site.Port ?? 4840}",
            SecurityPolicy = site.OpcuaSecurityPolicy,
            SecurityMode = site.OpcuaSecurityMode,
            Anonymous = site.OpcuaAnonymous,
            Username = site.OpcuaUsername,
            Password = site.OpcuaPassword,
            SessionTimeout = site.OpcuaSessionTimeout,
            RequestTimeout = site.OpcuaRequestTimeout,
            IsEnabled = site.IsEnabled
        };
    }
}

