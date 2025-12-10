-- 创建 netweights 表
CREATE TABLE IF NOT EXISTS netweights (
    id SERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL REFERENCES site_configs(id) ON DELETE CASCADE,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    netweight NUMERIC(10, 3) NOT NULL,
    status VARCHAR(20) DEFAULT 'normal',
    data_quality INTEGER DEFAULT 100,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 创建唯一索引（防止重复数据）
CREATE UNIQUE INDEX IF NOT EXISTS uq_netweight_site_timestamp 
    ON netweights(site_id, timestamp);

-- 创建时间范围查询索引
CREATE INDEX IF NOT EXISTS idx_netweights_site_time 
    ON netweights(site_id, timestamp);

-- 添加注释
COMMENT ON TABLE netweights IS '净重数据表';
COMMENT ON COLUMN netweights.netweight IS '净重值 (kg)';
