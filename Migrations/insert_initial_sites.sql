-- 插入初始站点配置数据
-- Insert initial site configurations

-- 清空现有站点数据（可选，如果需要重新初始化）
-- DELETE FROM site_configs;

-- 插入站点 1：1号泵房
INSERT INTO site_configs (
    user_id,
    site_code,
    site_name,
    site_location,
    site_description,
    ip_address,
    port,
    protocol,
    opcua_endpoint,
    opcua_security_policy,
    opcua_security_mode,
    opcua_anonymous,
    opcua_username,
    opcua_password,
    opcua_session_timeout,
    opcua_request_timeout,
    contact_person,
    contact_phone,
    operating_pressure_min,
    operating_pressure_max,
    pump_count,
    is_enabled,
    is_online,
    connection_status,
    is_default,
    alarm_enabled,
    is_active,
    created_at,
    updated_at
) VALUES (
    3,                                          -- user_id (关联到 admin 用户)
    'SITE_001',                                 -- site_code
    '1号泵房',                                  -- site_name
    '192.168.10.88 机房',                       -- site_location
    '1号站点，连接到 192.168.10.88 PLC',        -- site_description
    '192.168.10.88',                            -- ip_address
    4840,                                       -- port
    'OPC.UA',                                   -- protocol
    'opc.tcp://192.168.10.88:4840',            -- opcua_endpoint
    'None',                                     -- opcua_security_policy
    'None',                                     -- opcua_security_mode
    true,                                       -- opcua_anonymous
    NULL,                                       -- opcua_username
    NULL,                                       -- opcua_password
    30000,                                      -- opcua_session_timeout
    10000,                                      -- opcua_request_timeout
    '张三',                                     -- contact_person
    '13800138001',                              -- contact_phone
    '0.2',                                      -- operating_pressure_min
    '1.0',                                      -- operating_pressure_max
    2,                                          -- pump_count
    true,                                       -- is_enabled
    false,                                      -- is_online (初始离线，等待连接)
    'disconnected',                             -- connection_status
    true,                                       -- is_default (默认站点)
    true,                                       -- alarm_enabled
    true,                                       -- is_active
    CURRENT_TIMESTAMP,                          -- created_at
    CURRENT_TIMESTAMP                           -- updated_at
)
ON CONFLICT (site_code) DO UPDATE SET
    site_name = EXCLUDED.site_name,
    ip_address = EXCLUDED.ip_address,
    port = EXCLUDED.port,
    opcua_endpoint = EXCLUDED.opcua_endpoint,
    updated_at = CURRENT_TIMESTAMP;

-- 插入站点 2：2号泵房
INSERT INTO site_configs (
    user_id,
    site_code,
    site_name,
    site_location,
    site_description,
    ip_address,
    port,
    protocol,
    opcua_endpoint,
    opcua_security_policy,
    opcua_security_mode,
    opcua_anonymous,
    opcua_username,
    opcua_password,
    opcua_session_timeout,
    opcua_request_timeout,
    contact_person,
    contact_phone,
    operating_pressure_min,
    operating_pressure_max,
    pump_count,
    is_enabled,
    is_online,
    connection_status,
    is_default,
    alarm_enabled,
    is_active,
    created_at,
    updated_at
) VALUES (
    3,                                          -- user_id (关联到 admin 用户)
    'SITE_002',                                 -- site_code
    '2号泵房',                                  -- site_name
    '192.168.10.89 机房',                       -- site_location
    '2号站点，连接到 192.168.10.89 PLC',        -- site_description
    '192.168.10.89',                            -- ip_address
    4840,                                       -- port
    'OPC.UA',                                   -- protocol
    'opc.tcp://192.168.10.89:4840',            -- opcua_endpoint
    'None',                                     -- opcua_security_policy
    'None',                                     -- opcua_security_mode
    true,                                       -- opcua_anonymous
    NULL,                                       -- opcua_username
    NULL,                                       -- opcua_password
    30000,                                      -- opcua_session_timeout
    10000,                                      -- opcua_request_timeout
    '李四',                                     -- contact_person
    '13900139002',                              -- contact_phone
    '0.2',                                      -- operating_pressure_min
    '1.0',                                      -- operating_pressure_max
    2,                                          -- pump_count
    true,                                       -- is_enabled
    false,                                      -- is_online (初始离线，等待连接)
    'disconnected',                             -- connection_status
    false,                                      -- is_default
    true,                                       -- alarm_enabled
    true,                                       -- is_active
    CURRENT_TIMESTAMP,                          -- created_at
    CURRENT_TIMESTAMP                           -- updated_at
)
ON CONFLICT (site_code) DO UPDATE SET
    site_name = EXCLUDED.site_name,
    ip_address = EXCLUDED.ip_address,
    port = EXCLUDED.port,
    opcua_endpoint = EXCLUDED.opcua_endpoint,
    updated_at = CURRENT_TIMESTAMP;

-- 验证插入结果
SELECT 
    id,
    site_code,
    site_name,
    ip_address,
    port,
    opcua_endpoint,
    is_enabled,
    is_online,
    connection_status,
    is_default
FROM site_configs
ORDER BY site_code;

