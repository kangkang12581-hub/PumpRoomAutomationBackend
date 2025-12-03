# 泵房自动化系统 - .NET 8.0 Web API

这是泵房自动化系统的后端 API 服务，使用 .NET 8.0 和 ASP.NET Core Web API 构建。

## 功能特性

- 🔐 **用户认证与授权** - JWT Token 认证，基于角色的访问控制
- 👥 **用户管理** - 完整的用户 CRUD 操作，支持多种用户角色和权限
- 📊 **数据库集成** - PostgreSQL 数据库，使用 Entity Framework Core
- ⚙️ **系统配置** - 站点配置、报警配置、运行参数管理
- 🔔 **报警系统** - 报警配置和记录管理
- 📈 **遥测数据** - 分钟级时序数据采集和存储
- 🏭 **OPC UA 集成** - 工业自动化数据采集（即将完成）
- 📹 **摄像头集成** - 海康摄像头支持（配置完成）
- 📝 **日志记录** - 使用 Serilog 进行结构化日志记录
- 📚 **API 文档** - Swagger/OpenAPI 自动文档生成

## 技术栈

- **.NET 8.0** - 最新的 .NET 框架
- **ASP.NET Core Web API** - RESTful API 框架
- **Entity Framework Core 8.0** - ORM 框架
- **PostgreSQL** - 关系型数据库
- **JWT Bearer Authentication** - 身份认证
- **BCrypt.Net** - 密码加密
- **Serilog** - 结构化日志记录
- **Swagger/OpenAPI** - API 文档

## 快速开始

### 先决条件

- .NET 8.0 SDK
- PostgreSQL 12+ 数据库
- （可选）Visual Studio 2022 或 JetBrains Rider

### 安装步骤

1. **克隆仓库**
```bash
cd /home/adminroot/PumpRoomAutomationSystem/PumpRoomAutomationBackend
```

2. **配置数据库**

编辑 `appsettings.json` 文件，配置 PostgreSQL 连接字符串：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=pumproom_automation;Username=pumproom_user;Password=pumproom_password"
  }
}
```

3. **创建数据库**

确保 PostgreSQL 服务正在运行，并创建数据库：

```bash
# 使用 psql 或 pgAdmin 创建数据库
createdb -U postgres pumproom_automation

# 创建用户（如果不存在）
psql -U postgres -c "CREATE USER pumproom_user WITH PASSWORD 'pumproom_password';"
psql -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE pumproom_automation TO pumproom_user;"
```

4. **应用数据库迁移**

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

或者直接运行应用程序，它会自动应用迁移。

5. **运行应用程序**

```bash
dotnet restore
dotnet run
```

应用程序将在 `http://localhost:5000` 启动（或您配置的端口）。

### 访问 API 文档

启动应用后，访问 Swagger UI：

```
http://localhost:5000/swagger
```

### 默认用户

系统会自动创建一个默认管理员用户：

- **用户名**: `admin`
- **密码**: `admin123`
- **角色**: 管理员

**⚠️ 重要**: 在生产环境中，请立即更改默认密码！

## API 端点

### 认证端点

- `POST /api/auth/login` - 用户登录
- `POST /api/auth/register` - 用户注册
- `GET /api/auth/me` - 获取当前用户信息

### 用户管理端点

- `GET /api/users` - 获取用户列表（分页、搜索、筛选）
- `GET /api/users/{id}` - 获取指定用户
- `PUT /api/users/{id}` - 更新用户信息
- `DELETE /api/users/{id}` - 删除用户

### 健康检查

- `GET /health` - 健康检查端点（包含数据库连接检查）

## 配置说明

### JWT 配置

在 `appsettings.json` 中配置 JWT 设置：

```json
{
  "JwtSettings": {
    "Secret": "your-super-secret-jwt-key-change-this-in-production-must-be-at-least-32-characters",
    "Issuer": "PumpRoomAutomationSystem",
    "Audience": "PumpRoomAutomationClient",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 7
  }
}
```

### OPC UA 配置

```json
{
  "OpcUaSettings": {
    "Url": "opc.tcp://192.168.30.102:4840",
    "Timeout": 10000,
    "SecurityPolicy": "None",
    "SecurityMode": "None",
    "Anonymous": true,
    "Username": "",
    "Password": "",
    "SessionTimeout": 30000,
    "RequestTimeout": 10000,
    "MaxRetries": 5,
    "RetryDelay": 3000
  }
}
```

### 摄像头配置

```json
{
  "CameraSettings": {
    "Ip": "192.168.30.102",
    "Username": "admin",
    "Password": "Luvan12?",
    "RtspPort": 554,
    "HttpPort": 80
  }
}
```

