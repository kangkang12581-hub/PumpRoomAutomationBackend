#!/bin/bash

# 泵房自动化系统后端设置脚本
# Pump Room Automation Backend Setup Script

set -e

echo "========================================="
echo "泵房自动化系统 .NET Backend 设置脚本"
echo "========================================="
echo ""

# 检查 .NET 是否安装
if ! command -v dotnet &> /dev/null; then
    echo "❌ 错误: .NET 8.0 未安装"
    echo ""
    echo "正在尝试安装 .NET 8.0..."
    
    # 检查操作系统
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        # Linux
        wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
        chmod +x dotnet-install.sh
        ./dotnet-install.sh --channel 8.0
        
        echo "export DOTNET_ROOT=\$HOME/.dotnet" >> ~/.bashrc
        echo "export PATH=\$PATH:\$DOTNET_ROOT:\$DOTNET_ROOT/tools" >> ~/.bashrc
        source ~/.bashrc
        
        rm dotnet-install.sh
    else
        echo "请手动安装 .NET 8.0 SDK"
        echo "下载地址: https://dotnet.microsoft.com/download"
        exit 1
    fi
fi

echo "✅ .NET 版本:"
dotnet --version
echo ""

# 检查 PostgreSQL
echo "📊 检查 PostgreSQL..."
if ! command -v psql &> /dev/null; then
    echo "⚠️  PostgreSQL 未安装"
    echo "请安装 PostgreSQL 12 或更高版本"
    echo ""
    read -p "是否继续设置？ (y/n): " continue_choice
    if [ "$continue_choice" != "y" ]; then
        exit 1
    fi
else
    echo "✅ PostgreSQL 已安装"
    psql --version
fi
echo ""

# 创建数据库
echo "🗄️  配置数据库..."
read -p "是否创建数据库？ (y/n): " create_db
if [ "$create_db" == "y" ]; then
    read -p "PostgreSQL 超级用户名 (默认: postgres): " pg_user
    pg_user=${pg_user:-postgres}
    
    echo "创建数据库和用户..."
    psql -U $pg_user -c "CREATE DATABASE pumproom_automation;" || echo "数据库可能已存在"
    psql -U $pg_user -c "CREATE USER pumproom_user WITH PASSWORD 'pumproom_password';" || echo "用户可能已存在"
    psql -U $pg_user -c "GRANT ALL PRIVILEGES ON DATABASE pumproom_automation TO pumproom_user;"
    
    echo "✅ 数据库配置完成"
fi
echo ""

# 安装 EF Core 工具
echo "🔧 安装 Entity Framework Core 工具..."
dotnet tool install --global dotnet-ef || dotnet tool update --global dotnet-ef
echo ""

# 恢复 NuGet 包
echo "📦 恢复 NuGet 包..."
dotnet restore
echo ""

# 创建初始迁移
echo "🗄️  创建数据库迁移..."
if [ ! -d "Migrations" ]; then
    dotnet ef migrations add InitialCreate
    echo "✅ 迁移创建完成"
else
    echo "ℹ️  迁移目录已存在，跳过创建"
fi
echo ""

# 应用迁移
echo "🗄️  应用数据库迁移..."
read -p "是否应用迁移？ (y/n): " apply_migration
if [ "$apply_migration" == "y" ]; then
    dotnet ef database update
    echo "✅ 数据库迁移完成"
fi
echo ""

# 配置环境变量
echo "⚙️  配置应用程序..."
if [ ! -f "appsettings.Development.json" ]; then
    cp appsettings.json appsettings.Development.json
    echo "✅ 开发配置文件已创建"
fi
echo ""

# 构建项目
echo "🔨 构建项目..."
dotnet build
echo ""

echo "========================================="
echo "✅ 设置完成！"
echo "========================================="
echo ""
echo "下一步："
echo "1. 编辑 appsettings.json 配置数据库连接和 JWT 密钥"
echo "2. 运行 './start.sh' 启动应用"
echo "3. 访问 http://localhost:5000/swagger 查看 API 文档"
echo ""
echo "默认管理员账号："
echo "  用户名: admin"
echo "  密码: admin123"
echo ""
echo "⚠️  重要: 请在生产环境中更改默认密码和 JWT 密钥！"
echo ""

