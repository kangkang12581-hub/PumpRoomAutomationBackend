using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.DTOs.Site;
using PumpRoomAutomationBackend.Models.Entities;
using PumpRoomAutomationBackend.Services;
using PumpRoomAutomationBackend.Services.OpcUa;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// 站点管理控制器
/// Site Management Controller
/// </summary>
[ApiController]
[Route("api/sites")]
[Authorize]
public class SiteController : ControllerBase
{
    private readonly ISiteService _siteService;
    private readonly IOpcUaConnectionManager _connectionManager;
    private readonly ILogger<SiteController> _logger;
    
    public SiteController(
        ISiteService siteService,
        IOpcUaConnectionManager connectionManager,
        ILogger<SiteController> logger)
    {
        _siteService = siteService;
        _connectionManager = connectionManager;
        _logger = logger;
    }
    
    /// <summary>
    /// 获取所有站点
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SiteDto>>>> GetAllSites()
    {
        try
        {
            var sites = await _siteService.GetAllSitesAsync();
            var siteDtos = sites.Select(MapToDto).ToList();
            
            return Ok(ApiResponse<List<SiteDto>>.Ok(siteDtos, "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取站点列表失败");
            return StatusCode(500, ApiResponse<List<SiteDto>>.Fail("获取站点列表失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 获取已启用的站点
    /// </summary>
    [HttpGet("enabled")]
    public async Task<ActionResult<ApiResponse<List<SiteDto>>>> GetEnabledSites()
    {
        try
        {
            var sites = await _siteService.GetEnabledSitesAsync();
            var siteDtos = sites.Select(MapToDto).ToList();
            
            return Ok(ApiResponse<List<SiteDto>>.Ok(siteDtos, "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取启用站点列表失败");
            return StatusCode(500, ApiResponse<List<SiteDto>>.Fail("获取站点列表失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 根据站点编码获取站点
    /// </summary>
    [HttpGet("{siteCode}")]
    public async Task<ActionResult<ApiResponse<SiteDto>>> GetSite(string siteCode)
    {
        try
        {
            var site = await _siteService.GetSiteBySiteCodeAsync(siteCode);
            
            if (site == null)
            {
                return NotFound(ApiResponse<SiteDto>.Fail("站点不存在", "SITE_NOT_FOUND"));
            }
            
            var siteDto = MapToDto(site);
            return Ok(ApiResponse<SiteDto>.Ok(siteDto, "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取站点失败: {SiteCode}", siteCode);
            return StatusCode(500, ApiResponse<SiteDto>.Fail("获取站点失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 创建站点
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SiteDto>>> CreateSite([FromBody] CreateSiteRequest request)
    {
        try
        {
            // 获取当前用户ID
            var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(ApiResponse<SiteDto>.Fail("未授权", "UNAUTHORIZED"));
            }
            
            var site = new SiteConfig
            {
                UserId = userId,
                SiteCode = request.SiteCode,
                SiteName = request.SiteName,
                SiteLocation = request.SiteLocation,
                SiteDescription = request.SiteDescription,
                IpAddress = request.IpAddress,
                Port = request.Port,
                Protocol = request.Protocol,
                InternalCameraIp = request.InternalCameraIp,
                InternalCameraUsername = request.InternalCameraUsername,
                InternalCameraPassword = request.InternalCameraPassword,
                GlobalCameraIp = request.GlobalCameraIp,
                GlobalCameraUsername = request.GlobalCameraUsername,
                GlobalCameraPassword = request.GlobalCameraPassword,
                OpcuaEndpoint = request.OpcuaEndpoint,
                OpcuaSecurityPolicy = request.OpcuaSecurityPolicy,
                OpcuaSecurityMode = request.OpcuaSecurityMode,
                OpcuaAnonymous = request.OpcuaAnonymous,
                OpcuaUsername = request.OpcuaUsername,
                OpcuaPassword = request.OpcuaPassword,
                OpcuaSessionTimeout = request.OpcuaSessionTimeout,
                OpcuaRequestTimeout = request.OpcuaRequestTimeout,
                ContactPerson = request.ContactPerson,
                ContactPhone = request.ContactPhone,
                ContactEmail = request.ContactEmail,
                OperatingPressureMin = request.OperatingPressureMin,
                OperatingPressureMax = request.OperatingPressureMax,
                PumpCount = request.PumpCount,
                AlarmEnabled = request.AlarmEnabled,
                AlarmPhoneNumbers = request.AlarmPhoneNumbers,
                AlarmEmailAddresses = request.AlarmEmailAddresses,
                IsEnabled = true,
                IsActive = true
            };
            
            var createdSite = await _siteService.CreateSiteAsync(site);
            
            // 如果站点已启用，立即连接
            if (createdSite.IsEnabled)
            {
                _ = Task.Run(async () => await _connectionManager.ConnectSiteAsync(createdSite.SiteCode));
            }
            
            var siteDto = MapToDto(createdSite);
            return CreatedAtAction(nameof(GetSite), new { siteCode = createdSite.SiteCode }, 
                ApiResponse<SiteDto>.Ok(siteDto, "站点创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "创建站点失败: {Message}", ex.Message);
            return BadRequest(ApiResponse<SiteDto>.Fail(ex.Message, "INVALID_OPERATION"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建站点失败");
            return StatusCode(500, ApiResponse<SiteDto>.Fail("创建站点失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 更新站点
    /// </summary>
    [HttpPut("{siteCode}")]
    public async Task<ActionResult<ApiResponse<SiteDto>>> UpdateSite(string siteCode, [FromBody] UpdateSiteRequest request)
    {
        try
        {
            var existingSite = await _siteService.GetSiteBySiteCodeAsync(siteCode);
            
            if (existingSite == null)
            {
                return NotFound(ApiResponse<SiteDto>.Fail("站点不存在", "SITE_NOT_FOUND"));
            }
            
            // 更新字段
            existingSite.SiteName = request.SiteName;
            existingSite.SiteLocation = request.SiteLocation;
            existingSite.SiteDescription = request.SiteDescription;
            existingSite.IpAddress = request.IpAddress;
            existingSite.Port = request.Port;
            existingSite.Protocol = request.Protocol;
            existingSite.InternalCameraIp = request.InternalCameraIp;
            existingSite.InternalCameraUsername = request.InternalCameraUsername;
            existingSite.InternalCameraPassword = request.InternalCameraPassword;
            existingSite.GlobalCameraIp = request.GlobalCameraIp;
            existingSite.GlobalCameraUsername = request.GlobalCameraUsername;
            existingSite.GlobalCameraPassword = request.GlobalCameraPassword;
            existingSite.OpcuaEndpoint = request.OpcuaEndpoint;
            existingSite.OpcuaSecurityPolicy = request.OpcuaSecurityPolicy;
            existingSite.OpcuaSecurityMode = request.OpcuaSecurityMode;
            existingSite.OpcuaAnonymous = request.OpcuaAnonymous;
            existingSite.OpcuaUsername = request.OpcuaUsername;
            existingSite.OpcuaPassword = request.OpcuaPassword;
            existingSite.OpcuaSessionTimeout = request.OpcuaSessionTimeout;
            existingSite.OpcuaRequestTimeout = request.OpcuaRequestTimeout;
            existingSite.ContactPerson = request.ContactPerson;
            existingSite.ContactPhone = request.ContactPhone;
            existingSite.ContactEmail = request.ContactEmail;
            existingSite.OperatingPressureMin = request.OperatingPressureMin;
            existingSite.OperatingPressureMax = request.OperatingPressureMax;
            existingSite.PumpCount = request.PumpCount;
            // 只有在明确提供 IsEnabled 时才更新，避免意外重置
            if (request.IsEnabled.HasValue)
            {
                existingSite.IsEnabled = request.IsEnabled.Value;
            }
            // 注意：IsOnline 不应该通过更新接口修改，它由连接管理器自动管理
            existingSite.AlarmEnabled = request.AlarmEnabled;
            existingSite.AlarmPhoneNumbers = request.AlarmPhoneNumbers;
            existingSite.AlarmEmailAddresses = request.AlarmEmailAddresses;
            
            var updatedSite = await _siteService.UpdateSiteAsync(existingSite);
            
            // 如果配置有变化，重新连接
            _ = Task.Run(async () =>
            {
                await _connectionManager.DisconnectSiteAsync(siteCode);
                if (updatedSite.IsEnabled)
                {
                    await _connectionManager.ConnectSiteAsync(siteCode);
                }
            });
            
            var siteDto = MapToDto(updatedSite);
            return Ok(ApiResponse<SiteDto>.Ok(siteDto, "站点更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新站点失败: {SiteCode}", siteCode);
            return StatusCode(500, ApiResponse<SiteDto>.Fail("更新站点失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 删除站点
    /// </summary>
    [HttpDelete("{siteCode}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSite(string siteCode)
    {
        try
        {
            var site = await _siteService.GetSiteBySiteCodeAsync(siteCode);
            
            if (site == null)
            {
                return NotFound(ApiResponse<bool>.Fail("站点不存在", "SITE_NOT_FOUND"));
            }
            
            // 先断开连接
            await _connectionManager.DisconnectSiteAsync(siteCode);
            
            // 删除站点
            var deleted = await _siteService.DeleteSiteAsync(site.Id);
            
            if (deleted)
            {
                return Ok(ApiResponse<bool>.Ok(true, "站点删除成功"));
            }
            
            return StatusCode(500, ApiResponse<bool>.Fail("删除站点失败", "DELETE_FAILED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除站点失败: {SiteCode}", siteCode);
            return StatusCode(500, ApiResponse<bool>.Fail("删除站点失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 启用/禁用站点
    /// </summary>
    [HttpPost("{siteCode}/toggle")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleSite(string siteCode, [FromBody] bool enabled)
    {
        try
        {
            var success = await _siteService.ToggleSiteAsync(siteCode, enabled);
            
            if (!success)
            {
                return NotFound(ApiResponse<bool>.Fail("站点不存在", "SITE_NOT_FOUND"));
            }
            
            // 根据状态连接或断开
            if (enabled)
            {
                await _connectionManager.ConnectSiteAsync(siteCode);
            }
            else
            {
                await _connectionManager.DisconnectSiteAsync(siteCode);
            }
            
            var message = enabled ? "站点已启用" : "站点已禁用";
            return Ok(ApiResponse<bool>.Ok(true, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换站点状态失败: {SiteCode}", siteCode);
            return StatusCode(500, ApiResponse<bool>.Fail("操作失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 测试站点连接
    /// </summary>
    [HttpPost("{siteCode}/test-connection")]
    public async Task<ActionResult<ApiResponse<SiteConnectionStatusDto>>> TestConnection(string siteCode)
    {
        try
        {
            _logger.LogInformation("测试站点连接: {SiteCode}", siteCode);
            
            // 尝试连接
            var connected = await _connectionManager.ConnectSiteAsync(siteCode);
            
            var client = _connectionManager.GetClient(siteCode);
            
            var status = new SiteConnectionStatusDto
            {
                SiteCode = siteCode,
                SiteName = client?.SiteName ?? siteCode,
                IsConnected = connected,
                ConnectionStatus = connected ? "connected" : "failed",
                LastConnectTime = client?.LastConnectTime,
                LastDisconnectTime = client?.LastDisconnectTime,
                Endpoint = client?.Endpoint ?? string.Empty
            };
            
            var message = connected ? "连接测试成功" : "连接测试失败";
            return Ok(ApiResponse<SiteConnectionStatusDto>.Ok(status, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试连接失败: {SiteCode}", siteCode);
            return StatusCode(500, ApiResponse<SiteConnectionStatusDto>.Fail("测试连接失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 获取所有站点的连接状态
    /// </summary>
    [HttpGet("connection-status")]
    public ActionResult<ApiResponse<List<SiteConnectionStatusDto>>> GetAllConnectionStatus()
    {
        try
        {
            var statusDict = _connectionManager.GetAllConnectionStatus();
            var statusList = statusDict.Select(kvp =>
            {
                var client = _connectionManager.GetClient(kvp.Key);
                return new SiteConnectionStatusDto
                {
                    SiteCode = kvp.Key,
                    SiteName = client?.SiteName ?? kvp.Key,
                    IsConnected = kvp.Value,
                    ConnectionStatus = kvp.Value ? "connected" : "disconnected",
                    LastConnectTime = client?.LastConnectTime,
                    LastDisconnectTime = client?.LastDisconnectTime,
                    Endpoint = client?.Endpoint ?? string.Empty
                };
            }).ToList();
            
            return Ok(ApiResponse<List<SiteConnectionStatusDto>>.Ok(statusList, "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取连接状态失败");
            return StatusCode(500, ApiResponse<List<SiteConnectionStatusDto>>.Fail("获取连接状态失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 重新加载所有站点配置
    /// </summary>
    [HttpPost("reload")]
    public async Task<ActionResult<ApiResponse<bool>>> ReloadConfigurations()
    {
        try
        {
            await _connectionManager.ReloadSiteConfigurationsAsync();
            return Ok(ApiResponse<bool>.Ok(true, "配置重新加载成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重新加载配置失败");
            return StatusCode(500, ApiResponse<bool>.Fail("重新加载配置失败", "INTERNAL_ERROR"));
        }
    }
    
    private static SiteDto MapToDto(SiteConfig site)
    {
        return new SiteDto
        {
            Id = site.Id,
            SiteCode = site.SiteCode,
            SiteName = site.SiteName,
            SiteLocation = site.SiteLocation,
            SiteDescription = site.SiteDescription,
            IpAddress = site.IpAddress,
            Port = site.Port,
            Protocol = site.Protocol,
            InternalCameraIp = site.InternalCameraIp,
            InternalCameraUsername = site.InternalCameraUsername,
            InternalCameraPassword = site.InternalCameraPassword,
            GlobalCameraIp = site.GlobalCameraIp,
            GlobalCameraUsername = site.GlobalCameraUsername,
            GlobalCameraPassword = site.GlobalCameraPassword,
            OpcuaEndpoint = site.OpcuaEndpoint,
            OpcuaSecurityPolicy = site.OpcuaSecurityPolicy,
            OpcuaSecurityMode = site.OpcuaSecurityMode,
            OpcuaAnonymous = site.OpcuaAnonymous,
            OpcuaUsername = site.OpcuaUsername,
            OpcuaSessionTimeout = site.OpcuaSessionTimeout,
            OpcuaRequestTimeout = site.OpcuaRequestTimeout,
            ContactPerson = site.ContactPerson,
            ContactPhone = site.ContactPhone,
            ContactEmail = site.ContactEmail,
            OperatingPressureMin = site.OperatingPressureMin,
            OperatingPressureMax = site.OperatingPressureMax,
            PumpCount = site.PumpCount,
            IsEnabled = site.IsEnabled,
            IsOnline = site.IsOnline,
            ConnectionStatus = site.ConnectionStatus,
            LastHeartbeat = site.LastHeartbeat,
            IsDefault = site.IsDefault,
            AlarmEnabled = site.AlarmEnabled,
            AlarmPhoneNumbers = site.AlarmPhoneNumbers,
            AlarmEmailAddresses = site.AlarmEmailAddresses,
            CreatedAt = site.CreatedAt,
            UpdatedAt = site.UpdatedAt
        };
    }
}

