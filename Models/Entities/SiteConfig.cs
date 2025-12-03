using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

/// <summary>
/// 站点配置表模型
/// Site configuration table model
/// </summary>
[Table("site_configs")]
public class SiteConfig
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    // 关联用户ID（配置创建者/管理者）
    [Column("user_id")]
    [Required]
    public int UserId { get; set; }
    
    // 站点基本信息
    [Column("site_code")]
    [Required]
    [MaxLength(50)]
    public string SiteCode { get; set; } = string.Empty;
    
    [Column("site_name")]
    [Required]
    [MaxLength(255)]
    public string SiteName { get; set; } = string.Empty;
    
    [Column("site_location")]
    [MaxLength(500)]
    public string? SiteLocation { get; set; }
    
    [Column("site_description")]
    public string? SiteDescription { get; set; }
    
    // 站点网络配置
    [Column("ip_address")]
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [Column("port")]
    public int? Port { get; set; }
    
    [Column("protocol")]
    [MaxLength(20)]
    public string Protocol { get; set; } = "TCP";
    
    // 机内摄像头配置
    [Column("internal_camera_ip")]
    [MaxLength(45)]
    public string? InternalCameraIp { get; set; }
    
    [Column("internal_camera_username")]
    [MaxLength(100)]
    public string? InternalCameraUsername { get; set; }
    
    [Column("internal_camera_password")]
    [MaxLength(255)]
    public string? InternalCameraPassword { get; set; }
    
    // 全局摄像头配置
    [Column("global_camera_ip")]
    [MaxLength(45)]
    public string? GlobalCameraIp { get; set; }
    
    [Column("global_camera_username")]
    [MaxLength(100)]
    public string? GlobalCameraUsername { get; set; }
    
    [Column("global_camera_password")]
    [MaxLength(255)]
    public string? GlobalCameraPassword { get; set; }
    
    // OPC UA 连接配置
    [Column("opcua_endpoint")]
    [MaxLength(255)]
    public string? OpcuaEndpoint { get; set; }
    
    [Column("opcua_security_policy")]
    [MaxLength(50)]
    public string OpcuaSecurityPolicy { get; set; } = "None";
    
    [Column("opcua_security_mode")]
    [MaxLength(50)]
    public string OpcuaSecurityMode { get; set; } = "None";
    
    [Column("opcua_anonymous")]
    public bool OpcuaAnonymous { get; set; } = true;
    
    [Column("opcua_username")]
    [MaxLength(100)]
    public string? OpcuaUsername { get; set; }
    
    [Column("opcua_password")]
    [MaxLength(255)]
    public string? OpcuaPassword { get; set; }
    
    [Column("opcua_session_timeout")]
    public int OpcuaSessionTimeout { get; set; } = 30000;
    
    [Column("opcua_request_timeout")]
    public int OpcuaRequestTimeout { get; set; } = 10000;
    
    // 站点联系信息
    [Column("contact_person")]
    [MaxLength(100)]
    public string? ContactPerson { get; set; }
    
    [Column("contact_phone")]
    [MaxLength(20)]
    public string? ContactPhone { get; set; }
    
    [Column("contact_email")]
    [MaxLength(255)]
    public string? ContactEmail { get; set; }
    
    // 站点运行参数
    [Column("operating_pressure_min")]
    [MaxLength(50)]
    public string? OperatingPressureMin { get; set; }
    
    [Column("operating_pressure_max")]
    [MaxLength(50)]
    public string? OperatingPressureMax { get; set; }
    
    [Column("pump_count")]
    public int PumpCount { get; set; } = 0;
    
    // 站点状态
    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;
    
    [Column("is_online")]
    public bool IsOnline { get; set; } = false;
    
    [Column("connection_status")]
    [MaxLength(20)]
    public string ConnectionStatus { get; set; } = "disconnected";
    
    [Column("last_heartbeat")]
    public DateTime? LastHeartbeat { get; set; }
    
    [Column("is_default")]
    public bool IsDefault { get; set; } = false;
    
    // 报警配置
    [Column("alarm_enabled")]
    public bool AlarmEnabled { get; set; } = true;
    
    [Column("alarm_phone_numbers")]
    public string? AlarmPhoneNumbers { get; set; }
    
    [Column("alarm_email_addresses")]
    public string? AlarmEmailAddresses { get; set; }
    
    // 配置状态
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    // 时间戳字段
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    public virtual ICollection<AlarmRecord> AlarmRecords { get; set; } = new List<AlarmRecord>();
}

