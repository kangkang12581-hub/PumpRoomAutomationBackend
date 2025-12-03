namespace PumpRoomAutomationBackend.DTOs.Data;

public class FlowVelocityDto
{
    public long Id { get; set; }
    public int SiteId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Velocity { get; set; }
    public string? Status { get; set; }
    public short DataQuality { get; set; }
}

public class AddFlowVelocityRequest
{
    public int SiteId { get; set; }
    public DateTime? Timestamp { get; set; }
    public decimal Velocity { get; set; }
    public string? Status { get; set; }
    public short DataQuality { get; set; } = 100;
}

public class QueryFlowVelocityRequest
{
    public int SiteId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Interval { get; set; } = "minute";
    public int? Limit { get; set; }
}

public class AggregatedVelocityDto
{
    public DateTime TimeBucket { get; set; }
    public int DataCount { get; set; }
    public decimal AvgVelocity { get; set; }
    public decimal MinVelocity { get; set; }
    public decimal MaxVelocity { get; set; }
    public decimal? StdDevVelocity { get; set; }
}

public class FlowVelocityQueryResult
{
    public int SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Interval { get; set; } = "minute";
    public int TotalCount { get; set; }
    public List<AggregatedVelocityDto> Data { get; set; } = new();
}

