using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PumpRoomAutomationBackend.Models.Entities;

/// <summary>
/// 用户参数设置表
/// User settings table for storing frontend parameter configurations
/// </summary>
[Table("user_settings")]
public class UserSettings
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("user_id")]
    [Required]
    public int UserId { get; set; }
    
    // 环境数据阈值设置
    [Column("temp_high_threshold")]
    public double TempHighThreshold { get; set; } = 35.0;
    
    [Column("temp_low_threshold")]
    public double TempLowThreshold { get; set; } = 15.0;
    
    [Column("humidity_high_threshold")]
    public double HumidityHighThreshold { get; set; } = 75.0;
    
    [Column("humidity_low_threshold")]
    public double HumidityLowThreshold { get; set; } = 45.0;
    
    // 水流数据阈值设置
    [Column("level_high_threshold")]
    public double LevelHighThreshold { get; set; } = 150.0;
    
    [Column("level_low_threshold")]
    public double LevelLowThreshold { get; set; } = 50.0;
    
    [Column("level_diff_high_threshold")]
    public double LevelDiffHighThreshold { get; set; } = 15.0;
    
    [Column("level_diff_low_threshold")]
    public double LevelDiffLowThreshold { get; set; } = 2.0;
    
    [Column("water_temp_high_threshold")]
    public double WaterTempHighThreshold { get; set; } = 25.0;
    
    [Column("water_temp_low_threshold")]
    public double WaterTempLowThreshold { get; set; } = 5.0;
    
    // 格栅电机参数阈值
    [Column("motor_speed_high_threshold")]
    public double MotorSpeedHighThreshold { get; set; } = 30.0;
    
    [Column("motor_speed_low_threshold")]
    public double MotorSpeedLowThreshold { get; set; } = 5.0;
    
    [Column("motor_current_high_threshold")]
    public double MotorCurrentHighThreshold { get; set; } = 15.0;
    
    [Column("motor_current_low_threshold")]
    public double MotorCurrentLowThreshold { get; set; } = 2.0;
    
    [Column("motor_power_high_threshold")]
    public double MotorPowerHighThreshold { get; set; } = 10.0;
    
    [Column("motor_power_low_threshold")]
    public double MotorPowerLowThreshold { get; set; } = 1.0;
    
    [Column("motor_torque_high_threshold")]
    public double MotorTorqueHighThreshold { get; set; } = 95.0;
    
    [Column("motor_torque_low_threshold")]
    public double MotorTorqueLowThreshold { get; set; } = 20.0;
    
    [Column("motor_winding_temp_high_threshold")]
    public double MotorWindingTempHighThreshold { get; set; } = 80.0;
    
    [Column("motor_winding_temp_low_threshold")]
    public double MotorWindingTempLowThreshold { get; set; } = 30.0;
    
    // 毛刷电机参数阈值
    [Column("brush_speed_high_threshold")]
    public double BrushSpeedHighThreshold { get; set; } = 1400.0;
    
    [Column("brush_speed_low_threshold")]
    public double BrushSpeedLowThreshold { get; set; } = 200.0;
    
    [Column("brush_current_high_threshold")]
    public double BrushCurrentHighThreshold { get; set; } = 12.0;
    
    [Column("brush_current_low_threshold")]
    public double BrushCurrentLowThreshold { get; set; } = 1.0;
    
    [Column("brush_temp_high_threshold")]
    public double BrushTempHighThreshold { get; set; } = 70.0;
    
    [Column("brush_temp_low_threshold")]
    public double BrushTempLowThreshold { get; set; } = 20.0;
    
    [Column("brush_power_high_threshold")]
    public double BrushPowerHighThreshold { get; set; } = 8.0;
    
    [Column("brush_power_low_threshold")]
    public double BrushPowerLowThreshold { get; set; } = 0.5;
    
    // 称台参数阈值
    [Column("scale_weight_high_threshold")]
    public double ScaleWeightHighThreshold { get; set; } = 2000.0;
    
    [Column("scale_weight_low_threshold")]
    public double ScaleWeightLowThreshold { get; set; } = 100.0;
    
    // 系统运行模式设置
    [Column("default_system_mode")]
    [MaxLength(20)]
    public string DefaultSystemMode { get; set; } = "auto";
    
    [Column("auto_error_reset")]
    public bool AutoErrorReset { get; set; } = false;
    
    [Column("error_check_interval")]
    public int ErrorCheckInterval { get; set; } = 10;
    
    // 报警设置
    [Column("alarm_enabled")]
    public bool AlarmEnabled { get; set; } = true;
    
    [Column("alarm_sound_enabled")]
    public bool AlarmSoundEnabled { get; set; } = true;
    
    [Column("alarm_email_enabled")]
    public bool AlarmEmailEnabled { get; set; } = false;
    
    [Column("alarm_sms_enabled")]
    public bool AlarmSmsEnabled { get; set; } = false;
    
    // 数据刷新设置
    [Column("data_refresh_interval")]
    public int DataRefreshInterval { get; set; } = 5;
    
    [Column("chart_data_points")]
    public int ChartDataPoints { get; set; } = 24;
    
    // 用户界面设置
    [Column("ui_theme")]
    [MaxLength(20)]
    public string UiTheme { get; set; } = "light";
    
    [Column("ui_language")]
    [MaxLength(10)]
    public string UiLanguage { get; set; } = "zh-CN";
    
    [Column("show_advanced_controls")]
    public bool ShowAdvancedControls { get; set; } = false;
    
    // 扩展设置 (JSON格式存储其他自定义参数)
    [Column("custom_settings", TypeName = "jsonb")]
    public string CustomSettings { get; set; } = "{}";
    
    // 时间戳
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // 导航属性
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    /// <summary>
    /// 获取自定义设置字典
    /// </summary>
    public Dictionary<string, object>? GetCustomSettings()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(CustomSettings);
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
    
    /// <summary>
    /// 设置自定义设置字典
    /// </summary>
    public void SetCustomSettings(Dictionary<string, object> settings)
    {
        CustomSettings = JsonSerializer.Serialize(settings);
    }
}

