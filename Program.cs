using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Npgsql;
using PumpRoomAutomationBackend.Configuration;
using PumpRoomAutomationBackend.Data;
using PumpRoomAutomationBackend.Services;
using PumpRoomAutomationBackend.Services.Security;
using PumpRoomAutomationBackend.Services.OpcUa;
using PumpRoomAutomationBackend.Services.Email;
using PumpRoomAutomationBackend.Models.Enums;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// 加载配置
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings not found");
var appSettings = builder.Configuration.GetSection(AppSettings.SectionName).Get<AppSettings>()
    ?? new AppSettings();
var opcUaSettings = builder.Configuration.GetSection(OpcUaSettings.SectionName).Get<OpcUaSettings>()
    ?? new OpcUaSettings();
var cameraSettings = builder.Configuration.GetSection(CameraSettings.SectionName).Get<CameraSettings>()
    ?? new CameraSettings();

// 注册配置
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.SectionName));
builder.Services.Configure<OpcUaSettings>(builder.Configuration.GetSection(OpcUaSettings.SectionName));
builder.Services.Configure<CameraSettings>(builder.Configuration.GetSection(CameraSettings.SectionName));

// 添加数据库上下文
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Database connection string not found");

// 配置 Npgsql 数据源和枚举映射（Npgsql 7.0+ 方式）
// 使用 PgName 注解映射到数据库小写枚举标签
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
// 用户相关枚举：数据库标签为大写，使用 NullNameTranslator 直通
dataSourceBuilder.MapEnum<UserStatus>("userstatus", new Npgsql.NameTranslation.NpgsqlNullNameTranslator());
dataSourceBuilder.MapEnum<UserGroup>("usergroup", new Npgsql.NameTranslation.NpgsqlNullNameTranslator());
dataSourceBuilder.MapEnum<UserLevel>("userlevel", new Npgsql.NameTranslation.NpgsqlNullNameTranslator());
// 报警相关枚举：使用 PgName(小写) 注解
dataSourceBuilder.MapEnum<AlarmSeverity>("alarmseverity");
dataSourceBuilder.MapEnum<AlarmStatus>("alarmstatus");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(dataSource));

// 添加认证
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 添加 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:5173",
                "http://0.0.0.0:3000",
                "http://0.0.0.0:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// 注册服务
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<IAlarmConfigService, AlarmConfigService>();
builder.Services.AddScoped<IAlarmRecordService, AlarmRecordService>();

// 邮件和通知服务
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICameraService, CameraService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IAlarmNotificationService, AlarmNotificationService>();
builder.Services.AddHttpClient(); // 用于调用 HikVision 截图 API 和短信平台 API
builder.Services.AddScoped<IUpstreamWaterLevelService, UpstreamWaterLevelService>();
builder.Services.AddScoped<IDownstreamWaterLevelService, DownstreamWaterLevelService>();
builder.Services.AddScoped<IInstantaneousFlowService, InstantaneousFlowService>();
builder.Services.AddScoped<IFlowVelocityService, FlowVelocityService>();
builder.Services.AddScoped<IWaterTemperatureService, WaterTemperatureService>();
builder.Services.AddScoped<INetWeightService, NetWeightService>();
builder.Services.AddScoped<ICurrentService, CurrentService>();
builder.Services.AddScoped<IMotorWindingTempService, MotorWindingTempService>();
builder.Services.AddScoped<IExternalTempService, ExternalTempService>();
builder.Services.AddScoped<IInternalTempService, InternalTempService>();
builder.Services.AddScoped<IExternalHumidityService, ExternalHumidityService>();
builder.Services.AddScoped<IInternalHumidityService, InternalHumidityService>();
builder.Services.AddScoped<ISpeedService, SpeedService>();

// 注册 OPC UA 服务
builder.Services.AddSingleton<IOpcUaCache, OpcUaCache>();
builder.Services.AddSingleton<IOpcUaClient, OpcUaClientService>();

// 多站点 OPC UA 服务
builder.Services.AddSingleton<IOpcUaConnectionManager, OpcUaConnectionManager>();
builder.Services.AddHostedService<OpcUaHostedServiceMulti>();

// 注意：如果要使用旧的单连接服务，请注释上面两行并取消下面的注释
// builder.Services.AddHostedService<OpcUaHostedService>();

