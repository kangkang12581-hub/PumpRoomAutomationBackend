using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace PumpRoomAutomationBackend.Models.Entities;
[Table("water_temperatures")]
public class WaterTemperature
{
    [Key][Column("id")][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    [Required][Column("site_id")]
    public int SiteId { get; set; }
    [Required][Column("timestamp")]
    public DateTime Timestamp { get; set; }
    [Required][Column("temperature", TypeName = "numeric(10, 3)")]
    public decimal Temperature { get; set; }
    [Column("status")][MaxLength(50)]
    public string Status { get; set; } = "normal";
    [Column("data_quality")]
    public short DataQuality { get; set; } = 100;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(SiteId))]
    public SiteConfig? SiteConfig { get; set; }
}
