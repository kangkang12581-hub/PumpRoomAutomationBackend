using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 报警配置服务实现
/// </summary>
public class AlarmConfigService : IAlarmConfigService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AlarmConfigService> _logger;

    public AlarmConfigService(ApplicationDbContext context, ILogger<AlarmConfigService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AlarmConfigDto>> GetAllAsync()
    {
        var alarmConfigs = await _context.AlarmConfigs
            .Include(a => a.Site)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.AlarmCode)
            .ToListAsync();

        return alarmConfigs.Select(MapToDto).ToList();
    }

    public async Task<PagedAlarmConfigsResponse> GetPagedAsync(AlarmConfigQueryParams queryParams)
    {
        var query = _context.AlarmConfigs.Include(a => a.Site).AsQueryable();

        // 应用站点过滤条件
        if (queryParams.SiteId.HasValue)
        {
            if (queryParams.IncludeGlobal)
            {
                // 包含指定站点和全局配置
                query = query.Where(a => a.SiteId == queryParams.SiteId.Value || a.SiteId == null);
            }
            else
            {
                // 只包含指定站点
                query = query.Where(a => a.SiteId == queryParams.SiteId.Value);
            }
        }
        else if (!queryParams.IncludeGlobal)
        {
            // 只查询全局配置
            query = query.Where(a => a.SiteId == null);
        }

        // 应用其他过滤条件
        if (!string.IsNullOrWhiteSpace(queryParams.Category))
        {
            query = query.Where(a => a.AlarmCategory == queryParams.Category);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Severity))
        {
            query = query.Where(a => a.Severity == queryParams.Severity);
        }

        if (queryParams.IsActive.HasValue)
        {
            query = query.Where(a => a.IsActive == queryParams.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.SearchKeyword))
        {
            var keyword = queryParams.SearchKeyword.ToLower();
            query = query.Where(a => 
                a.AlarmName.ToLower().Contains(keyword) || 
                a.AlarmMessage.ToLower().Contains(keyword) ||
                a.AlarmCode.ToLower().Contains(keyword));
        }

        // 获取总数
        var totalCount = await query.CountAsync();

        // 应用分页
        var items = await query
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.AlarmCode)
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();

        return new PagedAlarmConfigsResponse
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize)
        };
    }

    public async Task<AlarmConfigDto?> GetByIdAsync(int id)
    {
        var alarmConfig = await _context.AlarmConfigs.FindAsync(id);
        return alarmConfig != null ? MapToDto(alarmConfig) : null;
    }

    public async Task<AlarmConfigDto?> GetByCodeAsync(string alarmCode)
    {
        var alarmConfig = await _context.AlarmConfigs
            .FirstOrDefaultAsync(a => a.AlarmCode == alarmCode);
        return alarmConfig != null ? MapToDto(alarmConfig) : null;
    }

    public async Task<List<AlarmConfigDto>> GetByCategoryAsync(string category)
    {
        var alarmConfigs = await _context.AlarmConfigs
            .Where(a => a.AlarmCategory == category)
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync();

        return alarmConfigs.Select(MapToDto).ToList();
    }

    public async Task<List<AlarmConfigDto>> GetBySeverityAsync(string severity)
    {
        var alarmConfigs = await _context.AlarmConfigs
            .Where(a => a.Severity == severity)
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync();

        return alarmConfigs.Select(MapToDto).ToList();
    }

    public async Task<List<AlarmConfigDto>> GetBySiteIdAsync(int siteId, bool includeGlobal = true)
    {
        var query = _context.AlarmConfigs
            .Include(a => a.Site)
            .AsQueryable();

        if (includeGlobal)
        {
            // 包含指定站点和全局配置
            query = query.Where(a => a.SiteId == siteId || a.SiteId == null);
        }
        else
        {
            // 只包含指定站点
            query = query.Where(a => a.SiteId == siteId);
        }

        var alarmConfigs = await query
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.AlarmCode)
            .ToListAsync();

        return alarmConfigs.Select(MapToDto).ToList();
    }

    public async Task<AlarmConfigDto> CreateAsync(CreateAlarmConfigRequest request)
    {
        // 检查在同一站点内代码是否已存在
        var exists = await _context.AlarmConfigs.AnyAsync(a => 
            a.AlarmCode == request.AlarmCode && a.SiteId == request.SiteId);
        if (exists)
        {
            var siteName = request.SiteId.HasValue ? $"站点ID {request.SiteId}" : "全局配置";
            throw new InvalidOperationException($"报警代码 {request.AlarmCode} 在{siteName}中已存在");
        }

        var alarmConfig = new AlarmConfig
        {
            SiteId = request.SiteId,
            AlarmCode = request.AlarmCode,
            AlarmName = request.AlarmName,
            AlarmMessage = request.AlarmMessage,
            AlarmCategory = request.AlarmCategory,
            Severity = request.Severity,
            TriggerVariable = request.TriggerVariable,
            TriggerBit = request.TriggerBit,
            AutoClear = request.AutoClear,
            RequireConfirmation = request.RequireConfirmation,
            Description = request.Description,
            SolutionGuide = request.SolutionGuide,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.AlarmConfigs.Add(alarmConfig);
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ 创建报警配置: {Code} - {Name}", alarmConfig.AlarmCode, alarmConfig.AlarmName);

        return MapToDto(alarmConfig);
    }

    public async Task<AlarmConfigDto> UpdateAsync(int id, UpdateAlarmConfigRequest request)
    {
        var alarmConfig = await _context.AlarmConfigs.FindAsync(id);
        if (alarmConfig == null)
        {
            throw new InvalidOperationException($"报警配置 ID {id} 不存在");
        }

        // 更新字段
        if (request.AlarmName != null) alarmConfig.AlarmName = request.AlarmName;
        if (request.AlarmMessage != null) alarmConfig.AlarmMessage = request.AlarmMessage;
        if (request.AlarmCategory != null) alarmConfig.AlarmCategory = request.AlarmCategory;
        if (request.Severity != null) alarmConfig.Severity = request.Severity;
        if (request.TriggerVariable != null) alarmConfig.TriggerVariable = request.TriggerVariable;
        if (request.TriggerBit.HasValue) alarmConfig.TriggerBit = request.TriggerBit;
        if (request.AutoClear.HasValue) alarmConfig.AutoClear = request.AutoClear.Value;
        if (request.RequireConfirmation.HasValue) alarmConfig.RequireConfirmation = request.RequireConfirmation.Value;
        if (request.Description != null) alarmConfig.Description = request.Description;
        if (request.SolutionGuide != null) alarmConfig.SolutionGuide = request.SolutionGuide;
        if (request.IsActive.HasValue) alarmConfig.IsActive = request.IsActive.Value;
        if (request.DisplayOrder.HasValue) alarmConfig.DisplayOrder = request.DisplayOrder.Value;

        alarmConfig.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ 更新报警配置: {Code} - {Name}", alarmConfig.AlarmCode, alarmConfig.AlarmName);

        return MapToDto(alarmConfig);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var alarmConfig = await _context.AlarmConfigs.FindAsync(id);
        if (alarmConfig == null)
        {
            return false;
        }

        _context.AlarmConfigs.Remove(alarmConfig);
        await _context.SaveChangesAsync();

        _logger.LogInformation("🗑️ 删除报警配置: {Code} - {Name}", alarmConfig.AlarmCode, alarmConfig.AlarmName);

        return true;
    }

    public async Task<bool> ToggleActiveAsync(int id, bool isActive)
    {
        var alarmConfig = await _context.AlarmConfigs.FindAsync(id);
        if (alarmConfig == null)
        {
            return false;
        }

        alarmConfig.IsActive = isActive;
        alarmConfig.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("🔄 切换报警配置状态: {Code} - {IsActive}", alarmConfig.AlarmCode, isActive);

        return true;
    }

    public async Task<AlarmConfigStatisticsDto> GetStatisticsAsync()
    {
        var allAlarmConfigs = await _context.AlarmConfigs.ToListAsync();

        var statistics = new AlarmConfigStatisticsDto
        {
            TotalCount = allAlarmConfigs.Count,
            ActiveCount = allAlarmConfigs.Count(a => a.IsActive),
            InactiveCount = allAlarmConfigs.Count(a => !a.IsActive),
            CategoryCounts = allAlarmConfigs
                .GroupBy(a => a.AlarmCategory)
                .ToDictionary(g => g.Key, g => g.Count()),
            SeverityCounts = allAlarmConfigs
                .GroupBy(a => a.Severity)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return statistics;
    }

    public async Task<List<string>> GetAllCategoriesAsync()
    {
        return await _context.AlarmConfigs
            .Select(a => a.AlarmCategory)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<List<string>> GetAllSeverityLevelsAsync()
    {
        return await _context.AlarmConfigs
            .Select(a => a.Severity)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
    }

    private static AlarmConfigDto MapToDto(AlarmConfig entity)
    {
        return new AlarmConfigDto
        {
            Id = entity.Id,
            SiteId = entity.SiteId,
            SiteName = entity.Site?.SiteName,
            SiteCode = entity.Site?.SiteCode,
            AlarmCode = entity.AlarmCode,
            AlarmName = entity.AlarmName,
            AlarmMessage = entity.AlarmMessage,
            AlarmCategory = entity.AlarmCategory,
            Severity = entity.Severity,
            TriggerVariable = entity.TriggerVariable,
            TriggerBit = entity.TriggerBit,
            AutoClear = entity.AutoClear,
            RequireConfirmation = entity.RequireConfirmation,
            Description = entity.Description,
            SolutionGuide = entity.SolutionGuide,
            IsActive = entity.IsActive,
            DisplayOrder = entity.DisplayOrder,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}

