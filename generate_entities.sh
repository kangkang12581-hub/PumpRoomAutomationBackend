#!/bin/bash

# 净重 NetWeight
cat > Models/Entities/NetWeight.cs << 'CSEOF'
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
CSEOF

# 频率 Frequency  
cat > Models/Entities/Frequency.cs << 'CSEOF'
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

[Table("frequencys")]
public class Frequency
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
    [Column("frequency", TypeName = "numeric(10, 3)")]
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
CSEOF

# 电流 Current
cat > Models/Entities/Current.cs << 'CSEOF'
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

[Table("currents")]
public class Current
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
    [Column("current", TypeName = "numeric(10, 3)")]
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
CSEOF

# 绕组温度 MotorWindingTemp
cat > Models/Entities/MotorWindingTemp.cs << 'CSEOF'
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

[Table("motorwindingtemps")]
public class MotorWindingTemp
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
    [Column("motorwindingtemp", TypeName = "numeric(10, 3)")]
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
CSEOF

# 柜外温度 ExternalTemp
cat > Models/Entities/ExternalTemp.cs << 'CSEOF'
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

[Table("externaltemps")]
public class ExternalTemp
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
    [Column("externaltemp", TypeName = "numeric(10, 3)")]
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
CSEOF

# 柜内温度 InternalTemp
cat > Models/Entities/InternalTemp.cs << 'CSEOF'
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

[Table("internaltemps")]
public class InternalTemp
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
    [Column("internaltemp", TypeName = "numeric(10, 3)")]
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
CSEOF

# 柜外湿度 ExternalHumidity
cat > Models/Entities/ExternalHumidity.cs << 'CSEOF'
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PumpRoomAutomationBackend.Models.Entities;

[Table("externalhumiditys")]
public class ExternalHumidity
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
    [Column("externalhumidity", TypeName = "numeric(10, 3)")]
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
CSEOF

echo "✅ 所有Entity文件创建完成！"
ls -lh Models/Entities/{NetWeight,Frequency,Current,MotorWindingTemp,ExternalTemp,InternalTemp,ExternalHumidity}.cs
