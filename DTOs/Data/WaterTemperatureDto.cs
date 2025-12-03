namespace PumpRoomAutomationBackend.DTOs.Data;

public class WaterTemperatureDto 
{ 
    public long Id { get; set; }
    public int SiteId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Temperature { get; set; }
    public string? Status { get; set; }
    public short DataQuality { get; set; }
}

public class AddWaterTemperatureRequest 
{ 
    public int SiteId { get; set; }
    public DateTime? Timestamp { get; set; }
    public decimal Temperature { get; set; }
    public string? Status { get; set; }
    public short DataQuality { get; set; } = 100;
}

public class QueryWaterTemperatureRequest 
{ 
    public int SiteId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Interval { get; set; } = "minute";
    public int? Limit { get; set; }
}

public class AggregatedTemperatureDto 
{ 
    public DateTime TimeBucket { get; set; }
    public int DataCount { get; set; }
    public decimal AvgTemperature { get; set; }
    public decimal MinTemperature { get; set; }
    public decimal MaxTemperature { get; set; }
    public decimal? StdDevTemperature { get; set; }
}

public class WaterTemperatureQueryResult 
{ 
    public int SiteId { get; set; }
    public string SiteName { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Interval { get; set; } = "minute";
    public int TotalCount { get; set; }
    public List<AggregatedTemperatureDto> Data { get; set; } = new();
}
