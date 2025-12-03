using System.ComponentModel.DataAnnotations;

namespace PumpRoomAutomationBackend.DTOs;

/// <summary>
/// 报警测试请求
/// </summary>
public class AlarmTestRequest
{
    /// <summary>
    /// 站点代码，例如 SITE_001
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SiteCode { get; set; } = string.Empty;

    /// <summary>
    /// 要写入的报警变量键（默认 IntTempHumidityCommError）
    /// </summary>
    [MaxLength(100)]
    public string? NodeKey { get; set; } = "IntTempHumidityCommError";

    /// <summary>
    /// 报警状态：true 触发，false 清除
    /// </summary>
    public bool Active { get; set; }
}



