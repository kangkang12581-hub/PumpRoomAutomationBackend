namespace PumpRoomAutomationBackend.Models.Enums;

/// <summary>
/// 用户组枚举
/// User group enumeration
/// </summary>
public enum UserGroup
{
    /// <summary>
    /// 超级管理员
    /// </summary>
    ROOT,
    
    /// <summary>
    /// 管理员
    /// </summary>
    ADMIN,
    
    /// <summary>
    /// 操作员
    /// </summary>
    OPERATOR,
    
    /// <summary>
    /// 观察员
    /// </summary>
    OBSERVER
}

/// <summary>
/// 用户级别枚举
/// User level enumeration
/// </summary>
public enum UserLevel
{
    /// <summary>
    /// 一级
    /// </summary>
    LEVEL_1,
    
    /// <summary>
    /// 二级
    /// </summary>
    LEVEL_2,
    
    /// <summary>
    /// 三级
    /// </summary>
    LEVEL_3,
    
    /// <summary>
    /// 四级
    /// </summary>
    LEVEL_4,
    
    /// <summary>
    /// 五级
    /// </summary>
    LEVEL_5
}

/// <summary>
/// 用户状态枚举
/// User status enumeration
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// 活跃
    /// </summary>
    ACTIVE,
    
    /// <summary>
    /// 禁用
    /// </summary>
    INACTIVE,
    
    /// <summary>
    /// 待审核
    /// </summary>
    PENDING,
    
    /// <summary>
    /// 暂停
    /// </summary>
    SUSPENDED
}

