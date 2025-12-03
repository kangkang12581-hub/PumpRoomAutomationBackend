using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs;
using PumpRoomAutomationBackend.Models.Entities;
using PumpRoomAutomationBackend.Models.Enums;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 报警记录服务接口
/// </summary>
public interface IAlarmRecordService
{
    Task<List<AlarmRecordDto>> GetAllAsync();
    Task<PagedAlarmRecordsResponse> GetPagedAsync(AlarmRecordQueryParams queryParams);
    Task<AlarmRecordDto?> GetByIdAsync(int id);
    Task<List<AlarmRecordDto>> GetBySiteIdAsync(int siteId);
    Task<bool> AcknowledgeAsync(int id, string acknowledgedBy);
    Task<bool> ClearAsync(int id);
    Task<bool> ClearBySiteIdAsync(int siteId);
    Task<AlarmRecordStatisticsDto> GetStatisticsAsync(int? siteId = null);
}

/// <summary>
/// 报警记录服务实现
/// </summary>
public class AlarmRecordService : IAlarmRecordService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AlarmRecordService> _logger;

    public AlarmRecordService(
        ApplicationDbContext context,
        ILogger<AlarmRecordService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AlarmRecordDto>> GetAllAsync()
    {
        var records = await _context.AlarmRecords
            .Include(a => a.Site)
            .OrderByDescending(a => a.AlarmStartTime)
            .ToListAsync();

        return records.Select(MapToDto).ToList();
    }

    public async Task<PagedAlarmRecordsResponse> GetPagedAsync(AlarmRecordQueryParams queryParams)
    {
        var query = _context.AlarmRecords
            .Include(a => a.Site)
            .AsQueryable();

        // 站点过滤
        if (queryParams.SiteId.HasValue)
        {
            query = query.Where(a => a.SiteId == queryParams.SiteId.Value);
        }

        // 状态过滤
        if (queryParams.Status.HasValue)
        {
            query = query.Where(a => a.Status == queryParams.Status.Value);
        }

        // 严重程度过滤
        if (queryParams.Severity.HasValue)
        {
            query = query.Where(a => a.Severity == queryParams.Severity.Value);
        }

        // 时间范围过滤
        if (queryParams.StartTime.HasValue)
        {
            query = query.Where(a => a.AlarmStartTime >= queryParams.StartTime.Value);
        }

        if (queryParams.EndTime.HasValue)
        {
            query = query.Where(a => a.AlarmStartTime <= queryParams.EndTime.Value);
        }

        // 搜索
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.ToLower();
            query = query.Where(a =>
                a.AlarmName.ToLower().Contains(search) ||
                (a.AlarmDescription != null && a.AlarmDescription.ToLower().Contains(search)) ||
                a.NodeName.ToLower().Contains(search));
        }

        // 排序
        query = query.OrderByDescending(a => a.AlarmStartTime);

        // 总数
        var total = await query.CountAsync();

        // 分页
        var records = await query
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();

        return new PagedAlarmRecordsResponse
        {
            Items = records.Select(MapToDto).ToList(),
            Total = total,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize,
            TotalPages = (int)Math.Ceiling(total / (double)queryParams.PageSize)
        };
    }

    public async Task<AlarmRecordDto?> GetByIdAsync(int id)
    {
        var record = await _context.AlarmRecords
            .Include(a => a.Site)
            .FirstOrDefaultAsync(a => a.Id == id);

        return record != null ? MapToDto(record) : null;
    }

    public async Task<List<AlarmRecordDto>> GetBySiteIdAsync(int siteId)
    {
        var records = await _context.AlarmRecords
            .Include(a => a.Site)
            .Where(a => a.SiteId == siteId)
            .OrderByDescending(a => a.AlarmStartTime)
            .ToListAsync();

        return records.Select(MapToDto).ToList();
    }

    public async Task<bool> AcknowledgeAsync(int id, string acknowledgedBy)
    {
        try
        {
            var record = await _context.AlarmRecords.FindAsync(id);
            if (record == null)
                return false;

            // 如果已经是已清除状态，直接返回成功
            if (record.Status == AlarmStatus.Cleared)
                return true;

            // 确认报警时，将状态改为已清除（Cleared），这样确认后的报警就不会显示在列表中
            record.Status = AlarmStatus.Cleared;
            record.AcknowledgedBy = acknowledgedBy;
            record.AcknowledgedTime = DateTime.UtcNow;
            record.AlarmEndTime = DateTime.UtcNow; // 设置报警结束时间
            record.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ 报警记录已确认并清除: ID={Id}, User={User}, Status=Cleared", id, acknowledgedBy);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 确认报警记录失败: ID={Id}", id);
            return false;
        }
    }

    public async Task<bool> ClearAsync(int id)
    {
        try
        {
            var record = await _context.AlarmRecords.FindAsync(id);
            if (record == null)
            {
                _logger.LogWarning("⚠️ 报警记录不存在: ID={Id}", id);
                return false;
            }

            // 如果已经是已清除状态，直接返回成功
            if (record.Status == AlarmStatus.Cleared)
            {
                _logger.LogInformation("ℹ️ 报警记录已经是已清除状态: ID={Id}", id);
                return true;
            }

            var now = DateTime.UtcNow;
            
            // 更新状态为已清除
            record.Status = AlarmStatus.Cleared;
            record.AlarmEndTime = now;
            record.UpdatedAt = now;
            
            // 如果没有确认者信息，设置默认值
            if (string.IsNullOrEmpty(record.AcknowledgedBy))
            {
                record.AcknowledgedBy = "System";
            }
            
            // 如果没有确认时间，设置当前时间
            if (!record.AcknowledgedTime.HasValue)
            {
                record.AcknowledgedTime = now;
            }

            // 保存到数据库
            var savedCount = await _context.SaveChangesAsync();
            _logger.LogInformation("✅ 报警记录已清除并保存到数据库: ID={Id}, Status=Cleared, SavedCount={SavedCount}", id, savedCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 清除报警记录失败: ID={Id}", id);
            return false;
        }
    }

    public async Task<bool> ClearBySiteIdAsync(int siteId)
    {
        try
        {
            // 查询该站点所有活跃状态的报警记录
            var records = await _context.AlarmRecords
                .Where(a => a.SiteId == siteId && a.Status == AlarmStatus.Active)
                .ToListAsync();

            if (records.Count == 0)
            {
                _logger.LogInformation("ℹ️ 站点 {SiteId} 没有活跃的报警记录", siteId);
                return true; // 没有需要清除的记录，返回成功
            }

            var now = DateTime.UtcNow;
            
            foreach (var record in records)
            {
                // 更新状态为已清除
                record.Status = AlarmStatus.Cleared;
                record.AlarmEndTime = now;
                record.UpdatedAt = now;
                
                // 如果没有确认者信息，设置默认值
                if (string.IsNullOrEmpty(record.AcknowledgedBy))
                {
                    record.AcknowledgedBy = "System";
                }
                
                // 如果没有确认时间，设置当前时间
                if (!record.AcknowledgedTime.HasValue)
                {
                    record.AcknowledgedTime = now;
                }
            }

            // 批量保存到数据库
            var savedCount = await _context.SaveChangesAsync();
            _logger.LogInformation("✅ 站点 {SiteId} 的所有活跃报警已清除: 共清除 {Count} 条报警记录，已保存到数据库", siteId, records.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 清除站点报警记录失败: SiteId={SiteId}", siteId);
            return false;
        }
    }

    public async Task<AlarmRecordStatisticsDto> GetStatisticsAsync(int? siteId = null)
    {
        var query = _context.AlarmRecords.AsQueryable();

        if (siteId.HasValue)
        {
            query = query.Where(a => a.SiteId == siteId.Value);
        }

        // 只统计当天的报警
        var todayStart = DateTime.UtcNow.Date;
        query = query.Where(a => a.AlarmStartTime >= todayStart);

        var allRecords = await query
            .Include(a => a.Site)
            .ToListAsync();

        // 按类别统计
        var byCategory = allRecords
            .GroupBy(a => GetCategoryFromAlarm(a))
            .ToDictionary(
                g => g.Key,
                g => g.Count()
            );

        return new AlarmRecordStatisticsDto
        {
            ByCategory = byCategory,
            Total = allRecords.Count,
            Active = allRecords.Count(a => a.Status == AlarmStatus.Active),
            Acknowledged = allRecords.Count(a => a.Status == AlarmStatus.Acknowledged),
            Cleared = allRecords.Count(a => a.Status == AlarmStatus.Cleared)
        };
    }

    private string GetCategoryFromAlarm(AlarmRecord alarm)
    {
        // 尝试从报警名称或描述中提取类别
        var name = alarm.AlarmName ?? "";
        var desc = alarm.AlarmDescription ?? "";

        if (name.Contains("重量") || name.Contains("称重") || name.Contains("Weight"))
            return "重量类";
        if (name.Contains("电机") || name.Contains("Motor") || name.Contains("变频器") || name.Contains("Drive"))
            return "电机类";
        if (name.Contains("流量") || name.Contains("液位") || name.Contains("Flow") || name.Contains("Level"))
            return "流体类";
        if (name.Contains("通讯") || name.Contains("通信") || name.Contains("CommError"))
            return "通讯类";
        if (name.Contains("控制") || name.Contains("Control"))
            return "控制类";

        return "其他";
    }

    private AlarmRecordDto MapToDto(AlarmRecord record)
    {
        return new AlarmRecordDto
        {
            Id = record.Id,
            SiteId = record.SiteId,
            SiteName = record.Site?.SiteName,
            SiteCode = record.Site?.SiteCode,
            AlarmName = record.AlarmName,
            AlarmDescription = record.AlarmDescription,
            NodeId = record.NodeId,
            NodeName = record.NodeName,
            Severity = record.Severity.ToString(),
            Status = record.Status.ToString(),
            CurrentValue = record.CurrentValue,
            AlarmValue = record.AlarmValue,
            Unit = record.Unit,
            AlarmStartTime = record.AlarmStartTime,
            AlarmEndTime = record.AlarmEndTime,
            AcknowledgedTime = record.AcknowledgedTime,
            AcknowledgedBy = record.AcknowledgedBy,
            Remarks = record.Remarks,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }
}

