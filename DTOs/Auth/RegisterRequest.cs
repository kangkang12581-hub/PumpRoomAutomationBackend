using System.ComponentModel.DataAnnotations;

namespace PumpRoomAutomationBackend.DTOs.Auth;

/// <summary>
/// 注册请求
/// Register Request
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在3到50个字符之间")]
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 显示名称
    /// </summary>
    [Required(ErrorMessage = "显示名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "显示名称长度必须在1到100个字符之间")]
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6到100个字符之间")]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// 确认密码
    /// </summary>
    [Required(ErrorMessage = "确认密码不能为空")]
    [Compare("Password", ErrorMessage = "密码和确认密码不一致")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    /// <summary>
    /// 邮箱
    /// </summary>
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }
    
    /// <summary>
    /// 电话
    /// </summary>
    [StringLength(20, ErrorMessage = "电话号码最多20个字符")]
    public string? Phone { get; set; }
    
    /// <summary>
    /// 全名（已废弃，使用display_name）
    /// </summary>
    public string? FullName { get; set; }
}

