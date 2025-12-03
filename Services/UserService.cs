using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs.User;
using PumpRoomAutomationBackend.Models.Entities;
using PumpRoomAutomationBackend.Models.Enums;
using PumpRoomAutomationBackend.Services.Security;

namespace PumpRoomAutomationBackend.Services;

/// <summary>
/// 用户服务
/// User Service
/// </summary>
public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync(int page = 1, int size = 20, string? search = null, UserGroup? userGroup = null, UserStatus? status = null);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<List<UserDto>> GetUsersBySiteIdAsync(int siteId);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<UserDto> CreateUserAsync(UserCreateDto createDto);
    Task<UserDto?> UpdateUserAsync(int id, UserUpdateDto updateDto);
    Task<bool> DeleteUserAsync(int id);
    Task<int> GetTotalUsersAsync(string? search = null, UserGroup? userGroup = null, UserStatus? status = null);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    
    public UserService(ApplicationDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }
    
    public async Task<List<UserDto>> GetUsersAsync(
        int page = 1, 
        int size = 20, 
        string? search = null, 
        UserGroup? userGroup = null, 
        UserStatus? status = null)
    {
        var query = _context.Users.AsQueryable();
        
        // 重要：只有当status明确有值时才过滤，null表示返回所有用户（包括非活跃用户）
        // 不添加任何默认的status过滤，确保返回所有用户
        
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => 
                u.Username.Contains(search) || 
                u.DisplayName.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)));
        }
        
        if (userGroup.HasValue)
        {
            query = query.Where(u => u.UserGroup == userGroup.Value);
        }
        
        // 只有当status明确有值时才过滤状态
        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }
        // 如果status为null，不添加任何状态过滤，返回所有用户
        
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        // 载入站点分配信息
        var userIds = users.Select(u => u.Id).ToList();
        var userSites = await _context.UserSites
            .Where(us => userIds.Contains(us.UserId))
            .Join(_context.SiteConfigs,
                us => us.SiteId,
                sc => sc.Id,
                (us, sc) => new { us.UserId, Site = sc })
            .ToListAsync();

        var userIdToSites = userSites
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new DTOs.User.SiteBriefDto
                {
                    Id = x.Site.Id,
                    SiteName = x.Site.SiteName,
                    SiteCode = x.Site.SiteCode
                }).ToList()
            );

        var result = new List<UserDto>();
        foreach (var u in users)
        {
            var dto = MapToDto(u);
            if (userIdToSites.TryGetValue(u.Id, out var sites))
            {
                dto.SiteCount = sites.Count;
                dto.Sites = sites;
            }
            else
            {
                dto.SiteCount = 0;
                dto.Sites = new List<DTOs.User.SiteBriefDto>();
            }
            result.Add(dto);
        }

        return result;
    }
    
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        // 载入站点分配
        var userIds = users.Select(u => u.Id).ToList();
        var userSites = await _context.UserSites
            .Where(us => userIds.Contains(us.UserId))
            .Join(_context.SiteConfigs,
                us => us.SiteId,
                sc => sc.Id,
                (us, sc) => new { us.UserId, Site = sc })
            .ToListAsync();

        var userIdToSites = userSites
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new DTOs.User.SiteBriefDto
                {
                    Id = x.Site.Id,
                    SiteName = x.Site.SiteName,
                    SiteCode = x.Site.SiteCode
                }).ToList()
            );

        var result = new List<UserDto>();
        foreach (var u in users)
        {
            var dto = MapToDto(u);
            if (userIdToSites.TryGetValue(u.Id, out var sites))
            {
                dto.SiteCount = sites.Count;
                dto.Sites = sites;
            }
            result.Add(dto);
        }

        return result;
    }

    public async Task<List<UserDto>> GetUsersBySiteIdAsync(int siteId)
    {
        // 通过 user_sites 关联查询属于该站点的用户
        var userIds = await _context.UserSites
            .Where(us => us.SiteId == siteId)
            .Select(us => us.UserId)
            .Distinct()
            .ToListAsync();

        if (userIds.Count == 0)
            return new List<UserDto>();

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .OrderBy(u => u.Username)
            .ToListAsync();

        return users.Select(MapToDto).ToList();
    }
    
    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;
        var dto = MapToDto(user);
        var sites = await _context.UserSites
            .Where(us => us.UserId == user.Id)
            .Join(_context.SiteConfigs, us => us.SiteId, sc => sc.Id, (us, sc) => sc)
            .ToListAsync();
        dto.SiteCount = sites.Count;
        dto.Sites = sites.Select(sc => new DTOs.User.SiteBriefDto { Id = sc.Id, SiteName = sc.SiteName, SiteCode = sc.SiteCode }).ToList();
        return dto;
    }
    
    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user == null ? null : MapToDto(user);
    }
    
    public async Task<UserDto> CreateUserAsync(UserCreateDto createDto)
    {
        if (string.IsNullOrWhiteSpace(createDto.Password))
        {
            throw new InvalidOperationException("密码不能为空");
        }
        
        // 检查用户名重复
        var exists = await _context.Users.AnyAsync(u => u.Username == createDto.Username);
        if (exists)
        {
            throw new InvalidOperationException("用户名已存在");
        }
        
        var user = new User
        {
            Username = createDto.Username,
            DisplayName = createDto.DisplayName,
            HashedPassword = _passwordService.HashPassword(createDto.Password),
            Email = createDto.Email,
            Phone = createDto.Phone,
            UserGroup = createDto.UserGroup,
            UserLevel = createDto.UserLevel,
            OperationTimeout = createDto.OperationTimeout,
            OperationPermissions = createDto.OperationPermissions,
            AuditPermissions = createDto.AuditPermissions,
            Status = createDto.Status,
            Description = createDto.Description,
            FullName = createDto.FullName ?? createDto.DisplayName,
            IsActive = createDto.Status == UserStatus.ACTIVE,
            IsAdmin = (createDto.UserGroup == UserGroup.ADMIN || createDto.UserGroup == UserGroup.ROOT),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return MapToDto(user);
    }
    
    public async Task<UserDto?> UpdateUserAsync(int id, UserUpdateDto updateDto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return null;
        
        if (!string.IsNullOrEmpty(updateDto.DisplayName))
            user.DisplayName = updateDto.DisplayName;
        
        if (updateDto.Email != null)
            user.Email = updateDto.Email;
        
        if (updateDto.Phone != null)
            user.Phone = updateDto.Phone;
        
        if (updateDto.UserGroup.HasValue)
            user.UserGroup = updateDto.UserGroup.Value;
        
        if (updateDto.UserLevel.HasValue)
            user.UserLevel = updateDto.UserLevel.Value;
        
        if (updateDto.OperationTimeout.HasValue)
            user.OperationTimeout = updateDto.OperationTimeout.Value;
        
        if (updateDto.OperationPermissions != null)
            user.OperationPermissions = updateDto.OperationPermissions;
        
        if (updateDto.AuditPermissions != null)
            user.AuditPermissions = updateDto.AuditPermissions;
        
        if (updateDto.Status.HasValue)
            user.Status = updateDto.Status.Value;
        
        if (updateDto.Description != null)
            user.Description = updateDto.Description;
        
        // 兼容字段处理
        if (updateDto.FullName != null)
            user.FullName = updateDto.FullName;
        
        if (updateDto.IsActive.HasValue)
        {
            user.IsActive = updateDto.IsActive.Value;
            user.Status = updateDto.IsActive.Value ? UserStatus.ACTIVE : UserStatus.INACTIVE;
        }
        
        if (updateDto.IsAdmin.HasValue && updateDto.IsAdmin.Value)
        {
            user.IsAdmin = true;
            user.UserGroup = UserGroup.ADMIN;
        }
        
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        
        return MapToDto(user);
    }
    
    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return false;
        
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<int> GetTotalUsersAsync(
        string? search = null, 
        UserGroup? userGroup = null, 
        UserStatus? status = null)
    {
        var query = _context.Users.AsQueryable();
        
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => 
                u.Username.Contains(search) || 
                u.DisplayName.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)));
        }
        
        if (userGroup.HasValue)
        {
            query = query.Where(u => u.UserGroup == userGroup.Value);
        }
        
        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }
        
        return await query.CountAsync();
    }
    
    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Phone = user.Phone,
            UserGroup = user.UserGroup,
            UserLevel = user.UserLevel,
            OperationTimeout = user.OperationTimeout,
            OperationPermissions = user.OperationPermissions,
            AuditPermissions = user.AuditPermissions,
            Status = user.Status,
            Description = user.Description,
            FullName = user.FullName,
            IsActive = user.IsActive,
            IsAdmin = user.IsAdmin,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLogin = user.LastLogin
        };
    }
}

