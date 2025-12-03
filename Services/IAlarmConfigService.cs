using PumpRoomAutomationBackend.DTOs;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 报警配置服务接口
/// </summary>
public interface IAlarmConfigService
{
    /// <summary>
    /// 获取所有报警配置
    /// </summary>
    Task<List<AlarmConfigDto>> GetAllAsync();
    
    /// <summary>
    /// 分页查询报警配置
    /// </summary>
    Task<PagedAlarmConfigsResponse> GetPagedAsync(AlarmConfigQueryParams queryParams);
    
    /// <summary>
    /// 根据ID获取报警配置
    /// </summary>
    Task<AlarmConfigDto?> GetByIdAsync(int id);
    
    /// <summary>
    /// 根据报警代码获取报警配置
    /// </summary>
    Task<AlarmConfigDto?> GetByCodeAsync(string alarmCode);
    
    /// <summary>
    /// 根据类别获取报警配置
    /// </summary>
    Task<List<AlarmConfigDto>> GetByCategoryAsync(string category);
    
    /// <summary>
    /// 根据严重程度获取报警配置
    /// </summary>
    Task<List<AlarmConfigDto>> GetBySeverityAsync(string severity);
    
    /// <summary>
    /// 根据站点ID获取报警配置（包含全局配置）
    /// </summary>
    /// <param name="siteId">站点ID</param>
    /// <param name="includeGlobal">是否包含全局配置（默认true）</param>
    Task<List<AlarmConfigDto>> GetBySiteIdAsync(int siteId, bool includeGlobal = true);
    
    /// <summary>
    /// 创建报警配置
    /// </summary>
    Task<AlarmConfigDto> CreateAsync(CreateAlarmConfigRequest request);
    
    /// <summary>
    /// 更新报警配置
    /// </summary>
    Task<AlarmConfigDto> UpdateAsync(int id, UpdateAlarmConfigRequest request);
    
    /// <summary>
    /// 删除报警配置
    /// </summary>
    Task<bool> DeleteAsync(int id);
    
    /// <summary>
    /// 启用/禁用报警配置
    /// </summary>
    Task<bool> ToggleActiveAsync(int id, bool isActive);
    
    /// <summary>
    /// 获取报警配置统计信息
    /// </summary>
    Task<AlarmConfigStatisticsDto> GetStatisticsAsync();
    
    /// <summary>
    /// 获取所有报警类别
    /// </summary>
    Task<List<string>> GetAllCategoriesAsync();
    
    /// <summary>
    /// 获取所有严重程度级别
    /// </summary>
    Task<List<string>> GetAllSeverityLevelsAsync();
}

