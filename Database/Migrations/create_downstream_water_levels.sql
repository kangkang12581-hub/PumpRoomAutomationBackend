-- 创建下游液位数据表
-- 用于存储各站点的下游液位历史数据（每分钟一条记录）

CREATE TABLE IF NOT EXISTS downstream_water_levels (
    id BIGSERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    water_level NUMERIC(10, 3) NOT NULL,  -- 液位值（米），保留3位小数
    status VARCHAR(50) DEFAULT 'normal',  -- 状态：normal, warning, alarm, offline
    data_quality SMALLINT DEFAULT 100,    -- 数据质量（0-100）
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    -- 外键约束
    CONSTRAINT fk_downstream_site FOREIGN KEY (site_id) 
        REFERENCES site_configs(id) ON DELETE CASCADE
);

-- 创建唯一索引：同一站点同一时间只能有一条记录（防止重复插入）
CREATE UNIQUE INDEX IF NOT EXISTS idx_downstream_site_timestamp 
    ON downstream_water_levels(site_id, timestamp);

-- 创建时间范围查询索引（非唯一，用于高效查询）
CREATE INDEX IF NOT EXISTS idx_downstream_time_range 
    ON downstream_water_levels(site_id, timestamp);

-- 创建状态索引（用于快速筛选告警数据）
CREATE INDEX IF NOT EXISTS idx_downstream_status 
    ON downstream_water_levels(site_id, status) 
    WHERE status != 'normal';

-- 添加注释
COMMENT ON TABLE downstream_water_levels IS '下游液位历史数据表';
COMMENT ON COLUMN downstream_water_levels.id IS '主键ID';
COMMENT ON COLUMN downstream_water_levels.site_id IS '站点ID';
COMMENT ON COLUMN downstream_water_levels.timestamp IS '数据时间戳';
COMMENT ON COLUMN downstream_water_levels.water_level IS '下游液位值（米）';
COMMENT ON COLUMN downstream_water_levels.status IS '状态：normal/warning/alarm/offline';
COMMENT ON COLUMN downstream_water_levels.data_quality IS '数据质量（0-100）';
COMMENT ON COLUMN downstream_water_levels.created_at IS '记录创建时间';

