using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using PumpRoomAutomationBackend.DTOs;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.DTOs.OpcUa;
using PumpRoomAutomationBackend.Models.OpcUa;
using PumpRoomAutomationBackend.Services.OpcUa;
using Opc.Ua;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// OPC UA 数据控制器
/// OPC UA Data Controller
/// </summary>
[ApiController]
[Route("api/opcua")]
[Authorize]
public class OpcUaController : ControllerBase
{
    private readonly IOpcUaCache _cache;
    private readonly IOpcUaClient _client;
    private readonly IOpcUaConnectionManager _connectionManager;
    private readonly ILogger<OpcUaController> _logger;
    
    public OpcUaController(
        IOpcUaCache cache, 
        IOpcUaClient client,
        IOpcUaConnectionManager connectionManager,
        ILogger<OpcUaController> logger)
    {
        _cache = cache;
        _client = client;
        _connectionManager = connectionManager;
        _logger = logger;
    }
    
    /// <summary>
    /// 报警测试：向指定站点的报警节点写入布尔值
    /// </summary>
    [HttpPost("alarms/test")]
    [Authorize(Roles = "ROOT,ADMIN,OPERATOR")]
    public async Task<ActionResult<ApiResponse<object>>> TriggerAlarmTest([FromBody] AlarmTestRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.SiteCode))
        {
            return BadRequest(ApiResponse<object>.Fail("站点代码不能为空", "INVALID_PARAMETER"));
        }
        
        var nodeKey = string.IsNullOrWhiteSpace(request.NodeKey)
            ? "IntTempHumidityCommError"
            : request.NodeKey!;
        
        var plcMap = LoadPlcDataMap();
        if (!plcMap.TryGetValue(nodeKey, out var nodeId) || string.IsNullOrWhiteSpace(nodeId))
        {
            var msg = $"节点 {nodeKey} 未在 nodes.json 中配置";
            _logger.LogWarning(msg);
            Console.WriteLine($"[AlarmTest] {msg}");
            return StatusCode(500, ApiResponse<object>.Fail(msg, "NODE_NOT_CONFIGURED"));
        }
        
        var siteClient = _connectionManager.GetClient(request.SiteCode);
        if (siteClient == null)
        {
            var allSites = string.Join(", ", _connectionManager.GetAllConnectionStatus().Keys);
            var msg = $"站点 {request.SiteCode} 未找到或未初始化。可用站点: {allSites}";
            _logger.LogWarning(msg);
            Console.WriteLine($"[AlarmTest] {msg}");
            return BadRequest(ApiResponse<object>.Fail(msg, "SITE_NOT_FOUND"));
        }
        
        if (!siteClient.IsConnected)
        {
            var msg = $"站点 {request.SiteCode} 的 OPC UA 未连接";
            _logger.LogWarning(msg);
            Console.WriteLine($"[AlarmTest] {msg}");
            return StatusCode(503, ApiResponse<object>.Fail(msg, "SITE_NOT_CONNECTED"));
        }
        
        Console.WriteLine($"[AlarmTest] 写入节点 {nodeKey}({nodeId}) => {request.Active}, 站点={request.SiteCode}");
        var status = await siteClient.WriteValueAsync(nodeId, request.Active);
        
        var result = new
        {
            request.SiteCode,
            nodeKey,
            nodeId,
            active = request.Active,
            status = status.ToString()
        };
        
        if (Opc.Ua.StatusCode.IsGood(status))
        {
            _logger.LogInformation("✅ 报警测试写入成功: Site={SiteCode}, Node={NodeKey}, Active={Active}", 
                request.SiteCode, nodeKey, request.Active);
            Console.WriteLine($"[AlarmTest] 写入成功 {status}");
            return Ok(ApiResponse<object>.Ok(result, "报警测试写入成功"));
        }
        
        var errorMsg = $"报警测试写入失败: {status}";
        _logger.LogWarning(errorMsg);
        Console.WriteLine($"[AlarmTest] 写入失败 {status}");
        return StatusCode(500, ApiResponse<object>.Fail(errorMsg, "WRITE_FAILED"));
    }
    
    /// <summary>
    /// 获取所有 PLC 数据
    /// Get All PLC Data
    /// </summary>
    [HttpGet("plc-data")]
    public ActionResult<ApiResponse<Dictionary<string, object?>>> GetAllPlcData([FromQuery] string? conn = null)
    {
        try
        {
            var plcMap = LoadPlcDataMap();
            var result = new Dictionary<string, object?>();
            
            lock (_cache.CacheLock)
            {
                foreach (var kv in plcMap)
                {
                    var key = kv.Key;
                    var nodeId = kv.Value;
                    
                    if (string.IsNullOrWhiteSpace(nodeId))
                    {
                        result[key] = null;
                        continue;
                    }
                    
                    var cacheKey = string.IsNullOrWhiteSpace(conn) ? nodeId : $"{conn}:{nodeId}";
                    
                    if (!_cache.NodeCache.TryGetValue(cacheKey, out var snap))
                    {
                        result[key] = null;
                        continue;
                    }
                    
                    result[key] = snap?.Value;
                }
            }
            
            return Ok(ApiResponse<Dictionary<string, object?>>.Ok(result, "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 PLC 数据时发生错误");
            return StatusCode(500, ApiResponse<Dictionary<string, object?>>.Fail("获取失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 批量读取所有参数设定节点的实时值（直接从PLC读取，不走缓存）
    /// Batch Read All Parameter Setting Nodes Real-time Values (Direct PLC Read)
    /// </summary>
    /// <param name="siteCode">站点代码，如 site-a, site-b，不传则使用默认站点</param>
    [HttpGet("parameters/read-all")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, object?>>>> ReadAllParameters([FromQuery] string? siteCode = null)
    {
        try
        {
            _logger.LogInformation("📥 收到批量读取参数请求, siteCode={SiteCode}", siteCode ?? "default");
            
            // 定义所有参数节点映射（与前端 ParametersModule.vue 中的 opcNodes 保持一致）
            var parameterNodes = new Dictionary<string, string>
            {
                // 速度参数
                { "setVelocityHighLimit", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_setVelocityHighLimit" },
                { "setVelocityLowLimit", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_setVelocityLowLimit" },
                { "setMVelocity", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_setMVelocity" },
                { "setVelocityAlm", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_setVelocityAlm" },
                { "setLiquidLevelDiff", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_setLiquidLevelDiff" },
                { "setP", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_setP" },
                { "setI", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_setI" },
                { "setD", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_setD" },
                
                // 绕组加热参数
                { "motorColiHeatTemp", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_MotorColiHeatTemp" },
                { "motorColiStopTemp", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_MotorColiStopTemp" },
                { "motorColiAlmTemp", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_MotorColiAlmTemp" },
                { "motorColiCoolStartTemp", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_MotorColiCoolStartTemp" },
                { "motorColiCoolStopTemp", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_MotorColiCoolStopTemp" },
                
                // 延时参数
                { "pumpRunTime", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_pumpRunTime" },
                { "pumpStopTime", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_pumpStopTime" },
                
                // 流体报警参数
                { "almLevelDiff", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_almLevelDiff" },
                { "almLevelDopplerHigh", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_almLevelDopplerHigh" },
                { "almFlowLow", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHr_almFlowLow" },
                
                // 容器重量参数
                { "setMaxTareWeight", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.Ghr_setMaxTareWeight" },
                { "setWarnWeight", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.Ghr_setWarnWeight" },
                { "setAlarmNetWeight", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.Ghr_setAlarmNetWeight" },
                
                // HART通信状态
                { "hartEn", "ns=4;s=|var|Inovance-ARM-Linux.Application.GVL_HMI.GHb_hartEn" }
            };

            var result = new Dictionary<string, object?>();
            
            // 根据 siteCode 获取对应的客户端并读取参数
            if (string.IsNullOrWhiteSpace(siteCode))
            {
                // 未指定站点，使用默认客户端
                _logger.LogInformation("使用默认OPC UA客户端");
                
                if (_client == null || !_client.IsConnected)
                {
                    _logger.LogWarning("⚠️ 默认OPC UA客户端未连接");
                    return StatusCode(503, ApiResponse<Dictionary<string, object?>>.Fail(
                        "默认站点的 OPC UA 未连接", "SERVICE_UNAVAILABLE"));
                }
                
                // 批量并发读取所有节点
                var readTasks = parameterNodes.Select(async kvp =>
                {
                    try
                    {
                        var dv = await _client.ReadValueAsync(kvp.Value);
                        return new { Key = kvp.Key, Value = dv?.Value as object };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "读取节点 {NodeKey} 失败", kvp.Key);
                        return new { Key = kvp.Key, Value = (object?)null };
                    }
                });
                
                var readResults = await Task.WhenAll(readTasks);
                foreach (var item in readResults)
                {
                    result[item.Key] = item.Value;
                }
            }
            else
            {
                // 从连接管理器获取指定站点的客户端
                _logger.LogInformation("尝试获取站点 {SiteCode} 的OPC UA客户端进行参数读取...", siteCode);
                
                // 获取所有已注册的站点状态，用于诊断
                var allStatus = _connectionManager.GetAllConnectionStatus();
                _logger.LogInformation("已注册的站点: {Sites}", string.Join(", ", allStatus.Keys));
                
                var siteClient = _connectionManager.GetClient(siteCode);
                
                if (siteClient == null)
                {
                    _logger.LogWarning("⚠️ 未找到站点 {SiteCode} 的OPC UA客户端。已注册站点: {Sites}", 
                        siteCode, string.Join(", ", allStatus.Keys));
                    return BadRequest(ApiResponse<Dictionary<string, object?>>.Fail(
                        $"站点 {siteCode} 未配置或未初始化。可用站点: {string.Join(", ", allStatus.Keys)}", 
                        "SITE_NOT_FOUND"));
                }
                
                if (!siteClient.IsConnected)
                {
                    _logger.LogWarning("⚠️ 站点 {SiteCode} 的OPC UA客户端未连接", siteCode);
                    return StatusCode(503, ApiResponse<Dictionary<string, object?>>.Fail(
                        $"站点 {siteCode} 的 OPC UA 未连接", "SERVICE_UNAVAILABLE"));
                }
                
                _logger.LogInformation("使用站点 {SiteCode} 的OPC UA客户端", siteCode);
                
                // 批量并发读取所有节点（SiteOpcUaClient也有ReadValueAsync方法）
                var readTasks = parameterNodes.Select(async kvp =>
                {
                    try
                    {
                        var dv = await siteClient.ReadValueAsync(kvp.Value);
                        return new { Key = kvp.Key, Value = dv?.Value as object };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[{SiteCode}] 读取节点 {NodeKey} 失败", siteCode, kvp.Key);
                        return new { Key = kvp.Key, Value = (object?)null };
                    }
                });
                
                var readResults = await Task.WhenAll(readTasks);
                foreach (var item in readResults)
                {
                    result[item.Key] = item.Value;
                }
            }

            _logger.LogInformation("✅ 批量读取参数完成: {Count} 个节点，站点={SiteCode}", result.Count, siteCode ?? "default");
            return Ok(ApiResponse<Dictionary<string, object?>>.Ok(result, "批量读取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量读取参数时发生错误");
            return StatusCode(500, ApiResponse<Dictionary<string, object?>>.Fail(
                "批量读取失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 读取单个节点值
    /// Read Single Node Value
    /// </summary>
    [HttpGet("read")]
    public async Task<ActionResult<ApiResponse<object>>> ReadNode([FromQuery] string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            return BadRequest(ApiResponse<object>.Fail("节点ID不能为空", "INVALID_PARAMETER"));
        
        try
        {
            var dv = await _client.ReadValueAsync(nodeId);
            if (dv == null)
                return StatusCode(503, ApiResponse<object>.Fail("OPC UA 未连接或读取失败", "SERVICE_UNAVAILABLE"));
            
            var result = new
            {
                nodeId,
                value = dv.Value,
                status = dv.StatusCode.ToString(),
                timestamp = dv.SourceTimestamp.ToLocalTime().ToString("O")
            };
            
            return Ok(ApiResponse<object>.Ok(result, "读取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取节点 {NodeId} 时发生错误", nodeId);
            return StatusCode(500, ApiResponse<object>.Fail("读取失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 浏览节点
    /// Browse Nodes
    /// </summary>
    [HttpGet("browse")]
    public async Task<ActionResult<ApiResponse<object>>> BrowseNodes([FromQuery] string? nodeId = null)
    {
        try
        {
            var nodes = await _client.BrowseNodesAsync(nodeId);
            var result = new
            {
                parentNodeId = nodeId ?? "Root",
                nodes = nodes.Select(n => new
                {
                    nodeId = n.NodeId,
                    browseName = n.BrowseName,
                    displayName = n.DisplayName,
                    nodeClass = n.NodeClass.ToString(),
                    hasValue = n.HasValue
                })
            };
            
            return Ok(ApiResponse<object>.Ok(result, "浏览成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "浏览节点时发生错误");
            return StatusCode(500, ApiResponse<object>.Fail("浏览失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 写入节点值
    /// Write Node Value
    /// </summary>
    [HttpPost("write")]
    public async Task<ActionResult<ApiResponse<object>>> WriteNode([FromBody] WriteRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.NodeId))
            return BadRequest(ApiResponse<object>.Fail("请求参数无效", "INVALID_PARAMETER"));
        
        try
        {
            if (!TryConvertJsonToType(request.Type, request.Value, out var typedValue, out var error))
            {
                return BadRequest(ApiResponse<object>.Fail(error ?? "类型转换失败", "TYPE_CONVERSION_ERROR"));
            }
            
            var status = await _client.WriteValueAsync(request.NodeId, typedValue!);
            
            if (Opc.Ua.StatusCode.IsGood(status))
            {
                var result = new
                {
                    nodeId = request.NodeId,
                    status = status.ToString()
                };
                return Ok(ApiResponse<object>.Ok(result, "写入成功"));
            }
            
            return StatusCode(500, ApiResponse<object>.Fail($"写入失败: {status}", "WRITE_FAILED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入节点 {NodeId} 时发生错误", request.NodeId);
            return StatusCode(500, ApiResponse<object>.Fail("写入失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 批量写入节点值
    /// Bulk Write Node Values
    /// </summary>
    [HttpPost("bulk-write")]
    public async Task<ActionResult<ApiResponse<object>>> BulkWrite([FromBody] BulkWriteRequest request)
    {
        if (request == null || request.Writes == null || request.Writes.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("请求参数无效", "INVALID_PARAMETER"));
        
        try
        {
            // 根据站点代码选择客户端
            string siteCode = request.SiteCode ?? "default";
            _logger.LogInformation("📝 批量写入参数, 站点={SiteCode}, 节点数={Count}", siteCode, request.Writes.Count);
            
            // 判断使用哪个客户端
            if (string.IsNullOrWhiteSpace(request.SiteCode))
            {
                // 使用默认客户端
                if (_client == null || !_client.IsConnected)
                {
                    _logger.LogWarning("⚠️ 默认OPC UA客户端未连接");
                    return StatusCode(503, ApiResponse<object>.Fail(
                        "默认站点的 OPC UA 未连接", "SERVICE_UNAVAILABLE"));
                }
                
                var results = await ExecuteBulkWriteAsync(_client, request.Writes);
                var result = new { count = results.Count, results };
                _logger.LogInformation("✅ 批量写入完成: {Count} 个节点", results.Count);
                return Ok(ApiResponse<object>.Ok(result, "批量写入完成"));
            }
            else
            {
                // 使用指定站点的客户端
                _logger.LogInformation("尝试获取站点 {SiteCode} 的OPC UA客户端...", request.SiteCode);
                
                // 获取所有已注册的站点状态，用于诊断
                var allStatus = _connectionManager.GetAllConnectionStatus();
                _logger.LogInformation("已注册的站点: {Sites}", string.Join(", ", allStatus.Keys));
                
                var siteClient = _connectionManager.GetClient(request.SiteCode);
                
                if (siteClient == null)
                {
                    _logger.LogWarning("⚠️ 未找到站点 {SiteCode} 的OPC UA客户端。已注册站点: {Sites}", 
                        request.SiteCode, string.Join(", ", allStatus.Keys));
                    return BadRequest(ApiResponse<object>.Fail(
                        $"站点 {request.SiteCode} 未配置或未初始化。可用站点: {string.Join(", ", allStatus.Keys)}", 
                        "SITE_NOT_FOUND"));
                }
                
                if (!siteClient.IsConnected)
                {
                    _logger.LogWarning("⚠️ 站点 {SiteCode} 的OPC UA客户端未连接", request.SiteCode);
                    return StatusCode(503, ApiResponse<object>.Fail(
                        $"站点 {request.SiteCode} 的 OPC UA 未连接", "SERVICE_UNAVAILABLE"));
                }
                
                var results = await ExecuteBulkWriteToSiteAsync(siteClient, request.Writes);
                var result = new { count = results.Count, results, siteCode = request.SiteCode };
                _logger.LogInformation("✅ 站点 {SiteCode} 批量写入完成: {Count} 个节点", request.SiteCode, results.Count);
                return Ok(ApiResponse<object>.Ok(result, $"批量写入完成（站点：{request.SiteCode}）"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量写入时发生错误");
            return StatusCode(500, ApiResponse<object>.Fail("批量写入失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 执行批量写入（默认客户端）
    /// </summary>
    private async Task<List<BulkWriteResult>> ExecuteBulkWriteAsync(IOpcUaClient client, List<WriteRequest> writes)
    {
        var results = new List<BulkWriteResult>();
        
        foreach (var w in writes)
        {
            if (string.IsNullOrWhiteSpace(w.NodeId))
            {
                results.Add(new BulkWriteResult
                {
                    NodeId = string.Empty,
                    Status = "Bad",
                    Error = "节点ID不能为空"
                });
                continue;
            }
            
            if (!TryConvertJsonToType(w.Type, w.Value, out var typedValue, out var error))
            {
                results.Add(new BulkWriteResult
                {
                    NodeId = w.NodeId,
                    Status = "Bad",
                    Error = error ?? "类型转换失败"
                });
                continue;
            }
            
            try
            {
                var status = await client.WriteValueAsync(w.NodeId, typedValue!);
                results.Add(new BulkWriteResult
                {
                    NodeId = w.NodeId,
                    Status = status.ToString()
                });
            }
            catch (Exception ex)
            {
                results.Add(new BulkWriteResult
                {
                    NodeId = w.NodeId ?? string.Empty,
                    Status = "Exception",
                    Error = ex.Message
                });
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// 执行批量写入（站点客户端）
    /// </summary>
    private async Task<List<BulkWriteResult>> ExecuteBulkWriteToSiteAsync(SiteOpcUaClient client, List<WriteRequest> writes)
    {
        var results = new List<BulkWriteResult>();
        
        foreach (var w in writes)
        {
            if (string.IsNullOrWhiteSpace(w.NodeId))
            {
                results.Add(new BulkWriteResult
                {
                    NodeId = string.Empty,
                    Status = "Bad",
                    Error = "节点ID不能为空"
                });
                continue;
            }
            
            if (!TryConvertJsonToType(w.Type, w.Value, out var typedValue, out var error))
            {
                results.Add(new BulkWriteResult
                {
                    NodeId = w.NodeId,
                    Status = "Bad",
                    Error = error ?? "类型转换失败"
                });
                continue;
            }
            
            try
            {
                var status = await client.WriteValueAsync(w.NodeId, typedValue!);
                results.Add(new BulkWriteResult
                {
                    NodeId = w.NodeId,
                    Status = status.ToString()
                });
            }
            catch (Exception ex)
            {
                results.Add(new BulkWriteResult
                {
                    NodeId = w.NodeId ?? string.Empty,
                    Status = "Exception",
                    Error = ex.Message
                });
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// 获取连接状态
    /// Get Connection Status
    /// </summary>
    [HttpGet("status")]
    public ActionResult<ApiResponse<object>> GetStatus()
    {
        var result = new
        {
            connected = _client.IsConnected,
            nodeCount = _cache.NodeCache.Count,
            timestamp = DateTime.UtcNow
        };
        
        return Ok(ApiResponse<object>.Ok(result, "获取成功"));
    }
    
    private static Dictionary<string, string> LoadPlcDataMap()
    {
        const string configPath = "nodes.json";
        if (!System.IO.File.Exists(configPath))
            return new Dictionary<string, string>();
        
        try
        {
            var json = System.IO.File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<NodesConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return config?.PlcData ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
    
    private static bool TryConvertJsonToType(string? typeName, JsonElement? valueElement, out object? typedValue, out string? error)
    {
        typedValue = null;
        error = null;
        
        if (valueElement is null || valueElement.Value.ValueKind == JsonValueKind.Undefined)
        {
            error = "缺少值";
            return false;
        }
        
        try
        {
            var t = (typeName ?? string.Empty).Trim();
            switch (t.ToLowerInvariant())
            {
                case "bool":
                case "boolean":
                    typedValue = valueElement.Value.ValueKind switch
                    {
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.String => bool.Parse(valueElement.Value.GetString() ?? "false"),
                        _ => valueElement.Value.GetBoolean()
                    };
                    return true;
                    
                case "int16":
                    typedValue = valueElement.Value.ValueKind == JsonValueKind.String
                        ? short.Parse(valueElement.Value.GetString() ?? "0")
                        : valueElement.Value.GetInt16();
                    return true;
                    
                case "uint16":
                    typedValue = valueElement.Value.ValueKind == JsonValueKind.String
                        ? ushort.Parse(valueElement.Value.GetString() ?? "0")
                        : valueElement.Value.GetUInt16();
                    return true;
                    
                case "int32":
                case "int":
                    typedValue = valueElement.Value.ValueKind == JsonValueKind.String
                        ? int.Parse(valueElement.Value.GetString() ?? "0")
                        : valueElement.Value.GetInt32();
                    return true;
                    
                case "uint32":
                    typedValue = valueElement.Value.ValueKind == JsonValueKind.String
                        ? uint.Parse(valueElement.Value.GetString() ?? "0")
                        : valueElement.Value.GetUInt32();
                    return true;
                    
                case "float":
                case "single":
                    typedValue = valueElement.Value.ValueKind == JsonValueKind.String
                        ? float.Parse(valueElement.Value.GetString() ?? "0")
                        : (float)valueElement.Value.GetDouble();
                    return true;
                    
                case "double":
                    typedValue = valueElement.Value.ValueKind == JsonValueKind.String
                        ? double.Parse(valueElement.Value.GetString() ?? "0")
                        : valueElement.Value.GetDouble();
                    return true;
                    
                case "string":
                    typedValue = valueElement.Value.ValueKind switch
                    {
                        JsonValueKind.String => valueElement.Value.GetString(),
                        JsonValueKind.Number => valueElement.Value.ToString(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Null => null,
                        _ => valueElement.Value.ToString()
                    };
                    return true;
                    
                default:
                    error = $"不支持的类型 '{typeName}'";
                    return false;
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
    
    // ==================== 多站点 API ====================
    
    /// <summary>
    /// 读取指定站点的节点值
    /// </summary>
    [HttpGet("sites/{siteCode}/read")]
    public async Task<ActionResult<ApiResponse<NodeDataResponse>>> ReadSiteNode(
        string siteCode,
        [FromQuery] string nodeId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return BadRequest(ApiResponse<NodeDataResponse>.Fail("节点ID不能为空", "INVALID_NODEID"));
            }
            
            var client = _connectionManager.GetClient(siteCode);
            if (client == null)
            {
                return NotFound(ApiResponse<NodeDataResponse>.Fail($"站点 {siteCode} 不存在或未连接", "SITE_NOT_FOUND"));
            }
            
            if (!client.IsConnected)
            {
                return BadRequest(ApiResponse<NodeDataResponse>.Fail($"站点 {siteCode} 未连接", "SITE_NOT_CONNECTED"));
            }
            
            var dataValue = await client.ReadValueAsync(nodeId);
            
            if (dataValue == null)
            {
                return NotFound(ApiResponse<NodeDataResponse>.Fail("读取节点失败", "READ_FAILED"));
            }
            
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
            _logger.LogError(ex, "读取站点节点失败: {SiteCode}, {NodeId}", siteCode, nodeId);
            return StatusCode(500, ApiResponse<NodeDataResponse>.Fail("读取节点失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 写入指定站点的节点值
    /// </summary>
    [HttpPost("sites/{siteCode}/write")]
    public async Task<ActionResult<ApiResponse<WriteNodeResponse>>> WriteSiteNode(
        string siteCode,
        [FromBody] WriteNodeRequest request)
    {
        try
        {
            var client = _connectionManager.GetClient(siteCode);
            if (client == null)
            {
                return NotFound(ApiResponse<WriteNodeResponse>.Fail($"站点 {siteCode} 不存在或未连接", "SITE_NOT_FOUND"));
            }
            
            if (!client.IsConnected)
            {
                return BadRequest(ApiResponse<WriteNodeResponse>.Fail($"站点 {siteCode} 未连接", "SITE_NOT_CONNECTED"));
            }
            
            // 转换值类型
            var convertedValue = ConvertValueForWrite(request.Value, request.Type);
            if (convertedValue == null && request.Value != null)
            {
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
            _logger.LogError(ex, "写入站点节点失败: {SiteCode}, {NodeId}", siteCode, request.NodeId);
            return StatusCode(500, ApiResponse<WriteNodeResponse>.Fail("写入节点失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 批量读取指定站点的节点值
    /// </summary>
    [HttpPost("sites/{siteCode}/bulk-read")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, NodeDataResponse>>>> BulkReadSiteNodes(
        string siteCode,
        [FromBody] List<string> nodeIds)
    {
        try
        {
            var client = _connectionManager.GetClient(siteCode);
            if (client == null)
            {
                return NotFound(ApiResponse<Dictionary<string, NodeDataResponse>>.Fail($"站点 {siteCode} 不存在或未连接", "SITE_NOT_FOUND"));
            }
            
            if (!client.IsConnected)
            {
                return BadRequest(ApiResponse<Dictionary<string, NodeDataResponse>>.Fail($"站点 {siteCode} 未连接", "SITE_NOT_CONNECTED"));
            }
            
            var results = await client.ReadValuesAsync(nodeIds);
            
            var response = new Dictionary<string, NodeDataResponse>();
            foreach (var (nodeId, dataValue) in results)
            {
                if (dataValue != null)
                {
                    response[nodeId] = new NodeDataResponse
                    {
                        NodeId = nodeId,
                        Value = dataValue.Value,
                        Status = dataValue.StatusCode.ToString(),
                        Timestamp = dataValue.SourceTimestamp.ToLocalTime().ToString("O"),
                        Type = dataValue.Value?.GetType()?.Name
                    };
                }
            }
            
            return Ok(ApiResponse<Dictionary<string, NodeDataResponse>>.Ok(response, $"批量读取成功，共 {response.Count} 个节点"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量读取站点节点失败: {SiteCode}", siteCode);
            return StatusCode(500, ApiResponse<Dictionary<string, NodeDataResponse>>.Fail("批量读取失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 获取所有站点的实时数据
    /// </summary>
    [HttpGet("sites/all/realtime")]
    public ActionResult<ApiResponse<Dictionary<string, Dictionary<string, object?>>>> GetAllSitesRealtimeData()
    {
        try
        {
            var allStatus = _connectionManager.GetAllConnectionStatus();
            var result = new Dictionary<string, Dictionary<string, object?>>();
            
            foreach (var (siteCode, isConnected) in allStatus)
            {
                if (!isConnected)
                {
                    result[siteCode] = new Dictionary<string, object?> { ["_connected"] = false };
                    continue;
                }
                
                var siteData = new Dictionary<string, object?> { ["_connected"] = true };
                
                // 从缓存中获取该站点的数据
                lock (_cache.CacheLock)
                {
                    foreach (var (cacheKey, snapshot) in _cache.NodeCache)
                    {
                        // 检查缓存键是否属于该站点 (格式: siteCode:nodeId)
                        if (cacheKey.StartsWith($"{siteCode}:"))
                        {
                            var nodeId = cacheKey.Substring(siteCode.Length + 1);
                            siteData[nodeId] = snapshot.Value;
                        }
                    }
                }
                
                result[siteCode] = siteData;
            }
            
            return Ok(ApiResponse<Dictionary<string, Dictionary<string, object?>>>.Ok(result, "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有站点实时数据失败");
            return StatusCode(500, ApiResponse<Dictionary<string, Dictionary<string, object?>>>.Fail("获取数据失败", "INTERNAL_ERROR"));
        }
    }
    
    private static object? ConvertValueForWrite(object? value, string? type)
    {
        if (value == null || type == null)
            return value;
        
        try
        {
            var typeLower = type.ToLower();
            
            // 如果值已经是目标类型，直接返回
            if (typeLower == "boolean" || typeLower == "bool")
            {
                if (value is bool boolValue)
                    return boolValue;
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
            Console.WriteLine($"转换失败: Value={value}, Type={type}, ValueType={value?.GetType()}, Error={ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// 写入请求
/// </summary>
public class WriteRequest
{
    public string NodeId { get; set; } = string.Empty;
    public string? Type { get; set; }
    public JsonElement? Value { get; set; }
}

/// <summary>
/// 批量写入请求
/// </summary>
public class BulkWriteRequest
{
    public List<WriteRequest> Writes { get; set; } = new();
    
    /// <summary>
    /// 站点代码（可选），如 site-a, site-b，不传则使用默认站点
    /// </summary>
    public string? SiteCode { get; set; }
}

/// <summary>
/// 批量写入结果
/// </summary>
public class BulkWriteResult
{
    public string NodeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
}

