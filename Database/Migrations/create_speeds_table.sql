-- 创建 speeds 表 (速度历史记录表)
CREATE TABLE IF NOT EXISTS speeds (
    id SERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL REFERENCES site_configs(id) ON DELETE CASCADE,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    speed NUMERIC(18, 3) NOT NULL,
    status VARCHAR(20) DEFAULT 'normal',
    data_quality INTEGER DEFAULT 100,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 创建唯一索引（防止重复数据）
CREATE UNIQUE INDEX IF NOT EXISTS uq_speed_site_timestamp 
    ON speeds(site_id, timestamp);

-- 创建时间范围查询索引
CREATE INDEX IF NOT EXISTS idx_speeds_site_time 
    ON speeds(site_id, timestamp);

-- 添加注释
COMMENT ON TABLE speeds IS '速度数据表 (Speed Data Table)';
COMMENT ON COLUMN speeds.speed IS '速度值 (转/分 或 m/s)';
COMMENT ON COLUMN speeds.timestamp IS '数据时间戳';
COMMENT ON COLUMN speeds.status IS '状态: normal, warning, alarm, offline';
COMMENT ON COLUMN speeds.data_quality IS '数据质量 (0-100)';

