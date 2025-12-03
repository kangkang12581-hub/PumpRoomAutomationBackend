namespace PumpRoomAutomationBackend.DTOs.Site;

/// <summary>
/// 站点数据传输对象
/// Site Data Transfer Object
/// </summary>
public class SiteDto
{
    public int Id { get; set; }
    public string SiteCode { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
    public string? SiteLocation { get; set; }
    public string? SiteDescription { get; set; }
    
    // 网络配置
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public string Protocol { get; set; } = "OPC.UA";
    
    // 机内摄像头配置
    public string? InternalCameraIp { get; set; }
    public string? InternalCameraUsername { get; set; }
    public string? InternalCameraPassword { get; set; }
    
    // 全局摄像头配置
    public string? GlobalCameraIp { get; set; }
    public string? GlobalCameraUsername { get; set; }
    public string? GlobalCameraPassword { get; set; }
    
    // OPC UA 配置
    public string? OpcuaEndpoint { get; set; }
    public string OpcuaSecurityPolicy { get; set; } = "None";
    public string OpcuaSecurityMode { get; set; } = "None";
    public bool OpcuaAnonymous { get; set; } = true;
    public string? OpcuaUsername { get; set; }
    public int OpcuaSessionTimeout { get; set; } = 30000;
    public int OpcuaRequestTimeout { get; set; } = 10000;
    
    // 联系信息
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    
    // 运行参数
    public string? OperatingPressureMin { get; set; }
    public string? OperatingPressureMax { get; set; }
    public int PumpCount { get; set; }
    
    // 状态
    public bool IsEnabled { get; set; }
    public bool IsOnline { get; set; }
    public string ConnectionStatus { get; set; } = "disconnected";
    public DateTime? LastHeartbeat { get; set; }
    public bool IsDefault { get; set; }
    
    // 报警配置
    public bool AlarmEnabled { get; set; }
    public string? AlarmPhoneNumbers { get; set; }
    public string? AlarmEmailAddresses { get; set; }
    
    // 时间戳
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

