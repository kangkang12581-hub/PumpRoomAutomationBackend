using NpgsqlTypes;

namespace PumpRoomAutomationBackend.Models.Enums;

/// <summary>
/// 报警严重级别
/// Alarm severity enumeration
/// </summary>
public enum AlarmSeverity
{
    /// <summary>
    /// 低级报警
    /// </summary>
    [PgName("low")]
    Low,
    
    /// <summary>
    /// 中级报警
    /// </summary>
    [PgName("medium")]
    Medium,
    
    /// <summary>
    /// 高级报警
    /// </summary>
    [PgName("high")]
    High,
    
    /// <summary>
    /// 紧急报警
    /// </summary>
    [PgName("critical")]
    Critical
}

/// <summary>
/// 报警状态
/// Alarm status enumeration
/// </summary>
public enum AlarmStatus
{
    /// <summary>
    /// 活跃报警
    /// </summary>
    [PgName("active")]
    Active,
    
    /// <summary>
    /// 已确认
    /// </summary>
    [PgName("acknowledged")]
    Acknowledged,
    
    /// <summary>
    /// 已解决
    /// </summary>
    [PgName("resolved")]
    Resolved,
    
    /// <summary>
    /// 已清除
    /// </summary>
    [PgName("cleared")]
    Cleared
}

