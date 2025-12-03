namespace PumpRoomAutomationBackend.DTOs.Data;

/// <summary>
/// 瞬时流量数据传输对象
/// </summary>
public class InstantaneousFlowDto
{
    public long Id { get; set; }
    public int SiteId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal FlowRate { get; set; }
    public string? Status { get; set; }
    public short DataQuality { get; set; }
}

/// <summary>
/// 添加瞬时流量数据请求
/// </summary>
public class AddInstantaneousFlowRequest
{
    public int SiteId { get; set; }
    public DateTime? Timestamp { get; set; }
    public decimal FlowRate { get; set; }
    public string? Status { get; set; }
    public short DataQuality { get; set; } = 100;
}

/// <summary>
/// 查询瞬时流量数据请求
/// </summary>
public class QueryInstantaneousFlowRequest
{
    public int SiteId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Interval { get; set; } = "minute";
    public int? Limit { get; set; }
}

/// <summary>
/// 聚合后的瞬时流量数据
/// </summary>
public class AggregatedFlowDto
{
    public DateTime TimeBucket { get; set; }
    public int DataCount { get; set; }
    public decimal AvgFlow { get; set; }
    public decimal MinFlow { get; set; }
    public decimal MaxFlow { get; set; }
    public decimal? StdDevFlow { get; set; }
}

/// <summary>
/// 查询结果包装
/// </summary>
public class InstantaneousFlowQueryResult
{
    public int SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Interval { get; set; } = "minute";
    public int TotalCount { get; set; }
    public List<AggregatedFlowDto> Data { get; set; } = new();
}