### 日志配置

使用 Serilog 进行日志记录，日志文件位于 `logs/` 目录。

## 项目结构

```
PumpRoomAutomationBackend/
├── Configuration/          # 配置类
│   ├── JwtSettings.cs
│   ├── OpcUaSettings.cs
│   ├── AppSettings.cs
│   └── CameraSettings.cs
├── Controllers/            # API 控制器
│   ├── AuthController.cs
│   └── UsersController.cs
├── Data/                   # 数据访问层
│   └── ApplicationDbContext.cs
├── DTOs/                   # 数据传输对象
│   ├── Auth/
│   ├── User/
│   └── Common/
├── Models/                 # 数据模型
│   ├── Entities/
│   └── Enums/
├── Services/               # 业务逻辑服务
│   ├── AuthService.cs
│   ├── UserService.cs
│   └── Security/
├── Program.cs              # 应用程序入口
├── appsettings.json        # 配置文件
└── PumpRoomAutomationBackend.csproj
```

## 数据模型

### 核心实体

- **User** - 用户信息
- **LoginLog** - 登录日志
- **AlarmConfig** - 报警配置
- **AlarmRecord** - 报警记录
- **SystemConfig** - 系统配置
- **SiteConfig** - 站点配置
- **OperationalParameters** - 运行参数
- **UserSettings** - 用户设置
- **TelemetryMinute** - 分钟级遥测数据

### 用户角色

- **ROOT** - 超级管理员
- **ADMIN** - 管理员
- **OPERATOR** - 操作员
- **OBSERVER** - 观察员

### 用户级别

- **LEVEL_1** - 一级
- **LEVEL_2** - 二级
- **LEVEL_3** - 三级
- **LEVEL_4** - 四级
- **LEVEL_5** - 五级

## 开发指南

### 添加新的 Entity

1. 在 `Models/Entities/` 中创建实体类
2. 在 `ApplicationDbContext` 中添加 DbSet
3. 在 `OnModelCreating` 中配置关系和约束
4. 创建迁移: `dotnet ef migrations add AddNewEntity`
5. 应用迁移: `dotnet ef database update`

### 添加新的 API 端点

1. 在 `DTOs/` 中创建请求和响应 DTO
2. 在 `Services/` 中实现业务逻辑
3. 在 `Controllers/` 中创建控制器
4. 在 `Program.cs` 中注册服务

## 部署

### 发布应用

```bash
dotnet publish -c Release -o ./publish
```

### 使用 Docker（即将支持）

```bash
# 构建镜像
docker build -t pumproom-backend:latest .

# 运行容器
docker run -d -p 5000:5000 \
  -e ConnectionStrings__DefaultConnection="Host=db;Port=5432;Database=pumproom_automation;Username=pumproom_user;Password=pumproom_password" \
  pumproom-backend:latest
```

### 使用 systemd（Linux）

创建服务文件 `/etc/systemd/system/pumproom-backend.service`:

```ini
[Unit]
Description=Pump Room Automation Backend
After=network.target postgresql.service

[Service]
Type=notify
User=www-data
WorkingDirectory=/var/www/pumproom-backend
ExecStart=/usr/bin/dotnet /var/www/pumproom-backend/PumpRoomAutomationBackend.dll
Restart=always
RestartSec=10
SyslogIdentifier=pumproom-backend

[Install]
WantedBy=multi-user.target
```

启动服务：

```bash
sudo systemctl enable pumproom-backend
sudo systemctl start pumproom-backend
sudo systemctl status pumproom-backend
```

## 故障排除

### 数据库连接失败

1. 检查 PostgreSQL 服务是否运行
2. 验证连接字符串是否正确
3. 确认数据库用户有足够的权限

### JWT 认证失败

1. 确认 JWT Secret 长度至少 32 个字符
2. 检查令牌是否过期
3. 验证 Issuer 和 Audience 配置

### 迁移错误

```bash
# 删除所有迁移并重新创建
dotnet ef database drop
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 性能优化

- 启用响应缓存
- 使用异步操作
- 数据库查询优化
- 添加索引
- 使用连接池

## 安全建议

1. **更改默认密码** - 立即更改默认管理员密码
2. **使用强密钥** - 生成强随机 JWT Secret
3. **启用 HTTPS** - 在生产环境中强制使用 HTTPS
4. **限制 CORS** - 只允许信任的来源
5. **日志审计** - 监控和审查登录日志
6. **定期更新** - 保持依赖包最新

## 贡献

欢迎提交问题和拉取请求！

## 许可证

[指定许可证]

## 联系方式

技术支持: support@pumproom.com

