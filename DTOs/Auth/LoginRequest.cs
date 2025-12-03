using System.ComponentModel.DataAnnotations;

namespace PumpRoomAutomationBackend.DTOs.Auth;

/// <summary>
/// 登录请求
/// Login Request
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;
}

