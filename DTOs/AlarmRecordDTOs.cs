using System.ComponentModel.DataAnnotations;
using PumpRoomAutomationBackend.Models.Enums;

namespace PumpRoomAutomationBackend.DTOs;

/// <summary>
/// 报警记录响应DTO
/// </summary>
public class AlarmRecordDto
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public string? SiteName { get; set; }
    public string? SiteCode { get; set; }
    public string AlarmName { get; set; } = string.Empty;
    public string? AlarmDescription { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CurrentValue { get; set; }
    public string? AlarmValue { get; set; }
    public string? Unit { get; set; }
    public DateTime AlarmStartTime { get; set; }
    public DateTime? AlarmEndTime { get; set; }
    public DateTime? AcknowledgedTime { get; set; }
    public string? AcknowledgedBy { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 报警记录查询参数
/// </summary>
public class AlarmRecordQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? SiteId { get; set; }
    public AlarmStatus? Status { get; set; }
    public AlarmSeverity? Severity { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Search { get; set; }
}

/// <summary>
/// 分页报警记录响应
/// </summary>
public class PagedAlarmRecordsResponse
{
    public List<AlarmRecordDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// 报警记录统计DTO
/// </summary>
public class AlarmRecordStatisticsDto
{
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public int Total { get; set; }
    public int Active { get; set; }
    public int Acknowledged { get; set; }
    public int Cleared { get; set; }
}

/// <summary>
/// 确认报警请求
/// </summary>
public class AcknowledgeAlarmRequest
{
    [Required(ErrorMessage = "确认人不能为空")]
    public string AcknowledgedBy { get; set; } = string.Empty;
}

