#!/bin/bash

# 泵房自动化系统后端启动脚本
# Pump Room Automation Backend Start Script

set -e

echo "========================================="
echo "泵房自动化系统 .NET Backend 启动脚本"
echo "========================================="
echo ""

# 检查 .NET 是否安装
if ! command -v dotnet &> /dev/null; then
    echo "❌ 错误: .NET 8.0 未安装"
    echo "请访问 https://dotnet.microsoft.com/download 下载并安装 .NET 8.0 SDK"
    exit 1
fi

echo "✅ .NET 版本:"
dotnet --version
echo ""

# 检查 PostgreSQL 连接
echo "📊 检查数据库连接..."
if ! pg_isready -h localhost -p 5432 -U pumproom_user &> /dev/null; then
    echo "⚠️  警告: 无法连接到 PostgreSQL 数据库"
    echo "请确保 PostgreSQL 服务正在运行"
    echo ""
    read -p "是否继续启动？ (y/n): " continue_choice
    if [ "$continue_choice" != "y" ]; then
        exit 1
    fi
else
    echo "✅ 数据库连接正常"
fi
echo ""

# 恢复 NuGet 包
echo "📦 恢复 NuGet 包..."
dotnet restore
echo ""

# 应用数据库迁移
echo "🗄️  应用数据库迁移..."
if dotnet ef database update; then
    echo "✅ 数据库迁移完成"
else
    echo "⚠️  数据库迁移失败，将继续启动"
fi
echo ""

# 启动应用
echo "🚀 启动应用程序..."
echo "========================================="
echo ""

# 检查是否有命令行参数
if [ "$1" == "--watch" ]; then
    echo "📝 使用热重载模式运行..."
    dotnet watch run
elif [ "$1" == "--production" ]; then
    echo "🏭 使用生产模式运行..."
    dotnet run --configuration Release
else
    echo "🔧 使用开发模式运行..."
    dotnet run
fi

