using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

[Table("speeds")]
public class Speed
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [Column("site_id")]
    public int SiteId { get; set; }

    [Required]
    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    [Required]
    [Column("speed")]
    public decimal SpeedValue { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string? Status { get; set; }

    [Column("data_quality")]
    public int DataQuality { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation property
    [ForeignKey("SiteId")]
    public virtual SiteConfig? Site { get; set; }
}

