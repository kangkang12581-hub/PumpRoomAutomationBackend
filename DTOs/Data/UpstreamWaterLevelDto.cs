namespace PumpRoomAutomationBackend.DTOs.Data;

/// <summary>
/// 上游液位数据传输对象
/// </summary>
public class UpstreamWaterLevelDto
{
    public long Id { get; set; }
    public int SiteId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal WaterLevel { get; set; }
    public string? Status { get; set; }
    public short DataQuality { get; set; }
}

/// <summary>
/// 添加上游液位数据请求
/// </summary>
public class AddUpstreamWaterLevelRequest
{
    public int SiteId { get; set; }
    public DateTime? Timestamp { get; set; }  // 如果为空，使用当前时间
    public decimal WaterLevel { get; set; }
    public string? Status { get; set; }
    public short DataQuality { get; set; } = 100;
}

/// <summary>
/// 批量添加上游液位数据请求
/// </summary>
public class BatchAddUpstreamWaterLevelRequest
{
    public List<AddUpstreamWaterLevelRequest> Data { get; set; } = new();
}

/// <summary>
/// 查询上游液位数据请求
/// </summary>
public class QueryUpstreamWaterLevelRequest
{
    /// <summary>
    /// 站点ID
    /// </summary>
    public int SiteId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 聚合间隔：minute, hour, day, month
    /// </summary>
    public string Interval { get; set; } = "minute";

    /// <summary>
    /// 最大返回记录数（防止查询过大）
    /// </summary>
    public int? Limit { get; set; }
}

/// <summary>
/// 聚合后的液位数据
/// </summary>
public class AggregatedWaterLevelDto
{
    /// <summary>
    /// 时间桶（聚合后的时间点）
    /// </summary>
    public DateTime TimeBucket { get; set; }

    /// <summary>
    /// 数据点数量
    /// </summary>
    public int DataCount { get; set; }

    /// <summary>
    /// 平均液位
    /// </summary>
    public decimal AvgLevel { get; set; }

    /// <summary>
    /// 最小液位
    /// </summary>
    public decimal MinLevel { get; set; }

    /// <summary>
    /// 最大液位
    /// </summary>
    public decimal MaxLevel { get; set; }

    /// <summary>
    /// 标准差（可选）
    /// </summary>
    public decimal? StdDevLevel { get; set; }
}

/// <summary>
/// 查询结果包装
/// </summary>
public class UpstreamWaterLevelQueryResult
{
    public int SiteId { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Interval { get; set; } = "minute";
    public int TotalCount { get; set; }
    public List<AggregatedWaterLevelDto> Data { get; set; } = new();
}

