using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 下游液位数据服务接口
/// </summary>
public interface IDownstreamWaterLevelService
{
    /// <summary>
    /// 添加下游液位数据
    /// </summary>
    Task<DownstreamWaterLevel> AddDataAsync(AddDownstreamWaterLevelRequest request);

    /// <summary>
    /// 查询下游液位数据（支持时间聚合）
    /// </summary>
    Task<DownstreamWaterLevelQueryResult> QueryDataAsync(QueryDownstreamWaterLevelRequest request);

    /// <summary>
    /// 获取指定站点的最新液位数据
    /// </summary>
    Task<DownstreamWaterLevelDto?> GetLatestDataAsync(int siteId);
}

