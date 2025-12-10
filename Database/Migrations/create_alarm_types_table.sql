-- 创建报警配置表
-- Create alarm configs table

CREATE TABLE IF NOT EXISTS alarm_configs (
    id SERIAL PRIMARY KEY,
    site_id INTEGER,                                      -- 站点ID（NULL表示全局配置）
    alarm_code VARCHAR(50) NOT NULL,                      -- 报警代码（如 ALM_001）
    alarm_name VARCHAR(200) NOT NULL,                     -- 报警名称
    alarm_message TEXT NOT NULL,                          -- 报警消息内容
    alarm_category VARCHAR(50) NOT NULL,                  -- 报警类别（重量类、电机类、流体类、通讯类等）
    severity VARCHAR(20) DEFAULT 'warning',               -- 严重程度（info, warning, error, critical）
    trigger_variable VARCHAR(100),                        -- 触发变量（OPC UA节点）
    trigger_bit INTEGER,                                  -- 触发位
    auto_clear BOOLEAN DEFAULT false,                     -- 是否自动清除
    require_confirmation BOOLEAN DEFAULT true,            -- 是否需要确认
    description TEXT,                                     -- 详细描述
    solution_guide TEXT,                                  -- 解决方案指南
    is_active BOOLEAN DEFAULT true,                       -- 是否启用
    display_order INTEGER DEFAULT 0,                      -- 显示顺序
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (site_id) REFERENCES site_configs(id) ON DELETE CASCADE,
    CONSTRAINT unique_alarm_code_per_site UNIQUE (site_id, alarm_code)
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_alarm_configs_site ON alarm_configs(site_id);
CREATE INDEX IF NOT EXISTS idx_alarm_configs_category ON alarm_configs(alarm_category);
CREATE INDEX IF NOT EXISTS idx_alarm_configs_severity ON alarm_configs(severity);
CREATE INDEX IF NOT EXISTS idx_alarm_configs_active ON alarm_configs(is_active);
CREATE INDEX IF NOT EXISTS idx_alarm_configs_site_category ON alarm_configs(site_id, alarm_category);

-- 添加注释
COMMENT ON TABLE alarm_configs IS '报警配置定义表（支持多站点配置）';
COMMENT ON COLUMN alarm_configs.site_id IS '站点ID，NULL表示全局配置（适用于所有站点）';
COMMENT ON COLUMN alarm_configs.alarm_code IS '报警代码，在同一站点内唯一';
COMMENT ON COLUMN alarm_configs.alarm_name IS '报警名称';
COMMENT ON COLUMN alarm_configs.alarm_message IS '报警消息内容';
COMMENT ON COLUMN alarm_configs.alarm_category IS '报警类别：重量类、电机类、流体类、通讯类、控制类';
COMMENT ON COLUMN alarm_configs.severity IS '严重程度：info, warning, error, critical';
COMMENT ON COLUMN alarm_configs.trigger_variable IS 'OPC UA触发变量';
COMMENT ON COLUMN alarm_configs.trigger_bit IS '触发位（用于布尔位变量）';

-- 插入全局报警配置数据（site_id为NULL，适用于所有站点）
INSERT INTO alarm_configs (site_id, alarm_code, alarm_name, alarm_message, alarm_category, severity, trigger_variable, trigger_bit, display_order) VALUES
-- 重量类报警（1-2）
(NULL, 'ALM_WEIGHT_001', '容器重量警告', '容器快要达到设定重量，请及时更换。', '重量类', 'warning', 'GWarnb_netWeight', 0, 1),
(NULL, 'ALM_WEIGHT_002', '容器重量超限', '容器已达到设定重量，请及时更换。', '重量类', 'error', 'GAb_netWeight', 0, 2),

-- 电机类报警（3-9, 20）
(NULL, 'ALM_MOTOR_001', '格栅变频器跳闸', '格栅除污机变频器电源开关跳闸，请检查处理。', '电机类', 'error', 'GAb_rotaryGrilleMotorTrip', 0, 3),
(NULL, 'ALM_MOTOR_002', '格栅电机过载', '格栅除污机电机过载，请检查处理。', '电机类', 'error', 'GAb_rotaryGrilleMotorOverLoad', 0, 4),
(NULL, 'ALM_MOTOR_003', '格栅电机过热', '格栅除污机电机绕组过热，请检查处理。', '电机类', 'error', 'GAb_rotaryGrilleMotorOverTemp', 0, 5),
(NULL, 'ALM_MOTOR_004', '格栅电机超速', '格栅除污机超速，请检查处理。', '电机类', 'error', 'GAb_rotaryGrilleOverSpeed', 0, 6),
(NULL, 'ALM_MOTOR_005', '格栅变频器报警', '格栅电机变频器报警，请检查处理。', '电机类', 'error', 'GAb_rotaryGrileDrive', 0, 7),
(NULL, 'ALM_MOTOR_006', '散热风机过载', '格栅电机散热风机过载，请检查处理。', '电机类', 'error', 'GAb_coolFan', 0, 8),
(NULL, 'ALM_MOTOR_007', '毛刷电机跳闸', '毛刷电机电源开关跳闸，请检查处理。', '电机类', 'error', 'GAb_brushMotorTrip', 0, 9),
(NULL, 'ALM_MOTOR_008', '格栅失速报警', '格栅失速报警，请检查格栅电机是否正常工作，格栅驱动链条是否断裂。', '电机类', 'critical', 'GAb_LostSteps', 0, 20),

-- 流体类报警（10-12）
(NULL, 'ALM_FLUID_001', '流量超低报警', '流量超低报警，请检查渠及多普勒流量计是否正常。', '流体类', 'warning', 'GAb_flowLowLimit', 0, 10),
(NULL, 'ALM_FLUID_002', '液位超高报警', '格栅机上游液位超高报警，请检查除污机是否堵塞，同时检查多普勒流量计。', '流体类', 'error', 'GAb_levelHighLimit', 0, 11),
(NULL, 'ALM_FLUID_003', '液位差过大', '前后级液位差过大，请检查处理。', '流体类', 'warning', 'GAb_levelDiffHigh', 0, 12),

-- 通讯类报警（13-18）
(NULL, 'ALM_COMM_001', '变频器通讯错误', '变频器通讯错误，请检查处理。', '通讯类', 'error', 'GAb_变频器_通讯错误', 0, 13),
(NULL, 'ALM_COMM_002', '柜内温湿度计通讯错误', '柜内温湿度计通讯错误，请检查处理。', '通讯类', 'warning', 'GAb_内温湿度计_通讯错误', 0, 14),
(NULL, 'ALM_COMM_003', '柜外温湿度计通讯错误', '柜外温湿度计通讯错误，请检查处理。', '通讯类', 'warning', 'GAb_外温湿度计_通讯错误', 0, 15),
(NULL, 'ALM_COMM_004', '超声波液位计通讯错误', '超声波液位计通讯错误，请检查处理。', '通讯类', 'error', 'GAb_超声波液位_通讯错误', 0, 16),
(NULL, 'ALM_COMM_005', '多普勒流量计通讯错误', '多普勒流量计通讯错误，请检查处理。', '通讯类', 'error', 'GAb_多普勒流量_通讯错误', 0, 17),
(NULL, 'ALM_COMM_006', '电子秤通讯错误', '电子秤通讯错误，请检查处理。', '通讯类', 'warning', 'GAb_电子秤_通讯错误', 0, 18),

-- 控制类报警（19）
(NULL, 'ALM_CTRL_001', '急停按钮按下', '急停按钮已按下。', '控制类', 'critical', 'Gib_急停按钮', 0, 19);

-- 为站点 3 (1号泵房) 创建所有报警配置
INSERT INTO alarm_configs (site_id, alarm_code, alarm_name, alarm_message, alarm_category, severity, trigger_variable, trigger_bit, display_order) VALUES
-- 重量类报警（1-2）
(3, 'ALM_WEIGHT_001', '容器重量警告', '容器快要达到设定重量，请及时更换。', '重量类', 'warning', 'GWarnb_netWeight', 0, 1),
(3, 'ALM_WEIGHT_002', '容器重量超限', '容器已达到设定重量，请及时更换。', '重量类', 'error', 'GAb_netWeight', 0, 2),

-- 电机类报警（3-9, 20）
(3, 'ALM_MOTOR_001', '格栅变频器跳闸', '格栅除污机变频器电源开关跳闸，请检查处理。', '电机类', 'error', 'GAb_rotaryGrilleMotorTrip', 0, 3),
(3, 'ALM_MOTOR_002', '格栅电机过载', '格栅除污机电机过载，请检查处理。', '电机类', 'error', 'GAb_rotaryGrilleMotorOverLoad', 0, 4),
(3, 'ALM_MOTOR_003', '格栅电机过热', '格栅除污机电机绕组过热，请检查处理。', '电机类', 'error', 'GAb_rotaryGrilleMotorOverTemp', 0, 5),
(3, 'ALM_MOTOR_004', '格栅电机超速', '格栅除污机超速，请检查处理。', '电机类', 'error', 'GAb_rotaryGrilleOverSpeed', 0, 6),
(3, 'ALM_MOTOR_005', '格栅变频器报警', '格栅电机变频器报警，请检查处理。', '电机类', 'error', 'GAb_rotaryGrileDrive', 0, 7),
(3, 'ALM_MOTOR_006', '散热风机过载', '格栅电机散热风机过载，请检查处理。', '电机类', 'error', 'GAb_coolFan', 0, 8),
(3, 'ALM_MOTOR_007', '毛刷电机跳闸', '毛刷电机电源开关跳闸，请检查处理。', '电机类', 'error', 'GAb_brushMotorTrip', 0, 9),
(3, 'ALM_MOTOR_008', '格栅失速报警', '格栅失速报警，请检查格栅电机是否正常工作，格栅驱动链条是否断裂。', '电机类', 'critical', 'GAb_LostSteps', 0, 20),

-- 流体类报警（10-12）
(3, 'ALM_FLUID_001', '流量超低报警', '流量超低报警，请检查渠及多普勒流量计是否正常。', '流体类', 'warning', 'GAb_flowLowLimit', 0, 10),
(3, 'ALM_FLUID_002', '液位超高报警', '格栅机上游液位超高报警，请检查除污机是否堵塞，同时检查多普勒流量计。', '流体类', 'error', 'GAb_levelHighLimit', 0, 11),
(3, 'ALM_FLUID_003', '液位差过大', '前后级液位差过大，请检查处理。', '流体类', 'warning', 'GAb_levelDiffHigh', 0, 12),

-- 通讯类报警（13-18）
(3, 'ALM_COMM_001', '变频器通讯错误', '变频器通讯错误，请检查处理。', '通讯类', 'error', 'GAb_变频器_通讯错误', 0, 13),
(3, 'ALM_COMM_002', '柜内温湿度计通讯错误', '柜内温湿度计通讯错误，请检查处理。', '通讯类', 'warning', 'GAb_内温湿度计_通讯错误', 0, 14),
(3, 'ALM_COMM_003', '柜外温湿度计通讯错误', '柜外温湿度计通讯错误，请检查处理。', '通讯类', 'warning', 'GAb_外温湿度计_通讯错误', 0, 15),
(3, 'ALM_COMM_004', '超声波液位计通讯错误', '超声波液位计通讯错误，请检查处理。', '通讯类', 'error', 'GAb_超声波液位_通讯错误', 0, 16),
(3, 'ALM_COMM_005', '多普勒流量计通讯错误', '多普勒流量计通讯错误，请检查处理。', '通讯类', 'error', 'GAb_多普勒流量_通讯错误', 0, 17),
(3, 'ALM_COMM_006', '电子秤通讯错误', '电子秤通讯错误，请检查处理。', '通讯类', 'warning', 'GAb_电子秤_通讯错误', 0, 18),

-- 控制类报警（19）
(3, 'ALM_CTRL_001', '急停按钮按下', '急停按钮已按下。', '控制类', 'critical', 'Gib_急停按钮', 0, 19);

-- 为站点 4 (2号泵房) 创建所有报警配置
INSERT INTO alarm_configs (site_id, alarm_code, alarm_name, alarm_message, alarm_category, severity, trigger_variable, trigger_bit, display_order) VALUES
-- 重量类报警（1-2）
(4, 'ALM_WEIGHT_001', '容器重量警告', '容器快要达到设定重量，请及时更换。', '重量类', 'warning', 'GWarnb_netWeight', 0, 1),
(4, 'ALM_WEIGHT_002', '容器重量超限', '容器已达到设定重量，请及时更换。', '重量类', 'error', 'GAb_netWeight', 0, 2),

-- 电机类报警（3-9, 20）
(4, 'ALM_MOTOR_001', '格栅变频器跳闸', '格栅除污机变频器电源开关跳闸，请检查处理。', '电机类', 'error', 'GAb_rotaryGrilleMotorTrip', 0, 3),
(4, 'ALM_MOTOR_002', '格栅电机过载', '格栅除污机电机过载，请检查处理。', '电机类', 'error', 'GAb_rotaryGrilleMotorOverLoad', 0, 4),
(4, 'ALM_MOTOR_003', '格栅电机过热', '格栅除污机电机绕组过热，请检查处理。', '电机类', 'error', 'GAb_rotaryGrilleMotorOverTemp', 0, 5),
(4, 'ALM_MOTOR_004', '格栅电机超速', '格栅除污机超速，请检查处理。', '电机类', 'error', 'GAb_rotaryGrilleOverSpeed', 0, 6),
(4, 'ALM_MOTOR_005', '格栅变频器报警', '格栅电机变频器报警，请检查处理。', '电机类', 'error', 'GAb_rotaryGrileDrive', 0, 7),
(4, 'ALM_MOTOR_006', '散热风机过载', '格栅电机散热风机过载，请检查处理。', '电机类', 'error', 'GAb_coolFan', 0, 8),
(4, 'ALM_MOTOR_007', '毛刷电机跳闸', '毛刷电机电源开关跳闸，请检查处理。', '电机类', 'error', 'GAb_brushMotorTrip', 0, 9),
(4, 'ALM_MOTOR_008', '格栅失速报警', '格栅失速报警，请检查格栅电机是否正常工作，格栅驱动链条是否断裂。', '电机类', 'critical', 'GAb_LostSteps', 0, 20),

-- 流体类报警（10-12）
(4, 'ALM_FLUID_001', '流量超低报警', '流量超低报警，请检查渠及多普勒流量计是否正常。', '流体类', 'warning', 'GAb_flowLowLimit', 0, 10),
(4, 'ALM_FLUID_002', '液位超高报警', '格栅机上游液位超高报警，请检查除污机是否堵塞，同时检查多普勒流量计。', '流体类', 'error', 'GAb_levelHighLimit', 0, 11),
(4, 'ALM_FLUID_003', '液位差过大', '前后级液位差过大，请检查处理。', '流体类', 'warning', 'GAb_levelDiffHigh', 0, 12),

-- 通讯类报警（13-18）
(4, 'ALM_COMM_001', '变频器通讯错误', '变频器通讯错误，请检查处理。', '通讯类', 'error', 'GAb_变频器_通讯错误', 0, 13),
(4, 'ALM_COMM_002', '柜内温湿度计通讯错误', '柜内温湿度计通讯错误，请检查处理。', '通讯类', 'warning', 'GAb_内温湿度计_通讯错误', 0, 14),
(4, 'ALM_COMM_003', '柜外温湿度计通讯错误', '柜外温湿度计通讯错误，请检查处理。', '通讯类', 'warning', 'GAb_外温湿度计_通讯错误', 0, 15),
(4, 'ALM_COMM_004', '超声波液位计通讯错误', '超声波液位计通讯错误，请检查处理。', '通讯类', 'error', 'GAb_超声波液位_通讯错误', 0, 16),
(4, 'ALM_COMM_005', '多普勒流量计通讯错误', '多普勒流量计通讯错误，请检查处理。', '通讯类', 'error', 'GAb_多普勒流量_通讯错误', 0, 17),
(4, 'ALM_COMM_006', '电子秤通讯错误', '电子秤通讯错误，请检查处理。', '通讯类', 'warning', 'GAb_电子秤_通讯错误', 0, 18),

-- 控制类报警（19）
(4, 'ALM_CTRL_001', '急停按钮按下', '急停按钮已按下。', '控制类', 'critical', 'Gib_急停按钮', 0, 19);

-- 验证插入结果（显示站点信息）
SELECT 
    at.id,
    COALESCE(sc.site_name, '全局配置') as site_name,
    at.alarm_code,
    at.alarm_name,
    at.alarm_category,
    at.severity,
    at.display_order
FROM alarm_configs at
LEFT JOIN site_configs sc ON at.site_id = sc.id
ORDER BY at.site_id NULLS FIRST, at.display_order;

-- 统计各类别报警数量
SELECT 
    alarm_category,
    COUNT(*) as count,
    STRING_AGG(severity, ', ' ORDER BY severity) as severity_types
FROM alarm_configs
GROUP BY alarm_category
ORDER BY alarm_category;

-- 按站点统计报警数量
SELECT 
    COALESCE(sc.site_name, '全局配置') as site_name,
    COUNT(*) as alarm_count
FROM alarm_configs at
LEFT JOIN site_configs sc ON at.site_id = sc.id
GROUP BY at.site_id, sc.site_name
ORDER BY at.site_id NULLS FIRST;

