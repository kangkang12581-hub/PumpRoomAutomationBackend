using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.DTOs.User;
using PumpRoomAutomationBackend.Models.Enums;
using PumpRoomAutomationBackend.Services;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// 用户管理控制器
/// User Management Controller
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    
    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }
    
    /// <summary>
    /// 创建用户
    /// Create User
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "ROOT,ADMIN")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser([FromBody] UserCreateDto createDto)
    {
        try
        {
            // 角色创建限制：
            // - ROOT 可以创建 ADMIN/OPERATOR/OBSERVER
            // - ADMIN 只能创建 OPERATOR/OBSERVER，不能创建 ADMIN/ROOT
            var isRoot = User.IsInRole("ROOT");
            var isAdmin = User.IsInRole("ADMIN");

            if (!isRoot && !isAdmin)
            {
                return Forbid();
            }

            var targetGroup = createDto.UserGroup;
            if (isAdmin)
            {
                if (targetGroup == UserGroup.ADMIN || targetGroup == UserGroup.ROOT)
                {
                    return StatusCode(403, ApiResponse<UserDto>.Fail("管理员无权创建管理员/超级管理员", "FORBIDDEN"));
                }
            }
            // isRoot 无限制

            var user = await _userService.CreateUserAsync(createDto);
            return Ok(ApiResponse<UserDto>.Ok(user, "创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<UserDto>.Fail(ex.Message, "INVALID_INPUT"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户时发生错误: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<UserDto>.Fail("创建用户失败", "INTERNAL_ERROR"));
        }
    }

        /// <summary>
        /// 获取指定站点的用户列表
        /// Get Users By Site ID
        /// </summary>
        [HttpGet("site/{siteId}")]
        public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsersBySite(int siteId)
        {
            try
            {
                var users = await _userService.GetUsersBySiteIdAsync(siteId);
                return Ok(ApiResponse<List<UserDto>>.Ok(users, $"获取站点 {siteId} 用户成功，共 {users.Count} 人"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取站点用户时发生错误: {Message}", ex.Message);
                return StatusCode(500, ApiResponse<List<UserDto>>.Fail("获取站点用户失败", "INTERNAL_ERROR"));
            }
        }

    /// <summary>
    /// 获取用户列表
    /// Get User List
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserDto>>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? search = null,
        [FromQuery] UserGroup? userGroup = null,
        [FromQuery] UserStatus? status = null)
    {
        try
        {
            // 记录请求参数，确认status是否为null（null表示返回所有用户）
            _logger.LogInformation("📥 获取用户列表请求: page={Page}, size={Size}, search={Search}, userGroup={UserGroup}, status={Status} (null表示返回所有用户)", 
                page, size, search ?? "null", userGroup?.ToString() ?? "null", status?.ToString() ?? "null");
            
            var users = await _userService.GetUsersAsync(page, size, search, userGroup, status);
            // 根据过滤条件计算正确的总数（包括非活跃用户）
            var total = await _userService.GetTotalUsersAsync(search, userGroup, status);
            
            _logger.LogInformation("✅ 返回用户列表: 总数={Total}, 当前页数量={Count}, status过滤={Status}", 
                total, users.Count, status?.ToString() ?? "无（返回所有用户）");
            
            var response = new PagedResponse<UserDto>
            {
                Items = users,
                Total = total,
                Page = page,
                Size = size,
                Pages = (int)Math.Ceiling(total / (double)size)
            };
            
            return Ok(ApiResponse<PagedResponse<UserDto>>.Ok(response, "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表时发生错误: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<PagedResponse<UserDto>>.Fail("获取用户列表失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 根据ID获取用户
    /// Get User By ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.Fail("用户不存在", "USER_NOT_FOUND"));
            }
            
            return Ok(ApiResponse<UserDto>.Ok(user, "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户时发生错误: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<UserDto>.Fail("获取用户失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 更新用户
    /// Update User
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(int id, [FromBody] UserUpdateDto updateDto)
    {
        try
        {
            var user = await _userService.UpdateUserAsync(id, updateDto);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.Fail("用户不存在", "USER_NOT_FOUND"));
            }
            
            _logger.LogInformation("用户 {UserId} 已更新", id);
            
            return Ok(ApiResponse<UserDto>.Ok(user, "更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户时发生错误: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<UserDto>.Fail("更新用户失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 删除用户
    /// Delete User
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(int id)
    {
        try
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result)
            {
                return NotFound(ApiResponse<bool>.Fail("用户不存在", "USER_NOT_FOUND"));
            }
            
            _logger.LogInformation("用户 {UserId} 已删除", id);
            
            return Ok(ApiResponse<bool>.Ok(true, "删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除用户时发生错误: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<bool>.Fail("删除用户失败", "INTERNAL_ERROR"));
        }
    }
}

