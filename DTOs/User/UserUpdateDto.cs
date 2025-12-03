using System.ComponentModel.DataAnnotations;
using PumpRoomAutomationBackend.Models.Enums;

namespace PumpRoomAutomationBackend.DTOs.User;

/// <summary>
/// 更新用户数据传输对象
/// Update User Data Transfer Object
/// </summary>
public class UserUpdateDto
{
    [StringLength(100, MinimumLength = 1, ErrorMessage = "显示名称长度必须在1到100个字符之间")]
    public string? DisplayName { get; set; }
    
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }
    
    [StringLength(20, ErrorMessage = "电话号码最多20个字符")]
    public string? Phone { get; set; }
    
    public UserGroup? UserGroup { get; set; }
    public UserLevel? UserLevel { get; set; }
    public int? OperationTimeout { get; set; }
    public string? OperationPermissions { get; set; }
    public string? AuditPermissions { get; set; }
    public UserStatus? Status { get; set; }
    public string? Description { get; set; }
    
    // 兼容字段
    public string? FullName { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsAdmin { get; set; }
}

