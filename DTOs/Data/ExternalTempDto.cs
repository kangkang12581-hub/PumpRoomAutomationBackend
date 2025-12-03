using System.ComponentModel.DataAnnotations;

namespace PumpRoomAutomationBackend.DTOs.Data;

public class AddExternalTempRequest
{
    [Required] public int SiteId { get; set; }
    public DateTime? Timestamp { get; set; }
    [Required] public decimal Value { get; set; }
    public string? Status { get; set; }
    public int DataQuality { get; set; } = 100;
}

public class ExternalTempDto
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public string? Status { get; set; }
    public int DataQuality { get; set; }
}

public class QueryExternalTempRequest
{
    [Required] public int SiteId { get; set; }
    [Required] public DateTime StartTime { get; set; }
    [Required] public DateTime EndTime { get; set; }
    public string Interval { get; set; } = "minute";
    public int? Limit { get; set; }
}

public class AggregatedExternalTempDto
{
    public DateTime TimeBucket { get; set; }
    public int DataCount { get; set; }
    public decimal AvgValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public decimal? StdDevValue { get; set; }
}

public class ExternalTempQueryResult
{
    public int SiteId { get; set; }
    public string SiteName { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Interval { get; set; } = "";
    public List<AggregatedExternalTempDto> Data { get; set; } = new();
    public int TotalCount { get; set; }
}
