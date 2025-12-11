using System;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.DTOs.OpcUa;
using PumpRoomAutomationBackend.Services;
using PumpRoomAutomationBackend.Services.OpcUa;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// PLC 数据控制器 (向后兼容，使用默认站点)
/// PLC Data Controller (Backward compatible, uses default site)
/// </summary>
[ApiController]
[Route("api/plcdata")]
[Authorize]
public class PlcDataController : ControllerBase
{
    private readonly IOpcUaConnectionManager _connectionManager;
    private readonly ISiteService _siteService;
    private readonly ILogger<PlcDataController> _logger;
    private const string DEFAULT_SITE_CODE = "SITE_001"; // 默认站点
    
    public PlcDataController(
        IOpcUaConnectionManager connectionManager,
        ISiteService siteService,
        ILogger<PlcDataController> logger)
    {
        _connectionManager = connectionManager;
        _siteService = siteService;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取默认站点编码（优先使用标记为默认的站点，否则使用第一个已启用的站点）
    /// </summary>
    private async Task<string?> GetDefaultSiteCodeAsync()
    {
        try
        {
            var sites = await _siteService.GetEnabledSitesAsync();
            
            if (sites.Count == 0)
            {
                _logger.LogWarning("没有找到已启用的站点");
                return null;
            }
            
            // 优先使用标记为默认的站点
            var defaultSite = sites.FirstOrDefault(s => s.IsDefault);
            if (defaultSite != null)
            {
                return defaultSite.SiteCode;
            }
            
            // 否则使用第一个已启用的站点
            return sites[0].SiteCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取默认站点失败");
            return DEFAULT_SITE_CODE; // 如果失败，使用硬编码的默认值
        }
    }
    
    /// <summary>
    /// 读取节点值（兼容旧API）
    /// </summary>
    [HttpGet("read")]
    public async Task<ActionResult<ApiResponse<NodeDataResponse>>> ReadNode([FromQuery] string nodeId)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("📥 [PlcData] 收到读取节点请求");
        _logger.LogInformation("   NodeId: {NodeId}", nodeId);
        _logger.LogInformation("   IP地址: {IpAddress}", GetClientIpAddress());
        _logger.LogInformation("========================================");
        
        try
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                _logger.LogWarning("❌ 节点ID为空");
                return BadRequest(ApiResponse<NodeDataResponse>.Fail("节点ID不能为空", "INVALID_NODEID"));
            }
            
