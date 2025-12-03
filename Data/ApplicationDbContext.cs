using Microsoft.EntityFrameworkCore;
using PumpRoomAutomationBackend.Models.Entities;

namespace PumpRoomAutomationBackend.Data;

/// <summary>
/// 应用程序数据库上下文
/// Application Database Context
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    // DbSets - 实体集合
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<LoginLog> LoginLogs { get; set; } = null!;
    public DbSet<UserAlarmNotificationConfig> UserAlarmNotifications { get; set; } = null!;
    public DbSet<AlarmRecord> AlarmRecords { get; set; } = null!;
    public DbSet<AlarmConfig> AlarmConfigs { get; set; } = null!;
    public DbSet<SystemConfig> SystemConfigs { get; set; } = null!;
    public DbSet<SiteConfig> SiteConfigs { get; set; } = null!;
    public DbSet<OperationalParameters> OperationalParameters { get; set; } = null!;
    public DbSet<UserSettings> UserSettings { get; set; } = null!;
    public DbSet<TelemetryMinute> TelemetryMinutes { get; set; } = null!;
    public DbSet<UserSite> UserSites { get; set; } = null!;
    public DbSet<UpstreamWaterLevel> UpstreamWaterLevels { get; set; } = null!;
    public DbSet<DownstreamWaterLevel> DownstreamWaterLevels { get; set; } = null!;
    public DbSet<InstantaneousFlow> InstantaneousFlows { get; set; } = null!;
    public DbSet<FlowVelocity> FlowVelocities { get; set; } = null!;
    public DbSet<WaterTemperature> WaterTemperatures { get; set; } = null!;
    public DbSet<NetWeight> NetWeights { get; set; } = null!;
    public DbSet<Current> Currents { get; set; } = null!;
    public DbSet<MotorWindingTemp> MotorWindingTemps { get; set; } = null!;
    public DbSet<ExternalTemp> ExternalTemps { get; set; } = null!;
    public DbSet<InternalTemp> InternalTemps { get; set; } = null!;
    public DbSet<ExternalHumidity> ExternalHumiditys { get; set; } = null!;
    public DbSet<InternalHumidity> InternalHumiditys { get; set; } = null!;
    public DbSet<Speed> Speeds { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User 配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            // 使用 PostgreSQL 枚举类型（已在 Program.cs 中配置映射）
            entity.Property(e => e.UserGroup)
                .HasColumnType("usergroup");
            
            entity.Property(e => e.UserLevel)
                .HasColumnType("userlevel");
            
            entity.Property(e => e.Status)
                .HasColumnType("userstatus");
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // LoginLog 配置
        modelBuilder.Entity<LoginLog>(entity =>
        {
            entity.Property(e => e.LoginTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // UserAlarmNotificationConfig 配置
        modelBuilder.Entity<UserAlarmNotificationConfig>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany(u => u.AlarmConfigs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // AlarmRecord 配置
        modelBuilder.Entity<AlarmRecord>(entity =>
        {
            entity.HasOne(e => e.Site)
                .WithMany(s => s.AlarmRecords)
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // 使用 PostgreSQL 枚举类型（已在 Program.cs 中配置映射）
            entity.Property(e => e.Severity)
                .HasColumnType("alarmseverity");
            
            entity.Property(e => e.Status)
                .HasColumnType("alarmstatus");
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // AlarmConfig 配置
        modelBuilder.Entity<AlarmConfig>(entity =>
        {
            entity.HasIndex(e => e.AlarmCode).IsUnique();
            entity.HasIndex(e => e.AlarmCategory);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.IsActive);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // SystemConfig 配置
        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany(u => u.SystemConfigs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // SiteConfig 配置
        modelBuilder.Entity<SiteConfig>(entity =>
        {
            entity.HasIndex(e => e.SiteCode).IsUnique();
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.SiteConfigs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // OperationalParameters 配置
        modelBuilder.Entity<OperationalParameters>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany(u => u.OperationalParameters)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // UserSettings 配置
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            
            entity.HasOne(e => e.User)
                .WithOne(u => u.Settings)
                .HasForeignKey<UserSettings>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // TelemetryMinute 配置
        modelBuilder.Entity<TelemetryMinute>(entity =>
        {
            // 同一站点同一分钟唯一，避免重复写入
            entity.HasIndex(e => new { e.SiteCode, e.TsMinute })
                .IsUnique()
                .HasDatabaseName("uq_site_minute");
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // UserSite 配置
        modelBuilder.Entity<UserSite>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.SiteId })
                .IsUnique()
                .HasDatabaseName("uq_user_site");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.SiteConfig)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // UpstreamWaterLevel 配置
        modelBuilder.Entity<UpstreamWaterLevel>(entity =>
        {
            // 组合唯一索引：同一站点同一时间点只能有一条记录
            entity.HasIndex(e => new { e.SiteId, e.Timestamp })
                .IsUnique()
                .HasDatabaseName("uq_upstream_site_timestamp");
            
            // 时间范围查询索引（降序，最新数据优先）
            entity.HasIndex(e => new { e.SiteId, e.Timestamp })
                .HasDatabaseName("idx_upstream_levels_site_time");
            
            // 外键关系
            entity.HasOne(e => e.SiteConfig)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // DownstreamWaterLevel 配置
        modelBuilder.Entity<DownstreamWaterLevel>(entity =>
        {
            // 组合唯一索引：同一站点同一时间点只能有一条记录
            entity.HasIndex(e => new { e.SiteId, e.Timestamp })
                .IsUnique()
                .HasDatabaseName("uq_downstream_site_timestamp");
            
            // 时间范围查询索引（降序，最新数据优先）
            entity.HasIndex(e => new { e.SiteId, e.Timestamp })
                .HasDatabaseName("idx_downstream_levels_site_time");
            
            // 外键关系
            entity.HasOne(e => e.SiteConfig)
                .WithMany()
                .HasForeignKey(e => e.SiteId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // InstantaneousFlow 配置
        modelBuilder.Entity<InstantaneousFlow>(entity =>
        {
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).IsUnique().HasDatabaseName("uq_flow_site_timestamp");
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).HasDatabaseName("idx_flows_site_time");
            entity.HasOne(e => e.SiteConfig).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // FlowVelocity 配置
        modelBuilder.Entity<FlowVelocity>(entity =>
        {
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).IsUnique().HasDatabaseName("uq_velocity_site_timestamp");
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).HasDatabaseName("idx_velocities_site_time");
            entity.HasOne(e => e.SiteConfig).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // WaterTemperature 配置
        modelBuilder.Entity<WaterTemperature>(entity =>
        {
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).IsUnique().HasDatabaseName("uq_temp_site_timestamp");
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).HasDatabaseName("idx_temps_site_time");
            entity.HasOne(e => e.SiteConfig).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // NetWeight 配置
        modelBuilder.Entity<NetWeight>(entity =>
        {
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).IsUnique().HasDatabaseName("uq_netweights_site_timestamp");
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).HasDatabaseName("idx_netweights_site_time");
            entity.HasOne(e => e.SiteConfig).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // Current 配置
        modelBuilder.Entity<Current>(entity =>
        {
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).IsUnique().HasDatabaseName("uq_currents_site_timestamp");
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).HasDatabaseName("idx_currents_site_time");
            entity.HasOne(e => e.SiteConfig).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // MotorWindingTemp 配置
        modelBuilder.Entity<MotorWindingTemp>(entity =>
        {
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).IsUnique().HasDatabaseName("uq_motorwindingtemps_site_timestamp");
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).HasDatabaseName("idx_motorwindingtemps_site_time");
            entity.HasOne(e => e.SiteConfig).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // ExternalTemp 配置
        modelBuilder.Entity<ExternalTemp>(entity =>
        {
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).IsUnique().HasDatabaseName("uq_externaltemps_site_timestamp");
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).HasDatabaseName("idx_externaltemps_site_time");
            entity.HasOne(e => e.SiteConfig).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // InternalTemp 配置
        modelBuilder.Entity<InternalTemp>(entity =>
        {
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).IsUnique().HasDatabaseName("uq_internaltemps_site_timestamp");
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).HasDatabaseName("idx_internaltemps_site_time");
            entity.HasOne(e => e.SiteConfig).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // ExternalHumidity 配置
        modelBuilder.Entity<ExternalHumidity>(entity =>
        {
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).IsUnique().HasDatabaseName("uq_externalhumiditys_site_timestamp");
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).HasDatabaseName("idx_externalhumiditys_site_time");
            entity.HasOne(e => e.SiteConfig).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // InternalHumidity 配置
        modelBuilder.Entity<InternalHumidity>(entity =>
        {
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).IsUnique().HasDatabaseName("uq_internalhumiditys_site_timestamp");
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).HasDatabaseName("idx_internalhumiditys_site_time");
            entity.HasOne(e => e.SiteConfig).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // Speed 配置
        modelBuilder.Entity<Speed>(entity =>
        {
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).IsUnique().HasDatabaseName("uq_speeds_site_timestamp");
            entity.HasIndex(e => new { e.SiteId, e.Timestamp }).HasDatabaseName("idx_speeds_site_time");
            entity.HasOne(e => e.Site).WithMany().HasForeignKey(e => e.SiteId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 自动更新时间戳
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        
        foreach (var entry in entries)
        {
            if (entry.Entity.GetType().GetProperty("UpdatedAt") != null)
            {
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            }
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }
}

