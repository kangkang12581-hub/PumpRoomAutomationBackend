using PumpRoomAutomationBackend.DTOs.User;

namespace PumpRoomAutomationBackend.DTOs.Auth;

/// <summary>
/// 注册响应
/// Register Response
/// </summary>
public class RegisterResponse
{
    /// <summary>
    /// 注册是否成功
    /// </summary>
    public bool Success { get; set; } = true;
    
    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户信息
    /// </summary>
    public UserDto? User { get; set; }
}

