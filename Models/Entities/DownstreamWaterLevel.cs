using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

/// <summary>
/// 下游液位数据实体
/// </summary>
[Table("downstream_water_levels")]
public class DownstreamWaterLevel
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// 站点ID
    /// </summary>
    [Required]
    [Column("site_id")]
    public int SiteId { get; set; }

    /// <summary>
    /// 数据时间戳
    /// </summary>
    [Required]
    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 下游液位值（米）
    /// </summary>
    [Required]
    [Column("water_level", TypeName = "numeric(10, 3)")]
    public decimal WaterLevel { get; set; }

    /// <summary>
    /// 状态：normal, warning, alarm, offline
    /// </summary>
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "normal";

    /// <summary>
    /// 数据质量（0-100）
    /// </summary>
    [Column("data_quality")]
    public short DataQuality { get; set; } = 100;

    /// <summary>
    /// 记录创建时间
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性：关联的站点配置
    /// </summary>
    [ForeignKey(nameof(SiteId))]
    public SiteConfig? SiteConfig { get; set; }
}

