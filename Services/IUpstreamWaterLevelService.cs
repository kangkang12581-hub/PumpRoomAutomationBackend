using PumpRoomAutomationBackend.DTOs.Data;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 上游液位数据服务接口
/// </summary>
public interface IUpstreamWaterLevelService
{
    /// <summary>
    /// 添加单条液位数据
    /// </summary>
    Task<UpstreamWaterLevelDto> AddDataAsync(AddUpstreamWaterLevelRequest request);

    /// <summary>
    /// 批量添加液位数据
    /// </summary>
    Task<int> BatchAddDataAsync(BatchAddUpstreamWaterLevelRequest request);

    /// <summary>
    /// 查询液位数据（支持聚合）
    /// </summary>
    Task<UpstreamWaterLevelQueryResult> QueryDataAsync(QueryUpstreamWaterLevelRequest request);

    /// <summary>
    /// 获取最新的液位数据
    /// </summary>
    Task<UpstreamWaterLevelDto?> GetLatestDataAsync(int siteId);

    /// <summary>
    /// 删除指定时间范围的数据（用于数据清理）
    /// </summary>
    Task<int> DeleteDataBeforeAsync(DateTime beforeDate);
}

