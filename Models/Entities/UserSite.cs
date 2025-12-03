using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

/// <summary>
/// 用户-站点关联表
/// User-Site Association Table
/// </summary>
[Table("user_sites")]
public class UserSite
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("site_id")]
    public int SiteId { get; set; }

    [Column("role")]
    [MaxLength(20)]
    public string? Role { get; set; }

    [Column("is_owner")]
    public bool? IsOwner { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性
    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("SiteId")]
    public SiteConfig? SiteConfig { get; set; }
}

