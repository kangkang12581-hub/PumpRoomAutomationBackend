-- 创建 currents 表
CREATE TABLE IF NOT EXISTS currents (
    id SERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL REFERENCES site_configs(id) ON DELETE CASCADE,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    current NUMERIC(10, 3) NOT NULL,
    status VARCHAR(20) DEFAULT 'normal',
    data_quality INTEGER DEFAULT 100,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 创建唯一索引（防止重复数据）
CREATE UNIQUE INDEX IF NOT EXISTS uq_current_site_timestamp 
    ON currents(site_id, timestamp);

-- 创建时间范围查询索引
CREATE INDEX IF NOT EXISTS idx_currents_site_time 
    ON currents(site_id, timestamp);

-- 添加注释
COMMENT ON TABLE currents IS '电流数据表';
COMMENT ON COLUMN currents.current IS '电流值 (A)';
