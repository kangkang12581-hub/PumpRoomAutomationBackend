using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PumpRoomAutomationBackend.Configuration;
using PumpRoomAutomationBackend.DTOs.Auth;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.DTOs.User;
using PumpRoomAutomationBackend.Services;

namespace PumpRoomAutomationBackend.Controllers;

/// <summary>
/// 认证控制器
/// Authentication Controller
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(
        IAuthService authService,
        IUserService userService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _userService = userService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }
    
    /// <summary>
    /// 用户登录
    /// User Login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("========================================");
            _logger.LogInformation("📥 收到登录请求");
            _logger.LogInformation("   用户名: {Username}", request.Username);
            _logger.LogInformation("   密码长度: {PasswordLength}", request.Password?.Length ?? 0);
            _logger.LogInformation("   IP地址: {IpAddress}", GetClientIpAddress());
            _logger.LogInformation("   User-Agent: {UserAgent}", Request.Headers.UserAgent.ToString());
            _logger.LogInformation("========================================");
            
            // 验证输入
            if (string.IsNullOrEmpty(request.Password))
            {
                _logger.LogWarning("❌ 登录失败 - 密码为空");
                return BadRequest(ApiResponse<LoginResponse>.Fail("密码不能为空", "PASSWORD_EMPTY"));
            }
            
            // 验证用户
            var user = await _authService.AuthenticateUserAsync(request.Username, request.Password);
            
            if (user == null)
            {
                _logger.LogWarning("⚠️  登录失败 - 认证返回null");
                
                // 记录失败的登录尝试
                await _authService.LogLoginAttemptAsync(
                    request.Username,
                    false,
                    ipAddress: GetClientIpAddress(),
                    userAgent: Request.Headers.UserAgent.ToString(),
                    errorMessage: "用户名或密码错误"
                );
                
                return Unauthorized(ApiResponse<LoginResponse>.Fail("用户名或密码错误", "AUTH_FAILED"));
            }
            
            // 生成 Token
            var token = _authService.GenerateToken(user);
            
            // 更新最后登录时间
            await _authService.UpdateLastLoginAsync(user.Id);
            
            // 记录成功的登录
            await _authService.LogLoginAttemptAsync(
                request.Username,
                true,
                userId: user.Id,
                ipAddress: GetClientIpAddress(),
                userAgent: Request.Headers.UserAgent.ToString()
            );
            
            // 获取用户完整信息
            var userDto = await _userService.GetUserByIdAsync(user.Id);
            
            var response = new LoginResponse
            {
                AccessToken = token,
                TokenType = "bearer",
                ExpiresIn = _jwtSettings.AccessTokenExpirationMinutes * 60,
                User = userDto!
            };
            
            _logger.LogInformation("用户 {Username} 登录成功", request.Username);
            
            return Ok(ApiResponse<LoginResponse>.Ok(response, "登录成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录时发生错误: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<LoginResponse>.Fail("登录失败，请稍后重试", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 用户注册
    /// User Registration
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // 检查用户名是否已存在
            var existingUser = await _authService.GetUserByUsernameAsync(request.Username);
            if (existingUser != null)
            {
                return BadRequest(ApiResponse<RegisterResponse>.Fail("用户名已存在", "USERNAME_EXISTS"));
            }
            
            // 创建用户
            var user = await _authService.CreateUserAsync(
                request.Username,
                request.Password,
                request.DisplayName,
                request.Email,
                request.Phone,
                isAdmin: false
            );
            
            // 获取用户DTO
            var userDto = await _userService.GetUserByIdAsync(user.Id);
            
            var response = new RegisterResponse
            {
                Success = true,
                Message = "注册成功",
                User = userDto
            };
            
            _logger.LogInformation("新用户注册成功: {Username}", request.Username);
            
            return Ok(ApiResponse<RegisterResponse>.Ok(response, "注册成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册时发生错误: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<RegisterResponse>.Fail("注册失败，请稍后重试", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 获取当前用户信息
    /// Get Current User Info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        try
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(ApiResponse<UserDto>.Fail("未授权", "UNAUTHORIZED"));
            }
            
            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.Fail("用户不存在", "USER_NOT_FOUND"));
            }
            
            return Ok(ApiResponse<UserDto>.Ok(user, "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取当前用户信息时发生错误: {Message}", ex.Message);
            return StatusCode(500, ApiResponse<UserDto>.Fail("获取用户信息失败", "INTERNAL_ERROR"));
        }
    }
    
    /// <summary>
    /// 分页获取用户列表（需要管理员权限）
    /// Get Paginated Users List (Admin only)
    /// </summary>
    [HttpGet("users/paginated")]
    [Authorize]
    public async Task<ActionResult<object>> GetPaginatedUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int size = 20,
        [FromQuery] string? search = null)
    {
        try
        {
            _logger.LogInformation("📥 收到分页获取用户列表请求: page={Page}, size={Size}, search={Search}", 
                page, size, search);
            
            // 检查是否是管理员
            var currentUsername = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUsername))
            {
                return Unauthorized(new { success = false, message = "未授权" });
            }
            
            var currentUser = await _userService.GetUserByUsernameAsync(currentUsername);
            if (currentUser == null || !currentUser.IsAdmin)
            {
                return Forbid();
            }
            
            // 获取所有用户
            var allUsers = await _userService.GetAllUsersAsync();
            
            // 如果有搜索关键词，进行过滤
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                allUsers = allUsers.Where((UserDto u) => 
                    u.Username.ToLower().Contains(searchLower) ||
                    (u.DisplayName?.ToLower().Contains(searchLower) ?? false) ||
                    (u.Email?.ToLower().Contains(searchLower) ?? false)
                ).ToList();
            }
            
            var total = allUsers.Count;
            var totalPages = (int)Math.Ceiling(total / (double)size);
            
            // 分页
            var users = allUsers
                .Skip((page - 1) * size)
                .Take(size)
                .ToList();
            
            var result = new
            {
                success = true,
                data = new
                {
                    users = users,
                    total = total,
                    page = page,
                    size = size,
                    totalPages = totalPages
                }
            };
            
            _logger.LogInformation("✅ 返回 {Count} 个用户，总计 {Total} 个", users.Count, total);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            return StatusCode(500, new { success = false, message = "获取用户列表失败" });
        }
    }
    
    private string GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }
        
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

