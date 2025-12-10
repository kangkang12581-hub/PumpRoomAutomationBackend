using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.Models.Entities;
using PumpRoomAutomationBackend.Models.Enums;
using PumpRoomAutomationBackend.Services.OpcUa;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 报警监控服务 - 监控 OPC UA 节点触发报警
/// Alarm Monitor Service - monitors OPC UA nodes for alarm conditions
/// </summary>
public class AlarmMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOpcUaCache _opcUaCache;
    private readonly IOpcUaConnectionManager _connectionManager;
    private readonly ILogger<AlarmMonitorService> _logger;
    
    // 存储每个站点+报警代码的最后状态（用于检测状态变化）
    private readonly Dictionary<string, AlarmState> _alarmStates = new();
    private readonly object _stateLock = new object();

    public AlarmMonitorService(
        IServiceProvider serviceProvider,
        IOpcUaCache opcUaCache,
        IOpcUaConnectionManager connectionManager,
        ILogger<AlarmMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _opcUaCache = opcUaCache;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚨 启动报警监控服务");
        
        // 初始延迟，等待 OPC UA 连接建立
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAlarmsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 报警监控检查失败");
            }

            // 每10秒检查一次
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
        
        _logger.LogInformation("🛑 报警监控服务已停止");
    }

    private async Task CheckAlarmsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IAlarmNotificationService>();

        // 获取所有已启用的站点
        var enabledSites = await dbContext.SiteConfigs
            .Where(s => s.IsEnabled)
            .Select(s => new { s.Id, s.SiteCode })
            .ToListAsync();

        if (!enabledSites.Any())
        {
            return;
        }

        // 获取所有激活的报警配置（包括全局配置和特定站点配置）
        var alarmConfigs = await dbContext.AlarmConfigs
            .Where(a => a.IsActive)
            .ToListAsync();

        if (!alarmConfigs.Any())
        {
            return;
        }

        foreach (var site in enabledSites)
        {
            // 检查站点是否连接
            var client = _connectionManager.GetClient(site.SiteCode);
            if (client?.IsConnected != true)
            {
                continue;
            }

            // 获取适用于该站点的报警配置（站点特定 + 全局）
            var applicableConfigs = alarmConfigs
                .Where(a => a.SiteId == null || a.SiteId == site.Id)
                .ToList();

            foreach (var config in applicableConfigs)
            {
                try
                {
                    await CheckSingleAlarmAsync(
                        site.Id,
                        site.SiteCode,
                        config,
                        dbContext,
                        notificationService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [{SiteCode}] 检查报警 {AlarmCode} 失败",
                        site.SiteCode, config.AlarmCode);
                }
            }
        }
    }

    private async Task CheckSingleAlarmAsync(
        int siteId,
        string siteCode,
        AlarmConfig config,
        ApplicationDbContext dbContext,
        IAlarmNotificationService notificationService)
    {
        // 构建状态键：站点代码 + 报警代码
        var stateKey = $"{siteCode}:{config.AlarmCode}";

        // 检查触发条件
        var isTriggered = CheckTriggerCondition(siteCode, config, out var currentValue, out var nodeId);

        AlarmState previousState;
        lock (_stateLock)
        {
            _alarmStates.TryGetValue(stateKey, out previousState!);
        }

        // 状态变化：未触发 -> 触发
        if (isTriggered && (previousState == null || !previousState.IsTriggered))
        {
            _logger.LogWarning("🚨 [{SiteCode}] 报警触发: {AlarmName} ({AlarmCode}), 当前值: {Value}",
                siteCode, config.AlarmName, config.AlarmCode, currentValue);

            // 创建报警记录
            var alarmRecord = new AlarmRecord
            {
                SiteId = siteId,
                AlarmName = config.AlarmName,
                AlarmDescription = config.AlarmMessage,
                NodeId = nodeId ?? config.TriggerVariable ?? "N/A",
                NodeName = config.AlarmName,
                Severity = MapSeverity(config.Severity),
                Status = AlarmStatus.Active,
                CurrentValue = currentValue,
                AlarmValue = config.TriggerBit?.ToString(),
                Unit = "",
                AlarmStartTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.AlarmRecords.Add(alarmRecord);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation("✅ [{SiteCode}] 报警记录已创建: ID={Id}, {AlarmName}",
                siteCode, alarmRecord.Id, config.AlarmName);

            // 发送报警通知（异步，不阻塞监控循环）
            _ = Task.Run(async () =>
            {
                try
                {
                    await notificationService.SendAlarmNotificationAsync(alarmRecord);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [{SiteCode}] 发送报警通知失败: {AlarmName}",
                        siteCode, config.AlarmName);
                }
            });

            // 更新状态
            lock (_stateLock)
            {
                _alarmStates[stateKey] = new AlarmState
                {
                    IsTriggered = true,
                    AlarmRecordId = alarmRecord.Id,
                    LastCheckTime = DateTime.UtcNow
                };
            }
        }
        // 状态变化：触发 -> 未触发（自动清除）
        else if (!isTriggered && previousState != null && previousState.IsTriggered)
        {
            if (config.AutoClear)
            {
                _logger.LogInformation("✅ [{SiteCode}] 报警自动清除: {AlarmName} ({AlarmCode})",
                    siteCode, config.AlarmName, config.AlarmCode);

                // 查找并清除活动的报警记录
                if (previousState.AlarmRecordId.HasValue)
                {
                    var alarmRecord = await dbContext.AlarmRecords
                        .FirstOrDefaultAsync(a => a.Id == previousState.AlarmRecordId.Value);

                    if (alarmRecord != null && alarmRecord.Status == AlarmStatus.Active)
                    {
                        alarmRecord.Status = AlarmStatus.Cleared;
                        alarmRecord.AlarmEndTime = DateTime.UtcNow;
                        alarmRecord.UpdatedAt = DateTime.UtcNow;
                        alarmRecord.Remarks = "自动清除";

                        await dbContext.SaveChangesAsync();

                        _logger.LogInformation("✅ [{SiteCode}] 报警记录已清除: ID={Id}, {AlarmName}",
                            siteCode, alarmRecord.Id, config.AlarmName);
                    }
                }
            }

            // 更新状态
            lock (_stateLock)
            {
                _alarmStates[stateKey] = new AlarmState
                {
                    IsTriggered = false,
                    AlarmRecordId = null,
                    LastCheckTime = DateTime.UtcNow
                };
            }
        }
        // 状态未变化，更新最后检查时间
        else if (previousState != null)
        {
            lock (_stateLock)
            {
                previousState.LastCheckTime = DateTime.UtcNow;
            }
        }
    }

    private bool CheckTriggerCondition(
        string siteCode,
        AlarmConfig config,
        out string? currentValue,
        out string? nodeId)
    {
        currentValue = null;
        nodeId = config.TriggerVariable;

        if (string.IsNullOrEmpty(config.TriggerVariable))
        {
            return false;
        }

        try
        {
            // 从缓存读取节点值
            var cacheKey = $"{siteCode}:{config.TriggerVariable}";
            
            lock (_opcUaCache.CacheLock)
            {
                if (_opcUaCache.NodeCache.TryGetValue(cacheKey, out var snapshot))
                {
                    if (snapshot?.Value == null)
                    {
                        return false;
                    }

                    currentValue = snapshot.Value.ToString();

                    // 如果配置了触发位，检查位值
                    if (config.TriggerBit.HasValue)
                    {
                        // 尝试将值转换为整数并检查特定位
                        if (long.TryParse(snapshot.Value.ToString(), out var intValue))
                        {
                            var bitValue = (intValue & (1L << config.TriggerBit.Value)) != 0;
                            return bitValue;
                        }
                        // 如果是布尔值
                        else if (bool.TryParse(snapshot.Value.ToString(), out var boolValue))
                        {
                            return boolValue;
                        }
                    }
                    else
                    {
                        // 没有配置触发位，检查布尔值或非零值
                        if (bool.TryParse(snapshot.Value.ToString(), out var boolValue))
                        {
                            return boolValue;
                        }
                        else if (double.TryParse(snapshot.Value.ToString(), out var doubleValue))
                        {
                            return doubleValue != 0;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [{SiteCode}] 检查触发条件失败: {TriggerVariable}",
                siteCode, config.TriggerVariable);
        }

        return false;
    }

    private static AlarmSeverity MapSeverity(string severity)
    {
        return severity.ToLower() switch
        {
            "critical" => AlarmSeverity.Critical,
            "high" => AlarmSeverity.High,
            "error" => AlarmSeverity.High,
            "medium" => AlarmSeverity.Medium,
            "warning" => AlarmSeverity.Medium,
            "low" => AlarmSeverity.Low,
            "info" => AlarmSeverity.Low,
            _ => AlarmSeverity.Medium
        };
    }

    private class AlarmState
    {
        public bool IsTriggered { get; set; }
        public int? AlarmRecordId { get; set; }
        public DateTime LastCheckTime { get; set; }
    }
}


