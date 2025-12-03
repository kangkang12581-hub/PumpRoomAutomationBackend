using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

[Table("netweights")]
public class NetWeight
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("site_id")]
    public int SiteId { get; set; }

    [Required]
    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    [Required]
    [Column("netweight", TypeName = "numeric(10, 3)")]
    public decimal Value { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string Status { get; set; } = "normal";

    [Column("data_quality")]
    public int DataQuality { get; set; } = 100;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(SiteId))]
    public virtual SiteConfig? SiteConfig { get; set; }
}
