using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// 站点分配控制器
/// Site Assignment Controller
/// </summary>
[ApiController]
[Route("api/site-assignments")]
[Authorize]
public class SiteAssignmentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SiteAssignmentController> _logger;

    public SiteAssignmentController(
        ApplicationDbContext context,
        ILogger<SiteAssignmentController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户的站点
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<object>>> GetMySites()
    {
        try
        {
            var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(ApiResponse<object>.Fail("未授权", "UNAUTHORIZED"));
            }

            var userSites = await _context.UserSites
                .Where(us => us.UserId == userId && us.SiteConfig != null)
                .Include(us => us.SiteConfig)
                .Select(us => new
                {
                    id = us.SiteConfig!.Id,
                    siteCode = us.SiteConfig.SiteCode,
                    siteName = us.SiteConfig.SiteName,
                    siteLocation = us.SiteConfig.SiteLocation,
                    ipAddress = us.SiteConfig.IpAddress,
                    port = us.SiteConfig.Port,
                    isEnabled = us.SiteConfig.IsEnabled,
                    isOnline = us.SiteConfig.IsOnline,
                    role = us.Role,
                    isOwner = us.IsOwner
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new { sites = userSites }, "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户站点失败");
            return StatusCode(500, ApiResponse<object>.Fail("获取站点失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 获取指定用户的站点
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<ApiResponse<object>>> GetSitesByUser(int userId)
    {
        try
        {
            var userSites = await _context.UserSites
                .Where(us => us.UserId == userId && us.SiteConfig != null)
                .Include(us => us.SiteConfig)
                .Select(us => new
                {
                    id = us.SiteConfig!.Id,
                    siteCode = us.SiteConfig.SiteCode,
                    siteName = us.SiteConfig.SiteName,
                    siteLocation = us.SiteConfig.SiteLocation,
                    ipAddress = us.SiteConfig.IpAddress,
                    port = us.SiteConfig.Port,
                    isEnabled = us.SiteConfig.IsEnabled,
                    isOnline = us.SiteConfig.IsOnline,
                    role = us.Role,
                    isOwner = us.IsOwner
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new { sites = userSites }, "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户站点失败: UserId={UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Fail("获取站点失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 分配站点给用户
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> AssignSites([FromBody] AssignSitesRequest request)
    {
        try
        {
            if (request.SiteIds == null || request.SiteIds.Count == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("站点列表不能为空", "INVALID_INPUT"));
            }

            // 检查用户是否存在
            var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
            if (!userExists)
            {
                return NotFound(ApiResponse<object>.Fail("用户不存在", "USER_NOT_FOUND"));
            }

            // 检查站点是否存在
            var existingSiteIds = await _context.SiteConfigs
                .Where(s => request.SiteIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            if (existingSiteIds.Count != request.SiteIds.Count)
            {
                return BadRequest(ApiResponse<object>.Fail("部分站点不存在", "INVALID_SITES"));
            }

            // 获取已经分配的站点
            var existingAssignments = await _context.UserSites
                .Where(us => us.UserId == request.UserId && request.SiteIds.Contains(us.SiteId))
                .Select(us => us.SiteId)
                .ToListAsync();

            // 只添加尚未分配的站点
            var newSiteIds = request.SiteIds.Except(existingAssignments).ToList();

            if (newSiteIds.Count > 0)
            {
                var userSites = newSiteIds.Select(siteId => new UserSite
                {
                    UserId = request.UserId,
                    SiteId = siteId,
                    Role = request.Role,
                    IsOwner = false,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                _context.UserSites.AddRange(userSites);
                await _context.SaveChangesAsync();

                _logger.LogInformation("站点分配成功: UserId={UserId}, SiteCount={Count}", request.UserId, newSiteIds.Count);
            }

            return Ok(ApiResponse<object>.Ok(new { assigned = newSiteIds.Count, skipped = existingAssignments.Count }, "分配成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分配站点失败: UserId={UserId}", request.UserId);
            return StatusCode(500, ApiResponse<object>.Fail("分配站点失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 取消分配站点
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult<ApiResponse<object>>> UnassignSites([FromBody] UnassignSitesRequest request)
    {
        try
        {
            if (request.SiteIds == null || request.SiteIds.Count == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("站点列表不能为空", "INVALID_INPUT"));
            }

            var userSites = await _context.UserSites
                .Where(us => us.UserId == request.UserId && request.SiteIds.Contains(us.SiteId))
                .ToListAsync();

            if (userSites.Count > 0)
            {
                _context.UserSites.RemoveRange(userSites);
                await _context.SaveChangesAsync();

                _logger.LogInformation("取消站点分配成功: UserId={UserId}, SiteCount={Count}", request.UserId, userSites.Count);
            }

            return Ok(ApiResponse<object>.Ok(new { removed = userSites.Count }, "取消分配成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消分配站点失败: UserId={UserId}", request.UserId);
            return StatusCode(500, ApiResponse<object>.Fail("取消分配站点失败", "INTERNAL_ERROR"));
        }
    }
}

/// <summary>
/// 分配站点请求
/// </summary>
public class AssignSitesRequest
{
    public int UserId { get; set; }
    public List<int> SiteIds { get; set; } = new();
    public string? Role { get; set; }
}

/// <summary>
/// 取消分配站点请求
/// </summary>
public class UnassignSitesRequest
{
    public int UserId { get; set; }
    public List<int> SiteIds { get; set; } = new();
}

