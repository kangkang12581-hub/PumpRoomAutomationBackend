-- ================================================
-- 泵房自动化系统数据库初始化脚本
-- Pump Room Automation System Database Initialization
-- ================================================

-- 1. 创建数据库（如果不存在）
-- 注意：此命令需要在 postgres 默认数据库中执行
-- CREATE DATABASE pumproom_automation 
--     WITH 
--     ENCODING = 'UTF8'
--     LC_COLLATE = 'Chinese (Simplified)_China.936'
--     LC_CTYPE = 'Chinese (Simplified)_China.936'
--     TEMPLATE = template0;

-- 2. 连接到新创建的数据库
-- \c pumproom_automation;

-- 3. 创建扩展（如果需要）
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- 4. 设置时区
SET timezone = 'UTC';

-- 数据库初始化完成
-- 应用程序会通过 Entity Framework Core Migrations 自动创建表结构