// 数据采集服务（每分钟自动存储）
builder.Services.AddHostedService<UpstreamWaterLevelCollectorService>();
builder.Services.AddHostedService<DownstreamWaterLevelCollectorService>();
builder.Services.AddHostedService<InstantaneousFlowCollectorService>();
builder.Services.AddHostedService<FlowVelocityCollectorService>();
builder.Services.AddHostedService<WaterTemperatureCollectorService>();
builder.Services.AddHostedService<NetWeightCollectorService>();
builder.Services.AddHostedService<CurrentCollectorService>();
builder.Services.AddHostedService<MotorWindingTempCollectorService>();
builder.Services.AddHostedService<ExternalTempCollectorService>();
builder.Services.AddHostedService<InternalTempCollectorService>();
builder.Services.AddHostedService<ExternalHumidityCollectorService>();
builder.Services.AddHostedService<InternalHumidityCollectorService>();
builder.Services.AddHostedService<SpeedCollectorService>();

// 报警监听服务
builder.Services.AddHostedService<AlarmMonitorService>();

// 添加控制器
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 配置JSON序列化为camelCase，与前端保持一致
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // 允许读取number类型的字符串
        options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
        // 允许枚举使用字符串进行序列化/反序列化（例如："ADMIN"、"OPERATOR"）
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// 添加健康检查
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database", tags: new[] { "db", "sql" });

// 添加 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = appSettings.AppName,
        Version = appSettings.AppVersion,
        Description = "泵房自动化系统 Web API - 用户认证、数据管理、OPC UA 集成",
        Contact = new OpenApiContact
        {
            Name = "技术支持",
            Email = "support@pumproom.com"
        }
    });

    // 添加 JWT 认证支持
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 数据库初始化
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // 应用待处理的迁移
        await context.Database.MigrateAsync();

        Log.Information("✅ 数据库初始化完成");

        // 创建默认管理员用户
        var authService = services.GetRequiredService<IAuthService>();
        var passwordService = services.GetRequiredService<IPasswordService>();
        var existingAdmin = await authService.GetUserByUsernameAsync("admin");

        if (existingAdmin == null)
        {
            await authService.CreateUserAsync(
                "admin",
                "admin123",
                "系统管理员",
                "admin@pumproom.com",
                null,
                true
            );

            Log.Information("✅ 默认管理员用户创建成功:");
            Log.Information("   👤 用户名: admin");
            Log.Information("   🔑 密码: admin123");
            Log.Information("   🛡️  角色: 管理员");
        }
        else
        {
            Log.Information("ℹ️  默认管理员用户已存在，跳过创建");
        }

        // 创建默认超级管理员（ROOT）用户（如不存在）
        var existingRoot = await authService.GetUserByUsernameAsync("root");
        if (existingRoot == null)
        {
            var rootUser = new PumpRoomAutomationBackend.Models.Entities.User
            {
                Username = "root",
                DisplayName = "超级管理员",
                Email = "root@pumproom.com",
                HashedPassword = passwordService.HashPassword("root123"),
                UserGroup = PumpRoomAutomationBackend.Models.Enums.UserGroup.ROOT,
                UserLevel = PumpRoomAutomationBackend.Models.Enums.UserLevel.LEVEL_1,
                IsAdmin = true,
                IsActive = true,
                Status = PumpRoomAutomationBackend.Models.Enums.UserStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(rootUser);
            await context.SaveChangesAsync();

            Log.Information("✅ 默认超级管理员用户创建成功:");
            Log.Information("   👤 用户名: root");
            Log.Information("   🔑 密码: root123");
            Log.Information("   🛡️  角色: ROOT");
        }
        else
        {
            Log.Information("ℹ️  超级管理员用户已存在，跳过创建");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ 数据库初始化失败: {Message}", ex.Message);
    }
}

// 配置中间件管道
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{appSettings.AppName} v{appSettings.AppVersion}");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 健康检查端点
app.MapHealthChecks("/health");

// 根路径
app.MapGet("/", () => new
{
    message = appSettings.AppName,
    version = appSettings.AppVersion,
    status = "running",
    timestamp = DateTime.UtcNow
});

Log.Information("=" + new string('=', 60));
Log.Information($"🚀 启动 {appSettings.AppName} v{appSettings.AppVersion}");
Log.Information("=" + new string('=', 60));
Log.Information($"🌐 服务器地址: {builder.Configuration["Urls"] ?? "http://localhost:5000"}");
Log.Information($"📚 API文档: {builder.Configuration["Urls"] ?? "http://localhost:5000"}/swagger");
Log.Information("=" + new string('=', 60));

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用程序启动失败");
}
finally
{
    Log.CloseAndFlush();
}

