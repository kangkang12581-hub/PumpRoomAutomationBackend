-- 创建 internalhumiditys 表 (柜内湿度历史记录表)
CREATE TABLE IF NOT EXISTS internalhumiditys (
    id SERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL REFERENCES site_configs(id) ON DELETE CASCADE,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    internalhumidity NUMERIC(18, 3) NOT NULL,
    status VARCHAR(20) DEFAULT 'normal',
    data_quality INTEGER DEFAULT 100,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 创建唯一索引（防止重复数据）
CREATE UNIQUE INDEX IF NOT EXISTS uq_internalhumidity_site_timestamp 
    ON internalhumiditys(site_id, timestamp);

-- 创建时间范围查询索引
CREATE INDEX IF NOT EXISTS idx_internalhumiditys_site_time 
    ON internalhumiditys(site_id, timestamp);

-- 添加注释
COMMENT ON TABLE internalhumiditys IS '柜内湿度数据表 (Internal Humidity Data Table)';
COMMENT ON COLUMN internalhumiditys.internalhumidity IS '柜内湿度值 (%)';
COMMENT ON COLUMN internalhumiditys.timestamp IS '数据时间戳';
COMMENT ON COLUMN internalhumiditys.status IS '状态: normal, warning, alarm, offline';
COMMENT ON COLUMN internalhumiditys.data_quality IS '数据质量 (0-100)';

