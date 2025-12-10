-- 创建 externaltemps 表
CREATE TABLE IF NOT EXISTS externaltemps (
    id SERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL REFERENCES site_configs(id) ON DELETE CASCADE,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    externaltemp NUMERIC(10, 3) NOT NULL,
    status VARCHAR(20) DEFAULT 'normal',
    data_quality INTEGER DEFAULT 100,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 创建唯一索引（防止重复数据）
CREATE UNIQUE INDEX IF NOT EXISTS uq_externaltemp_site_timestamp 
    ON externaltemps(site_id, timestamp);

-- 创建时间范围查询索引
CREATE INDEX IF NOT EXISTS idx_externaltemps_site_time 
    ON externaltemps(site_id, timestamp);

-- 添加注释
COMMENT ON TABLE externaltemps IS '柜外温度数据表';
COMMENT ON COLUMN externaltemps.externaltemp IS '柜外温度值 (℃)';
