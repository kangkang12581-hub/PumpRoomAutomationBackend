using PumpRoomAutomationBackend.DTOs.Data;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 瞬时流量数据服务接口
/// </summary>
public interface IInstantaneousFlowService
{
    /// <summary>
    /// 添加瞬时流量数据
    /// </summary>
    Task<InstantaneousFlow> AddDataAsync(AddInstantaneousFlowRequest request);

    /// <summary>
    /// 查询瞬时流量数据（支持时间聚合）
    /// </summary>
    Task<InstantaneousFlowQueryResult> QueryDataAsync(QueryInstantaneousFlowRequest request);

    /// <summary>
    /// 获取指定站点的最新流量数据
    /// </summary>
    Task<InstantaneousFlowDto?> GetLatestDataAsync(int siteId);
}

