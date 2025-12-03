using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PumpRoomAutomationBackend.Models.Entities;

/// <summary>
/// 分钟级时序数据表
/// Minute-level telemetry data table
/// 用于存储来自 OpcUaClient 的关键指标，每分钟一条
/// </summary>
[Table("telemetry_minute")]
[Index(nameof(SiteCode), nameof(TsMinute), Name = "ix_telemetry_minute_site_time")]
[Index(nameof(SiteCode), Name = "IX_telemetry_minute_SiteCode")]
[Index(nameof(TsMinute), Name = "IX_telemetry_minute_TsMinute")]
public class TelemetryMinute
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    // 站点编码，便于多站点查询聚合
    [Column("site_code")]
    [Required]
    [MaxLength(50)]
    public string SiteCode { get; set; } = string.Empty;
    
    // 关键测点
    [Column("upstream_level")]
    public double? UpstreamLevel { get; set; }
    
    [Column("downstream_level")]
    public double? DownstreamLevel { get; set; }
    
    [Column("instantaneous_flow")]
    public double? InstantaneousFlow { get; set; }
    
    [Column("flow_velocity")]
    public double? FlowVelocity { get; set; }
    
    [Column("water_temperature")]
    public double? WaterTemperature { get; set; }
    
    [Column("net_weight")]
    public double? NetWeight { get; set; }
    
    [Column("speed")]
    public double? Speed { get; set; }
    
    [Column("electric_current")]
    public double? ElectricCurrent { get; set; }
    
    [Column("winding_temperature")]
    public double? WindingTemperature { get; set; }
    
    [Column("cabinet_outer_temperature")]
    public double? CabinetOuterTemperature { get; set; }
    
    [Column("cabinet_inner_temperature")]
    public double? CabinetInnerTemperature { get; set; }
    
    [Column("cabinet_outer_humidity")]
    public double? CabinetOuterHumidity { get; set; }
    
    // 采样时间（对齐到分钟）
    [Column("ts_minute")]
    [Required]
    public DateTime TsMinute { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

