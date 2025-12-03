using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.DTOs;
using PumpRoomAutomationBackend.DTOs.Common;
using PumpRoomAutomationBackend.Models.Entities;
using System.Security.Claims;

namespace PumpRoomAutomationBackend.Controllers;

[ApiController]
[Route("api/system-configs")]
[Authorize(Roles = "ROOT,ADMIN")] // 仅ROOT/ADMIN可管理系统配置
public class SystemConfigController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SystemConfigController> _logger;

    public SystemConfigController(ApplicationDbContext context, ILogger<SystemConfigController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SystemConfigDto>>> Create([FromBody] SystemConfigCreateDto dto)
    {
        try
        {
            // Prefer NameIdentifier (user id) from JWT; fallback to legacy "nameid"
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(ApiResponse<SystemConfigDto>.Fail("无法识别用户", "UNAUTHORIZED"));
            }

            // upsert: 如果已存在记录，则更新；否则新增。系统配置仅保留一条
            var existing = await _context.SystemConfigs
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.UpdatedAt)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.UserId = userId;
                existing.PhoneAlarmAddress = dto.PhoneAlarmAddress;
                existing.PhoneAccessId = dto.PhoneAccessId;
                existing.PhoneAccessKey = dto.PhoneAccessKey;
                existing.SmsAccessId = dto.SmsAccessId;
                existing.SmsAccessKey = dto.SmsAccessKey;
                existing.SmtpServer = dto.SmtpServer;
                existing.SmtpPort = dto.SmtpPort ?? existing.SmtpPort;
                existing.EmailAccount = dto.EmailAccount;
                existing.EmailPassword = dto.EmailPassword;
                if (dto.IsActive.HasValue) existing.IsActive = dto.IsActive.Value; // 仅当前端显式传入时才变更
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(ApiResponse<SystemConfigDto>.Ok(Map(existing), "更新成功"));
            }
            else
            {
                var entity = new SystemConfig
                {
                    UserId = userId,
                    PhoneAlarmAddress = dto.PhoneAlarmAddress,
                    PhoneAccessId = dto.PhoneAccessId,
                    PhoneAccessKey = dto.PhoneAccessKey,
                    SmsAccessId = dto.SmsAccessId,
                    SmsAccessKey = dto.SmsAccessKey,
                    SmtpServer = dto.SmtpServer,
                    SmtpPort = dto.SmtpPort ?? 587,
                    EmailAccount = dto.EmailAccount,
                    EmailPassword = dto.EmailPassword,
                    IsActive = dto.IsActive ?? true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SystemConfigs.Add(entity);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<SystemConfigDto>.Ok(Map(entity), "创建成功"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建系统配置失败");
            return StatusCode(500, ApiResponse<SystemConfigDto>.Fail("创建系统配置失败", "INTERNAL_ERROR"));
        }
    }

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<SystemConfigDto?>>> GetActive()
    {
        var cfg = await _context.SystemConfigs
            .OrderByDescending(c => c.IsActive)
            .ThenByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync();
        return Ok(ApiResponse<SystemConfigDto?>.Ok(cfg != null ? Map(cfg) : null, "获取成功"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        if (page <= 0) page = 1;
        if (size <= 0 || size > 200) size = 20;

        var query = _context.SystemConfigs.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c =>
                (c.PhoneAlarmAddress ?? "").ToLower().Contains(s) ||
                (c.SmsAccessId ?? "").ToLower().Contains(s) ||
                (c.EmailAccount ?? "").ToLower().Contains(s) ||
                (c.SmtpServer ?? "").ToLower().Contains(s)
            );
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)size);

        var items = await query
            .OrderByDescending(c => c.IsActive)
            .ThenByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var result = new
        {
            items = items.Select(Map).ToList(),
            total,
            page,
            size,
            totalPages
        };

        return Ok(ApiResponse<object>.Ok(result, "获取成功"));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<SystemConfigDto>>> GetById([FromRoute] int id)
    {
        var cfg = await _context.SystemConfigs.FindAsync(id);
        if (cfg == null)
        {
            return NotFound(ApiResponse<SystemConfigDto>.Fail("未找到配置", "NOT_FOUND"));
        }
        return Ok(ApiResponse<SystemConfigDto>.Ok(Map(cfg), "获取成功"));
    }

    private static SystemConfigDto Map(SystemConfig c) => new SystemConfigDto
    {
        Id = c.Id,
        UserId = c.UserId,
        PhoneAlarmAddress = c.PhoneAlarmAddress,
        PhoneAccessId = c.PhoneAccessId,
        PhoneAccessKey = c.PhoneAccessKey,
        SmsAccessId = c.SmsAccessId,
        SmsAccessKey = c.SmsAccessKey,
        SmtpServer = c.SmtpServer,
        SmtpPort = c.SmtpPort,
        EmailAccount = c.EmailAccount,
        EmailPassword = c.EmailPassword,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}


