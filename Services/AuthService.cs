using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PumpRoomAutomationBackend.Configuration;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.Models.Entities;
using PumpRoomAutomationBackend.Models.Enums;
using PumpRoomAutomationBackend.Services.Security;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 认证服务
/// Authentication Service
/// </summary>
public interface IAuthService
{
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> AuthenticateUserAsync(string username, string password);
    Task<User> CreateUserAsync(string username, string password, string displayName, string? email = null, string? phone = null, bool isAdmin = false);
    Task UpdateLastLoginAsync(int userId);
    Task LogLoginAttemptAsync(string username, bool success, int? userId = null, string? ipAddress = null, string? userAgent = null, string? errorMessage = null);
    string GenerateToken(User user);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;
    
    public AuthService(
        ApplicationDbContext context,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }
    
    /// <summary>
    /// 根据用户名获取用户
    /// Get user by username
    /// </summary>
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }
    
    /// <summary>
    /// 验证用户
    /// Authenticate user
    /// </summary>
    public async Task<User?> AuthenticateUserAsync(string username, string password)
    {
        _logger.LogInformation("🔐 开始验证用户登录: {Username}", username);
        
        // 第一步：查找用户
        var user = await GetUserByUsernameAsync(username);
        
        if (user == null)
        {
            _logger.LogWarning("❌ 登录失败 - 用户不存在: {Username}", username);
            return null;
        }
        
        _logger.LogInformation("✅ 用户找到: {Username} (ID: {UserId})", username, user.Id);
        _logger.LogDebug("   用户状态: IsActive={IsActive}, Status={Status}, IsAdmin={IsAdmin}", 
            user.IsActive, user.Status, user.IsAdmin);
        
        // 第二步：验证密码
        _logger.LogDebug("🔑 验证密码中...");
        _logger.LogDebug("   输入密码长度: {PasswordLength}", password.Length);
        
        if (string.IsNullOrEmpty(user.HashedPassword))
        {
            _logger.LogWarning("❌ 登录失败 - 用户密码哈希为空: {Username}", username);
            return null;
        }
        
        var hashPrefix = user.HashedPassword.Length > 20 ? user.HashedPassword.Substring(0, 20) : user.HashedPassword;
        _logger.LogDebug("   存储哈希: {HashPrefix}...", hashPrefix);
        
        var passwordValid = _passwordService.VerifyPassword(password, user.HashedPassword);
        
        if (!passwordValid)
        {
            _logger.LogWarning("❌ 登录失败 - 密码错误: {Username}", username);
            _logger.LogDebug("   密码验证失败详情:");
            _logger.LogDebug("   - 输入密码: {Password}", password);
            _logger.LogDebug("   - 存储哈希: {Hash}", user.HashedPassword);
            return null;
        }
        
        _logger.LogInformation("✅ 密码验证通过: {Username}", username);
        
        // 第三步：检查账户状态
        if (!user.IsActive)
        {
            _logger.LogWarning("❌ 登录失败 - 账户未激活: {Username} (IsActive={IsActive})", username, user.IsActive);
            return null;
        }
        
        if (user.Status != UserStatus.ACTIVE)
        {
            _logger.LogWarning("❌ 登录失败 - 账户状态异常: {Username} (Status={Status})", username, user.Status);
            return null;
        }
        
        _logger.LogInformation("✅ 账户状态正常: {Username}", username);
        _logger.LogInformation("🎉 用户登录验证成功: {Username} (ID: {UserId})", username, user.Id);
        
        return user;
    }
    
    /// <summary>
    /// 创建用户
    /// Create user
    /// </summary>
    public async Task<User> CreateUserAsync(
        string username, 
        string password, 
        string displayName,
        string? email = null, 
        string? phone = null, 
        bool isAdmin = false)
    {
        // 检查用户名是否已存在
        var existingUser = await GetUserByUsernameAsync(username);
        if (existingUser != null)
        {
            throw new InvalidOperationException("用户名已存在");
        }
        
        // 创建新用户
        var user = new User
        {
            Username = username,
            DisplayName = displayName,
            HashedPassword = _passwordService.HashPassword(password),
            Email = email,
            Phone = phone,
            UserGroup = isAdmin ? UserGroup.ADMIN : UserGroup.OPERATOR,
            UserLevel = UserLevel.LEVEL_3,
            Status = UserStatus.ACTIVE,
            IsActive = true,
            IsAdmin = isAdmin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return user;
    }
    
    /// <summary>
    /// 更新用户最后登录时间
    /// Update user last login time
    /// </summary>
    public async Task UpdateLastLoginAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// 记录登录尝试
    /// Log login attempt
    /// </summary>
    public async Task LogLoginAttemptAsync(
        string username, 
        bool success, 
        int? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? errorMessage = null)
    {
        var loginLog = new LoginLog
        {
            UserId = userId,
            Username = username,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Success = success,
            ErrorMessage = errorMessage,
            LoginTime = DateTime.UtcNow
        };
        
        _context.LoginLogs.Add(loginLog);
        await _context.SaveChangesAsync();
    }
    
    /// <summary>
    /// 生成令牌
    /// Generate token
    /// </summary>
    public string GenerateToken(User user)
    {
        return _jwtTokenService.GenerateAccessToken(
            user.Username, 
            user.Id, 
            user.UserGroup.ToString()
        );
    }
}