            string? siteCode = null;
            try
            {
                siteCode = await GetDefaultSiteCodeAsync();
                _logger.LogInformation("🏢 使用默认站点: {SiteCode}", siteCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取默认站点失败");
                return BadRequest(ApiResponse<NodeDataResponse>.Fail("获取站点失败", "GET_SITE_FAILED"));
            }
            
            if (siteCode == null)
            {
                _logger.LogWarning("❌ 没有可用的站点");
                return NotFound(ApiResponse<NodeDataResponse>.Fail("没有可用的站点", "NO_SITE_AVAILABLE"));
            }
            
            IOpcUaClient? client = null;
            try
            {
                client = _connectionManager.GetClient(siteCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取客户端失败: {SiteCode}", siteCode);
                return BadRequest(ApiResponse<NodeDataResponse>.Fail("获取客户端失败", "GET_CLIENT_FAILED"));
            }
            
            if (client == null)
            {
                _logger.LogWarning("❌ 站点 {SiteCode} 的客户端不存在", siteCode);
                return NotFound(ApiResponse<NodeDataResponse>.Fail($"站点 {siteCode} 不存在或未连接", "SITE_NOT_FOUND"));
            }
            
            _logger.LogInformation("🔗 站点连接状态: {IsConnected}", client.IsConnected);
            
            if (!client.IsConnected)
            {
                _logger.LogWarning("❌ 默认站点 {SiteCode} 未连接", siteCode);
                return BadRequest(ApiResponse<NodeDataResponse>.Fail($"默认站点 {siteCode} 未连接", "SITE_NOT_CONNECTED"));
            }
            
            _logger.LogInformation("📖 开始读取节点: {NodeId}", nodeId);
            
            Opc.Ua.DataValue? dataValue = null;
            try
            {
                dataValue = await client.ReadValueAsync(nodeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取节点异常: {NodeId}", nodeId);
                return BadRequest(ApiResponse<NodeDataResponse>.Fail($"读取节点失败: {ex.Message}", "READ_EXCEPTION"));
            }
            
            if (dataValue == null)
            {
                _logger.LogWarning("❌ 读取节点失败: {NodeId} - 返回值为空", nodeId);
                return NotFound(ApiResponse<NodeDataResponse>.Fail("读取节点失败", "READ_FAILED"));
            }
            
            _logger.LogInformation("✅ 读取节点成功: {NodeId} = {Value}", nodeId, dataValue.Value);
            
            var response = new NodeDataResponse
            {
                NodeId = nodeId,
                Value = dataValue.Value,
                Status = dataValue.StatusCode.ToString(),
                Timestamp = dataValue.SourceTimestamp.ToLocalTime().ToString("O"),
                Type = dataValue.Value?.GetType()?.Name
            };
            
            return Ok(ApiResponse<NodeDataResponse>.Ok(response, "读取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取节点失败: {NodeId} - 未预期的异常", nodeId);
            return BadRequest(ApiResponse<NodeDataResponse>.Fail($"读取节点失败: {ex.Message}", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 写入节点值（兼容旧API）
    /// </summary>
    [HttpPost("write")]
    public async Task<ActionResult<ApiResponse<WriteNodeResponse>>> WriteNode([FromBody] WriteNodeRequest request)
    {
        try
        {
            var siteCode = await GetDefaultSiteCodeAsync();
            if (siteCode == null)
            {
                return NotFound(ApiResponse<WriteNodeResponse>.Fail("没有可用的站点", "NO_SITE_AVAILABLE"));
            }
            
            var client = _connectionManager.GetClient(siteCode);
            if (client == null)
            {
                return NotFound(ApiResponse<WriteNodeResponse>.Fail($"站点 {siteCode} 不存在或未连接", "SITE_NOT_FOUND"));
            }
            
            if (!client.IsConnected)
            {
                return BadRequest(ApiResponse<WriteNodeResponse>.Fail($"默认站点 {siteCode} 未连接", "SITE_NOT_CONNECTED"));
            }
            
            // 记录请求详情
            _logger.LogInformation("📥 [PlcData] 写入节点请求: NodeId={NodeId}, Type={Type}, Value={Value}, ValueType={ValueType}", 
                request.NodeId, request.Type, request.Value, request.Value?.GetType()?.Name ?? "null");
            
            // 转换值类型
            var convertedValue = ConvertValueForWrite(request.Value, request.Type);
            _logger.LogInformation("🔄 [PlcData] 转换结果: ConvertedValue={ConvertedValue}, ConvertedType={ConvertedType}", 
                convertedValue, convertedValue?.GetType()?.Name ?? "null");
            
            if (convertedValue == null && request.Value != null)
            {
                _logger.LogError("❌ [PlcData] 值类型转换失败: Value={Value}, ValueType={ValueType}, Type={Type}", 
                    request.Value, request.Value?.GetType()?.Name ?? "null", request.Type);
                return BadRequest(ApiResponse<WriteNodeResponse>.Fail("值类型转换失败", "TYPE_CONVERSION_FAILED"));
            }
            
            var statusCode = await client.WriteValueAsync(request.NodeId, convertedValue!);
            
            var response = new WriteNodeResponse
            {
                Success = statusCode.ToString().Contains("Good"),
                NodeId = request.NodeId,
                Status = statusCode.ToString()
            };
            
            var message = response.Success ? "写入成功" : "写入失败";
            return Ok(ApiResponse<WriteNodeResponse>.Ok(response, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入节点失败: {NodeId}", request.NodeId);
            return StatusCode(500, ApiResponse<WriteNodeResponse>.Fail("写入节点失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 获取设备工作模式（本地/远程）
    /// Get Device Working Mode (Local/Remote)
    /// </summary>
    [HttpGet("mode-status")]
    public async Task<ActionResult<ApiResponse<object>>> GetModeStatus()
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("📥 [PlcData] 收到获取工作模式请求");
        _logger.LogInformation("========================================");
        
        try
        {
            string? siteCode = null;
            try
            {
                siteCode = await GetDefaultSiteCodeAsync();
                _logger.LogInformation("🏢 使用默认站点: {SiteCode}", siteCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取默认站点失败");
                return Ok(ApiResponse<object>.Ok(new { isRemote = false, mode = "local", available = false }, "获取站点失败"));
            }
            
            if (siteCode == null)
            {
                _logger.LogWarning("❌ 没有可用的站点");
                return Ok(ApiResponse<object>.Ok(new { isRemote = false, mode = "local", available = false }, "站点不可用"));
            }
            
            IOpcUaClient? client = null;
            try
            {
                client = _connectionManager.GetClient(siteCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取客户端失败: {SiteCode}", siteCode);
                return Ok(ApiResponse<object>.Ok(new { isRemote = false, mode = "local", available = false }, "获取客户端失败"));
            }
            
            if (client == null || !client.IsConnected)
            {
                _logger.LogWarning("❌ 站点未连接: {SiteCode}", siteCode);
                return Ok(ApiResponse<object>.Ok(new { isRemote = false, mode = "local", available = false }, "站点未连接"));
            }
            
            // 读取本地/远程模式节点 GHb_localRem
            // TRUE: 远程模式 (Remote), FALSE: 本地模式 (Local)
            var nodeId = "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHb_localRem";
            
            _logger.LogInformation("📖 读取工作模式节点: {NodeId}", nodeId);
            
            Opc.Ua.DataValue? dataValue = null;
            try
            {
                dataValue = await client.ReadValueAsync(nodeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取工作模式节点失败: {NodeId}", nodeId);
                return Ok(ApiResponse<object>.Ok(new { isRemote = false, mode = "local", available = false }, "读取节点失败"));
            }
            
            if (dataValue == null || dataValue.Value == null)
            {
                _logger.LogWarning("❌ 读取工作模式节点失败: 返回值为空");
                return Ok(ApiResponse<object>.Ok(new { isRemote = false, mode = "local", available = false }, "读取失败"));
            }
            
            bool isRemote = false;
            try
            {
                isRemote = Convert.ToBoolean(dataValue.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "转换工作模式值失败: {Value}", dataValue.Value);
                return Ok(ApiResponse<object>.Ok(new { isRemote = false, mode = "local", available = false }, "数据格式错误"));
            }
            
            string mode = isRemote ? "remote" : "local";
            
            _logger.LogInformation("✅ 工作模式读取成功: {Mode} (isRemote={IsRemote})", mode, isRemote);
            
            var result = new
            {
                isRemote = isRemote,
                mode = mode,
                available = true,
                timestamp = dataValue.SourceTimestamp.ToLocalTime().ToString("O")
            };
            
            return Ok(ApiResponse<object>.Ok(result, "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工作模式失败: 未预期的异常");
            return Ok(ApiResponse<object>.Ok(new { isRemote = false, mode = "local", available = false }, "获取失败"));
        }
    }
    
    /// <summary>
    /// 获取环境数据（温度、湿度）- 兼容旧API
    /// </summary>
    [HttpGet("environment")]
    public async Task<ActionResult<object>> GetEnvironmentData()
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("📥 [PlcData] 收到获取环境数据请求");
        _logger.LogInformation("========================================");
        
        try
        {
            var siteCode = await GetDefaultSiteCodeAsync();
            _logger.LogInformation("🏢 使用默认站点: {SiteCode}", siteCode);
            
            if (siteCode == null)
            {
                _logger.LogWarning("❌ 没有可用的站点，返回模拟数据");
                return Ok(new
                {
                    actIntTemp = 24.5,
                    actIntRH = 65,
                    actExtTemp = 18.2,
                    actExtRH = 72
                });
            }
            
            var client = _connectionManager.GetClient(siteCode);
            if (client == null || !client.IsConnected)
            {
                _logger.LogWarning("❌ 站点未连接，返回模拟数据");
                return Ok(new
                {
                    actIntTemp = 24.5,
                    actIntRH = 65,
                    actExtTemp = 18.2,
                    actExtRH = 72
                });
            }
            
            // 读取环境相关节点
            var nodeIds = new List<string>
            {
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actIntTemp",
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actIntRH",
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actExtTemp",
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actExtRH"
            };
            
            _logger.LogInformation("📖 读取 {Count} 个环境节点", nodeIds.Count);
            var results = await client.ReadValuesAsync(nodeIds);
            
            var data = new Dictionary<string, object?>();
            
            foreach (var (nodeId, dataValue) in results)
            {
                if (dataValue != null && dataValue.Value != null)
                {
                    if (nodeId.Contains("actIntTemp"))
                        data["actIntTemp"] = dataValue.Value;
                    else if (nodeId.Contains("actIntRH"))
                        data["actIntRH"] = dataValue.Value;
                    else if (nodeId.Contains("actExtTemp"))
                        data["actExtTemp"] = dataValue.Value;
                    else if (nodeId.Contains("actExtRH"))
                        data["actExtRH"] = dataValue.Value;
                }
            }
            
            _logger.LogInformation("✅ 环境数据读取成功: {Data}", System.Text.Json.JsonSerializer.Serialize(data));
            
            // 如果没有读取到数据，返回默认值
            if (data.Count == 0)
            {
                _logger.LogWarning("⚠️ 未读取到环境数据，返回默认值");
                return Ok(new
                {
                    actIntTemp = 24.5,
                    actIntRH = 65,
                    actExtTemp = 18.2,
                    actExtRH = 72
                });
            }
            
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取环境数据失败");
            // 返回模拟数据
            return Ok(new
            {
                actIntTemp = 24.5,
                actIntRH = 65,
                actExtTemp = 18.2,
                actExtRH = 72
            });
        }
    }
    
    /// <summary>
    /// 获取所有PLC数据（兼容旧API，返回默认站点的数据）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<Dictionary<string, object?>>>> GetAllData()
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("📥 [PlcData] 收到获取所有数据请求");
        _logger.LogInformation("   IP地址: {IpAddress}", GetClientIpAddress());
        _logger.LogInformation("========================================");
        
        try
        {
            var siteCode = await GetDefaultSiteCodeAsync();
            _logger.LogInformation("🏢 使用默认站点: {SiteCode}", siteCode);
            if (siteCode == null)
            {
                return NotFound(ApiResponse<Dictionary<string, object?>>.Fail("没有可用的站点", "NO_SITE_AVAILABLE"));
            }
            
            var client = _connectionManager.GetClient(siteCode);
            if (client == null || !client.IsConnected)
            {
                return BadRequest(ApiResponse<Dictionary<string, object?>>.Fail($"默认站点未连接", "SITE_NOT_CONNECTED"));
            }
            
            // 读取水流相关节点
            var nodeIds = new List<string>
            {
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actLevel",           // 下游液位
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actLevelDoppler",   // 上游液位
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actLiquidLevelDiff", // 液位差
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actFlowVelocity",   // 流速
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actFlow",           // 瞬时流量
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actTemp",          // 水温
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actFlowTotal",     // 累计流量
                // 电机相关
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actFreq",          // 频率
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_getFreq",          // 设定频率
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actCurrent",       // 电流
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actPower",          // 功率
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actTorqor",         // 转矩
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actMotorColiTemp",   // 绕组温度
                // 环境数据
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actIntTemp",        // 柜内温度
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actIntRH",           // 柜内湿度
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actExtTemp",         // 柜外温度
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_actExtRH",            // 柜外湿度
                // 称重数据
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.Ghr_actTareWeight",     // 毛重
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.Ghr_actNetWeight",      // 净重
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_totalWeightDay",    // 日总重
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_totalWeight",        // 总重
                // 报警数据
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_ALM.GAb_rotaryGrilleMotorTrip",      // 格栅电机跳闸
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_ALM.GAb_rotaryGrilleMotorOverLoad",  // 格栅电机过载
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_ALM.GAb_rotaryGrilleMotorOverTemp",   // 格栅电机超温
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_ALM.GAb_rotaryGrilleOverSpeed",      // 格栅电机超速
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_ALM.GAb_brushMotorTrip",              // 毛刷电机跳闸
                // 控制状态数据（GVL_IO）
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_IO.Gob_rotaryGrilleFor",              // 格栅电机运行
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_IO.Gob_BrushMotor",                  // 毛刷电机运行
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_IO.Gob_VibratMotor",                  // 振动电机运行
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_IO.Gob_coilHeating",                 // 防潮
                "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_IO.Gob_coolFan"                       // 散热
            };
            
            _logger.LogInformation("📖 批量读取 {Count} 个节点", nodeIds.Count);
            var results = await client.ReadValuesAsync(nodeIds);
            
            var result = new Dictionary<string, object?>
            {
                ["_siteCode"] = siteCode,
                ["_siteName"] = client.SiteName,
                ["_connected"] = client.IsConnected
            };
            
            // 映射节点ID到前端期望的字段名
            foreach (var (nodeId, dataValue) in results)
            {
                if (dataValue?.Value == null) continue;
                
                // 根据节点ID映射到前端字段名
                // 注意：actLevel 对应下游液位，actLevelDoppler 对应上游液位
                if (nodeId.Contains("GHr_actLevel") && !nodeId.Contains("Doppler") && !nodeId.Contains("LiquidLevelDiff"))
                    result["actLevelDoppler"] = Convert.ToDouble(dataValue.Value);  // 下游液位
                else if (nodeId.Contains("GHr_actLevelDoppler"))
                    result["actLevel"] = Convert.ToDouble(dataValue.Value);  // 上游液位
                else if (nodeId.Contains("GHr_actLiquidLevelDiff"))
                    result["actLiquidLevelDiff"] = Convert.ToDouble(dataValue.Value);  // 液位差
                else if (nodeId.Contains("GHr_actFlowVelocity"))
                    result["actFlowVelocity"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_actFlow") && !nodeId.Contains("Total") && !nodeId.Contains("Velocity"))
                    result["actFlow"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_actFlowTotal"))
                    result["actFlowTotal"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_actTemp") && !nodeId.Contains("Int") && !nodeId.Contains("Ext"))
                    result["actTemp"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_actFreq") && !nodeId.Contains("get"))
                    result["actFreq"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_getFreq"))
                    result["getFreq"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_actCurrent"))
                    result["actCurrent"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_actPower"))
                    result["actPower"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_actTorqor"))
                    result["actTorqor"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_actMotorColiTemp"))
                    result["actMotorColiTemp"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_actIntTemp"))
                    result["actIntTemp"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_actIntRH"))
                    result["actIntRH"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_actExtTemp"))
                    result["actExtTemp"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_actExtRH"))
                    result["actExtRH"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("Ghr_actTareWeight"))
                    result["actTareWeight"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("Ghr_actNetWeight"))
                    result["actNetWeight"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_totalWeightDay"))
                    result["totalWeightDay"] = Convert.ToDouble(dataValue.Value);
                else if (nodeId.Contains("GHr_totalWeight") && !nodeId.Contains("Day"))
                    result["totalWeight"] = Convert.ToDouble(dataValue.Value);
                // 报警数据
                else if (nodeId.Contains("GAb_rotaryGrilleMotorTrip"))
                    result["rotaryGrilleMotorTrip"] = Convert.ToBoolean(dataValue.Value);
                else if (nodeId.Contains("GAb_rotaryGrilleMotorOverLoad"))
                    result["rotaryGrilleMotorOverLoad"] = Convert.ToBoolean(dataValue.Value);
                else if (nodeId.Contains("GAb_rotaryGrilleMotorOverTemp"))
                    result["rotaryGrilleMotorOverTemp"] = Convert.ToBoolean(dataValue.Value);
                else if (nodeId.Contains("GAb_rotaryGrilleOverSpeed"))
                    result["rotaryGrilleOverSpeed"] = Convert.ToBoolean(dataValue.Value);
                else if (nodeId.Contains("GAb_brushMotorTrip"))
                    result["brushMotorTrip"] = Convert.ToBoolean(dataValue.Value);
                // 控制状态数据（GVL_IO）
                else if (nodeId.Contains("Gob_rotaryGrilleFor"))
                    result["rotaryGrilleFor"] = Convert.ToBoolean(dataValue.Value);
                else if (nodeId.Contains("Gob_BrushMotor"))
                    result["brushMotor"] = Convert.ToBoolean(dataValue.Value);
                else if (nodeId.Contains("Gob_VibratMotor"))
                    result["vibratMotor"] = Convert.ToBoolean(dataValue.Value);
                else if (nodeId.Contains("Gob_coilHeating"))
                    result["coilHeating"] = Convert.ToBoolean(dataValue.Value);
                else if (nodeId.Contains("Gob_coolFan"))
                    result["coolFan"] = Convert.ToBoolean(dataValue.Value);
            }
            
            _logger.LogInformation("✅ 获取PLC数据成功: {Count} 个字段", result.Count);
            return Ok(ApiResponse<Dictionary<string, object?>>.Ok(result, "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取PLC数据失败");
            return StatusCode(500, ApiResponse<Dictionary<string, object?>>.Fail("获取数据失败", "INTERNAL_ERROR"));
        }
    }
    
    private static object? ConvertValueForWrite(object? value, string? type)
    {
        if (value == null || type == null)
            return value;
        
        try
        {
            var typeLower = type.ToLower();
            var valueType = value.GetType().Name;
            
            // 处理 JsonElement（System.Text.Json 反序列化时可能产生）
            if (value is JsonElement jsonElement)
            {
                try
                {
                    // Boolean 类型需要特殊处理
                    if (typeLower == "boolean" || typeLower == "bool")
                    {
                        if (jsonElement.ValueKind == JsonValueKind.True)
                            return true;
                        if (jsonElement.ValueKind == JsonValueKind.False)
                            return false;
                        if (jsonElement.ValueKind == JsonValueKind.Number)
                            return jsonElement.GetInt32() != 0;
                        if (jsonElement.ValueKind == JsonValueKind.String)
                        {
                            var str = jsonElement.GetString();
                            if (bool.TryParse(str, out var parsedBool))
                                return parsedBool;
                            if (str?.Equals("1", StringComparison.OrdinalIgnoreCase) == true)
                                return true;
                            if (str?.Equals("0", StringComparison.OrdinalIgnoreCase) == true)
                                return false;
                        }
                        // 如果无法从 JsonElement 转换，继续到下面的 Boolean 处理
                    }
                    else
                    {
                        // 非 Boolean 类型的 JsonElement 处理
                        return typeLower switch
                        {
                            "int16" or "short" => jsonElement.GetInt16(),
                            "int32" or "int" => jsonElement.GetInt32(),
                            "int64" or "long" => jsonElement.GetInt64(),
                            "uint16" or "ushort" => jsonElement.GetUInt16(),
                            "uint32" or "uint" => jsonElement.GetUInt32(),
                            "uint64" or "ulong" => jsonElement.GetUInt64(),
                            "float" or "single" => jsonElement.GetSingle(),
                            "double" => jsonElement.GetDouble(),
                            "string" => jsonElement.GetString() ?? string.Empty,
                            _ => jsonElement.ValueKind == JsonValueKind.Number 
                                ? jsonElement.GetDouble() 
                                : jsonElement.GetString() ?? value
                        };
                    }
                }
                catch
                {
                    // 如果JsonElement转换失败，继续尝试其他方法
                }
            }
            
            // 如果值已经是目标类型，直接返回
            if (typeLower == "boolean" || typeLower == "bool")
            {
                if (value is bool boolValue)
                    return boolValue;
                
                // 处理字符串 "true"/"false"
                if (value is string strValue)
                {
                    if (bool.TryParse(strValue, out var parsedBool))
                        return parsedBool;
                    if (strValue.Equals("1", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (strValue.Equals("0", StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                
                // 处理数字：0 = false, 非0 = true
                if (value is int intValue)
                    return intValue != 0;
                if (value is long longValue)
                    return longValue != 0;
                if (value is double doubleValue)
                    return doubleValue != 0;
                
                // 最后尝试 Convert.ToBoolean
                return Convert.ToBoolean(value);
            }
            
            return typeLower switch
            {
                "int16" or "short" => Convert.ToInt16(value),
                "int32" or "int" => Convert.ToInt32(value),
                "int64" or "long" => Convert.ToInt64(value),
                "uint16" or "ushort" => Convert.ToUInt16(value),
                "uint32" or "uint" => Convert.ToUInt32(value),
                "uint64" or "ulong" => Convert.ToUInt64(value),
                "float" or "single" => Convert.ToSingle(value),
                "double" => Convert.ToDouble(value),
                "string" => value.ToString(),
                _ => value
            };
        }
        catch (Exception ex)
        {
            // 记录转换错误的详细信息
            Console.WriteLine($"❌ 转换失败: Value={value}, ValueType={value?.GetType()?.Name ?? "null"}, Type={type}, Error={ex.Message}, StackTrace={ex.StackTrace}");
            return null;
        }
    }
    
    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

