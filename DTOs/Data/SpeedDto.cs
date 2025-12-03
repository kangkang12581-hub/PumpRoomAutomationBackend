using System.ComponentModel.DataAnnotations;

namespace PumpRoomAutomationBackend.DTOs.Data;

public class AddSpeedRequest
{
    [Required] public int SiteId { get; set; }
    public DateTime? Timestamp { get; set; }
    [Required] public decimal Speed { get; set; }
    public string? Status { get; set; }
    public int DataQuality { get; set; } = 100;
}

public class SpeedDto
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Speed { get; set; }
    public string? Status { get; set; }
    public int DataQuality { get; set; }
}

public class QuerySpeedRequest
{
    [Required] public int SiteId { get; set; }
    [Required] public DateTime StartTime { get; set; }
    [Required] public DateTime EndTime { get; set; }
    public string Interval { get; set; } = "minute";
    public int? Limit { get; set; }
}

public class AggregatedSpeedDto
{
    public DateTime TimeBucket { get; set; }
    public int DataCount { get; set; }
    public decimal AvgSpeed { get; set; }
    public decimal MinSpeed { get; set; }
    public decimal MaxSpeed { get; set; }
    public decimal? StdDevSpeed { get; set; }
}

public class SpeedQueryResult
{
    public int SiteId { get; set; }
    public string SiteName { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Interval { get; set; } = "";
    public List<AggregatedSpeedDto> Data { get; set; } = new();
    public int TotalCount { get; set; }
}

public class SpeedStatisticsDto
{
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public decimal AvgValue { get; set; }
    public int DataCount { get; set; }
}

