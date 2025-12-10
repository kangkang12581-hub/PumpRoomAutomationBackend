#!/usr/bin/env pwsh
# ================================================
# PostgreSQL 本地数据库设置脚本 (Windows PowerShell)
# ================================================

Write-Host "🚀 开始设置本地 PostgreSQL 数据库..." -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Cyan

# 数据库配置
$DB_NAME = "pumproom_automation"
$DB_USER = "postgres"
$DB_PASSWORD = "123456"
$DB_HOST = "localhost"
$DB_PORT = "5432"

# 检查 PostgreSQL 是否已安装
Write-Host ""
Write-Host "1️⃣  检查 PostgreSQL 安装..." -ForegroundColor Yellow

$pgPath = Get-Command psql -ErrorAction SilentlyContinue
if (-not $pgPath) {
    Write-Host "❌ 错误：未找到 PostgreSQL (psql 命令)！" -ForegroundColor Red
    Write-Host ""
    Write-Host "请按照以下步骤安装 PostgreSQL：" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "方法 1：使用官方安装程序（推荐）" -ForegroundColor Cyan
    Write-Host "  1. 访问: https://www.postgresql.org/download/windows/" -ForegroundColor White
    Write-Host "  2. 下载 PostgreSQL 16 安装程序" -ForegroundColor White
    Write-Host "  3. 运行安装程序，设置密码为: $DB_PASSWORD" -ForegroundColor White
    Write-Host "  4. 端口保持默认: 5432" -ForegroundColor White
    Write-Host "  5. 完成安装后重新运行此脚本" -ForegroundColor White
    Write-Host ""
    Write-Host "方法 2：使用 Scoop" -ForegroundColor Cyan
    Write-Host "  scoop install postgresql" -ForegroundColor White
    Write-Host ""
    Write-Host "方法 3：使用 Chocolatey" -ForegroundColor Cyan
    Write-Host "  choco install postgresql" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "✅ PostgreSQL 已安装: $($pgPath.Source)" -ForegroundColor Green

# 检查 PostgreSQL 服务是否运行
Write-Host ""
Write-Host "2️⃣  检查 PostgreSQL 服务状态..." -ForegroundColor Yellow

$pgService = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq "Running" }
if (-not $pgService) {
    Write-Host "⚠️  PostgreSQL 服务未运行" -ForegroundColor Yellow
    Write-Host "尝试启动服务..." -ForegroundColor Yellow
    
    # 尝试查找并启动 PostgreSQL 服务
    $allPgServices = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue
    if ($allPgServices) {
        foreach ($service in $allPgServices) {
            Write-Host "启动服务: $($service.Name)..." -ForegroundColor Cyan
            try {
                Start-Service $service.Name
                Write-Host "✅ 服务已启动" -ForegroundColor Green
            } catch {
                Write-Host "❌ 无法启动服务，请手动启动 PostgreSQL 服务" -ForegroundColor Red
                Write-Host "可以在 Windows 服务管理器中启动，或运行：" -ForegroundColor Yellow
                Write-Host "  services.msc" -ForegroundColor White
                exit 1
            }
        }
    } else {
        Write-Host "❌ 未找到 PostgreSQL 服务" -ForegroundColor Red
        Write-Host "请确保 PostgreSQL 已正确安装并配置为 Windows 服务" -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "✅ PostgreSQL 服务正在运行: $($pgService.Name)" -ForegroundColor Green
}

# 设置 PGPASSWORD 环境变量（用于非交互式连接）
$env:PGPASSWORD = $DB_PASSWORD

# 测试连接
Write-Host ""
Write-Host "3️⃣  测试数据库连接..." -ForegroundColor Yellow

$testConnection = psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d postgres -c "SELECT version();" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 无法连接到 PostgreSQL！" -ForegroundColor Red
    Write-Host "错误信息: $testConnection" -ForegroundColor Red
    Write-Host ""
    Write-Host "请检查：" -ForegroundColor Yellow
    Write-Host "  1. PostgreSQL 服务是否正在运行" -ForegroundColor White
    Write-Host "  2. 用户名是否正确: $DB_USER" -ForegroundColor White
    Write-Host "  3. 密码是否正确: $DB_PASSWORD" -ForegroundColor White
    Write-Host "  4. 端口是否正确: $DB_PORT" -ForegroundColor White
    Write-Host ""
    Write-Host "如果密码不是 '$DB_PASSWORD'，请修改 appsettings.json 中的连接字符串" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ 数据库连接成功" -ForegroundColor Green

# 检查数据库是否已存在
Write-Host ""
Write-Host "4️⃣  检查数据库 '$DB_NAME'..." -ForegroundColor Yellow

$dbExists = psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$DB_NAME';" 2>&1
if ($dbExists -eq "1") {
    Write-Host "⚠️  数据库 '$DB_NAME' 已存在" -ForegroundColor Yellow
    $response = Read-Host "是否要删除并重新创建？(y/N)"
    if ($response -eq "y" -or $response -eq "Y") {
        Write-Host "删除现有数据库..." -ForegroundColor Yellow
        psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d postgres -c "DROP DATABASE $DB_NAME;" 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ 已删除现有数据库" -ForegroundColor Green
        } else {
            Write-Host "❌ 删除数据库失败" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "✅ 使用现有数据库" -ForegroundColor Green
        Write-Host ""
        Write-Host "📊 数据库连接信息：" -ForegroundColor Cyan
        Write-Host "   Host: $DB_HOST" -ForegroundColor White
        Write-Host "   Port: $DB_PORT" -ForegroundColor White
        Write-Host "   Database: $DB_NAME" -ForegroundColor White
        Write-Host "   Username: $DB_USER" -ForegroundColor White
        Write-Host "   Password: $DB_PASSWORD" -ForegroundColor White
        Write-Host ""
        Write-Host "✅ 数据库设置完成！现在可以运行应用程序了。" -ForegroundColor Green
        exit 0
    }
}

# 创建数据库
Write-Host ""
Write-Host "5️⃣  创建数据库 '$DB_NAME'..." -ForegroundColor Yellow

$createDb = psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d postgres -c "CREATE DATABASE $DB_NAME WITH ENCODING='UTF8';" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 创建数据库失败！" -ForegroundColor Red
    Write-Host "错误信息: $createDb" -ForegroundColor Red
    exit 1
}

