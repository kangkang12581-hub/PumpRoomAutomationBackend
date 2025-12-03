using PumpRoomAutomationBackend.DTOs.User;

namespace PumpRoomAutomationBackend.DTOs.Auth;

/// <summary>
/// 登录响应
/// Login Response
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// 令牌类型
    /// </summary>
    public string TokenType { get; set; } = "bearer";
    
    /// <summary>
    /// 过期时间（秒）
    /// </summary>
    public int ExpiresIn { get; set; }
    
    /// <summary>
    /// 用户信息
    /// </summary>
    public UserDto User { get; set; } = null!;
}

