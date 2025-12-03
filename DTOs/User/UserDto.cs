using PumpRoomAutomationBackend.Models.Enums;

namespace PumpRoomAutomationBackend.DTOs.User;

/// <summary>
/// 用户数据传输对象
/// User Data Transfer Object
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public UserGroup UserGroup { get; set; }
    public UserLevel UserLevel { get; set; }
    public int OperationTimeout { get; set; }
    public string? OperationPermissions { get; set; }
    public string? AuditPermissions { get; set; }
    public UserStatus Status { get; set; }
    public string? Description { get; set; }
    
    // 兼容字段
    public string? FullName { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
    
    // 时间戳
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    
    // 站点分配信息
    public int SiteCount { get; set; }
    public List<SiteBriefDto> Sites { get; set; } = new();
    
    // 显示名称
    public string UserGroupDisplay => GetUserGroupDisplay();
    public string UserLevelDisplay => GetUserLevelDisplay();
    public string StatusDisplay => GetStatusDisplay();
    
    // 辅助属性
    public bool IsRoot => UserGroup == UserGroup.ROOT;
    public bool IsManager => UserGroup == UserGroup.ADMIN;
    public bool IsOperator => UserGroup == UserGroup.OPERATOR;
    public bool IsObserver => UserGroup == UserGroup.OBSERVER;
    public bool IsActiveStatus => Status == UserStatus.ACTIVE;
    
    private string GetUserGroupDisplay()
    {
        return UserGroup switch
        {
            UserGroup.ROOT => "超级管理员",
            UserGroup.ADMIN => "管理员",
            UserGroup.OPERATOR => "操作员",
            UserGroup.OBSERVER => "观察员",
            _ => "未知"
        };
    }
    
    private string GetUserLevelDisplay()
    {
        return UserLevel switch
        {
            UserLevel.LEVEL_1 => "一级",
            UserLevel.LEVEL_2 => "二级",
            UserLevel.LEVEL_3 => "三级",
            UserLevel.LEVEL_4 => "四级",
            UserLevel.LEVEL_5 => "五级",
            _ => "未知"
        };
    }
    
    private string GetStatusDisplay()
    {
        return Status switch
        {
            UserStatus.ACTIVE => "活跃",
            UserStatus.INACTIVE => "禁用",
            UserStatus.PENDING => "待审核",
            UserStatus.SUSPENDED => "暂停",
            _ => "未知"
        };
    }
}

/// <summary>
/// 站点简要信息
/// </summary>
public class SiteBriefDto
{
    public int Id { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;
}

