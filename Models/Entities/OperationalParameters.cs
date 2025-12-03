using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

/// <summary>
/// 运行参数表模型
/// Operational parameters table model
/// </summary>
[Table("operational_parameters")]
public class OperationalParameters
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    // 关联用户ID
    [Column("user_id")]
    [Required]
    public int UserId { get; set; }
    
    // 速度参数
    [Column("set_velocity_high_limit")]
    public double? SetVelocityHighLimit { get; set; }
    
    [Column("set_velocity_low_limit")]
    public double? SetVelocityLowLimit { get; set; }
    
    [Column("set_m_velocity")]
    public double? SetMVelocity { get; set; }
    
    [Column("set_velocity_alm")]
    public double? SetVelocityAlm { get; set; }
    
    [Column("set_liquid_level_diff")]
    public double? SetLiquidLevelDiff { get; set; }
    
    [Column("set_p")]
    public double? SetP { get; set; }
    
    [Column("set_i")]
    public double? SetI { get; set; }
    
    [Column("set_d")]
    public double? SetD { get; set; }
    
    // 绕组加热参数
    [Column("motor_coli_heat_temp")]
    public double? MotorColiHeatTemp { get; set; }
    
    [Column("motor_coli_stop_temp")]
    public double? MotorColiStopTemp { get; set; }
    
    [Column("motor_coli_alm_temp")]
    public double? MotorColiAlmTemp { get; set; }
    
    [Column("motor_coli_cool_start_temp")]
    public double? MotorColiCoolStartTemp { get; set; }
    
    [Column("motor_coli_cool_stop_temp")]
    public double? MotorColiCoolStopTemp { get; set; }
    
    [Column("heating_is_running")]
    public bool HeatingIsRunning { get; set; } = false;
    
    // 延时参数
    [Column("pump_run_time")]
    public int? PumpRunTime { get; set; }
    
    [Column("pump_stop_time")]
    public int? PumpStopTime { get; set; }
    
    // 流体参数
    [Column("alm_level_diff")]
    public double? AlmLevelDiff { get; set; }
    
    [Column("alm_level_doppler_high")]
    public double? AlmLevelDopplerHigh { get; set; }
    
    [Column("alm_flow_low")]
    public double? AlmFlowLow { get; set; }
    
    // 环境参数
    [Column("temp_max")]
    public double? TempMax { get; set; }
    
    [Column("temp_min")]
    public double? TempMin { get; set; }
    
    [Column("humidity_max")]
    public int? HumidityMax { get; set; }
    
    [Column("humidity_min")]
    public int? HumidityMin { get; set; }
    
    [Column("vibration_threshold")]
    public double? VibrationThreshold { get; set; }
    
    [Column("noise_threshold")]
    public int? NoiseThreshold { get; set; }
    
    [Column("pressure")]
    public double? Pressure { get; set; }
    
    [Column("air_quality_threshold")]
    public int? AirQualityThreshold { get; set; }
    
    // 容器重量参数
    [Column("set_max_tare_weight")]
    public double? SetMaxTareWeight { get; set; }
    
    [Column("set_warn_weight")]
    public double? SetWarnWeight { get; set; }
    
    [Column("set_alarm_net_weight")]
    public double? SetAlarmNetWeight { get; set; }
    
    // HART通信参数
    [Column("hart_en")]
    public bool HartEn { get; set; } = false;
    
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
}

