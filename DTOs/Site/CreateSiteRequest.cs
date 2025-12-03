using System.ComponentModel.DataAnnotations;

namespace PumpRoomAutomationBackend.DTOs.Site;

/// <summary>
/// 创建站点请求
/// Create Site Request
/// </summary>
public class CreateSiteRequest
{
    [Required(ErrorMessage = "站点编码不能为空")]
    [MaxLength(50)]
    public string SiteCode { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "站点名称不能为空")]
    [MaxLength(255)]
    public string SiteName { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? SiteLocation { get; set; }
    
    public string? SiteDescription { get; set; }
    
    // 网络配置
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    public int? Port { get; set; } = 4840;
    
    public string Protocol { get; set; } = "OPC.UA";
    
    // 机内摄像头配置
    [MaxLength(45)]
    public string? InternalCameraIp { get; set; }
    
    [MaxLength(100)]
    public string? InternalCameraUsername { get; set; }
    
    [MaxLength(255)]
    public string? InternalCameraPassword { get; set; }
    
    // 全局摄像头配置
    [MaxLength(45)]
    public string? GlobalCameraIp { get; set; }
    
    [MaxLength(100)]
    public string? GlobalCameraUsername { get; set; }
    
    [MaxLength(255)]
    public string? GlobalCameraPassword { get; set; }
    
    // OPC UA 配置（可选，如果不提供会根据IP和端口自动生成）
    [MaxLength(255)]
    public string? OpcuaEndpoint { get; set; }
    
    public string OpcuaSecurityPolicy { get; set; } = "None";
    public string OpcuaSecurityMode { get; set; } = "None";
    public bool OpcuaAnonymous { get; set; } = true;
    
    [MaxLength(100)]
    public string? OpcuaUsername { get; set; }
    
    [MaxLength(255)]
    public string? OpcuaPassword { get; set; }
    
    public int OpcuaSessionTimeout { get; set; } = 30000;
    public int OpcuaRequestTimeout { get; set; } = 10000;
    
    // 联系信息
    [MaxLength(100)]
    public string? ContactPerson { get; set; }
    
    [MaxLength(20)]
    public string? ContactPhone { get; set; }
    
    [MaxLength(255)]
    [EmailAddress]
    public string? ContactEmail { get; set; }
    
    // 运行参数
    public string? OperatingPressureMin { get; set; }
    public string? OperatingPressureMax { get; set; }
    public int PumpCount { get; set; } = 0;
    
    // 报警配置
    public bool AlarmEnabled { get; set; } = true;
    public string? AlarmPhoneNumbers { get; set; }
    public string? AlarmEmailAddresses { get; set; }
}