Write-Host "✅ 数据库创建成功" -ForegroundColor Green

# 执行初始化 SQL 脚本（如果存在）
$initSqlPath = Join-Path $PSScriptRoot "Database\init-database.sql"
if (Test-Path $initSqlPath) {
    Write-Host ""
    Write-Host "6️⃣  执行初始化脚本..." -ForegroundColor Yellow
    
    psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f $initSqlPath 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ 初始化脚本执行成功" -ForegroundColor Green
    } else {
        Write-Host "⚠️  初始化脚本执行失败（这可能不影响应用运行）" -ForegroundColor Yellow
    }
}

# 清除密码环境变量
Remove-Item Env:PGPASSWORD

# 显示完成信息
Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "✅ 数据库设置完成！" -ForegroundColor Green
Write-Host ""
Write-Host "📊 数据库连接信息：" -ForegroundColor Cyan
Write-Host "   Host: $DB_HOST" -ForegroundColor White
Write-Host "   Port: $DB_PORT" -ForegroundColor White
Write-Host "   Database: $DB_NAME" -ForegroundColor White
Write-Host "   Username: $DB_USER" -ForegroundColor White
Write-Host "   Password: $DB_PASSWORD" -ForegroundColor White
Write-Host ""
Write-Host "📝 连接字符串：" -ForegroundColor Cyan
Write-Host "   Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD" -ForegroundColor White
Write-Host ""
Write-Host "🚀 下一步：运行应用程序，它将自动创建表结构（通过 EF Core Migrations）" -ForegroundColor Yellow
Write-Host "   dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "🔍 连接到数据库（可选）：" -ForegroundColor Cyan
Write-Host "   psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME" -ForegroundColor White
Write-Host ""


