-- 为 site_configs 表添加 OPC UA 配置字段
-- Migration: Add OPC UA configuration fields to site_configs table

-- 1. 添加 OPC UA 连接配置字段
ALTER TABLE site_configs
ADD COLUMN IF NOT EXISTS opcua_endpoint VARCHAR(255),
ADD COLUMN IF NOT EXISTS opcua_security_policy VARCHAR(50) DEFAULT 'None',
ADD COLUMN IF NOT EXISTS opcua_security_mode VARCHAR(50) DEFAULT 'None',
ADD COLUMN IF NOT EXISTS opcua_anonymous BOOLEAN DEFAULT true,
ADD COLUMN IF NOT EXISTS opcua_username VARCHAR(100),
ADD COLUMN IF NOT EXISTS opcua_password VARCHAR(255),
ADD COLUMN IF NOT EXISTS opcua_session_timeout INTEGER DEFAULT 30000,
ADD COLUMN IF NOT EXISTS opcua_request_timeout INTEGER DEFAULT 10000;

-- 2. 为现有记录更新 OPC UA 端点（如果 ip_address 和 port 已存在）
UPDATE site_configs
SET opcua_endpoint = 'opc.tcp://' || ip_address || ':' || COALESCE(port::text, '4840')
WHERE opcua_endpoint IS NULL 
  AND ip_address IS NOT NULL;

-- 3. 为没有端口的记录设置默认端口
UPDATE site_configs
SET port = 4840
WHERE port IS NULL AND protocol = 'OPC.UA';

-- 4. 添加注释
COMMENT ON COLUMN site_configs.opcua_endpoint IS 'OPC UA 服务器端点地址，如 opc.tcp://192.168.10.88:4840';
COMMENT ON COLUMN site_configs.opcua_security_policy IS 'OPC UA 安全策略：None, Basic256Sha256';
COMMENT ON COLUMN site_configs.opcua_security_mode IS 'OPC UA 安全模式：None, Sign, SignAndEncrypt';
COMMENT ON COLUMN site_configs.opcua_anonymous IS '是否使用匿名连接';
COMMENT ON COLUMN site_configs.opcua_username IS 'OPC UA 用户名（如果不使用匿名）';
COMMENT ON COLUMN site_configs.opcua_password IS 'OPC UA 密码（加密存储）';
COMMENT ON COLUMN site_configs.opcua_session_timeout IS 'OPC UA 会话超时时间（毫秒）';
COMMENT ON COLUMN site_configs.opcua_request_timeout IS 'OPC UA 请求超时时间（毫秒）';

-- 验证添加结果
SELECT column_name, data_type, character_maximum_length, column_default
FROM information_schema.columns
WHERE table_name = 'site_configs'
  AND column_name LIKE 'opcua%'
ORDER BY ordinal_position;

