using System.ComponentModel.DataAnnotations;
using PumpRoomAutomationBackend.Models.Enums;

namespace PumpRoomAutomationBackend.DTOs.User;

/// <summary>
/// 创建用户数据传输对象
/// Create User Data Transfer Object
/// </summary>
public class UserCreateDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在3到50个字符之间")]
    public string Username { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "显示名称不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "显示名称长度必须在1到100个字符之间")]
    public string DisplayName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6到100个字符之间")]
    public string Password { get; set; } = string.Empty;
    
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }
    
    [StringLength(20, ErrorMessage = "电话号码最多20个字符")]
    public string? Phone { get; set; }
    
    public UserGroup UserGroup { get; set; } = UserGroup.OPERATOR;
    public UserLevel UserLevel { get; set; } = UserLevel.LEVEL_3;
    public int OperationTimeout { get; set; } = 3600;
    public string? OperationPermissions { get; set; }
    public string? AuditPermissions { get; set; }
    public UserStatus Status { get; set; } = UserStatus.ACTIVE;
    public string? Description { get; set; }
    
    // 兼容字段
    public string? FullName { get; set; }
    public bool IsAdmin { get; set; } = false;
}

