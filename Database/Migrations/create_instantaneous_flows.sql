-- 创建瞬时流量数据表
-- 用于存储各站点的瞬时流量历史数据（每分钟一条记录）

CREATE TABLE IF NOT EXISTS instantaneous_flows (
    id BIGSERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    flow_rate NUMERIC(10, 3) NOT NULL,  -- 流量值（m³/h），保留3位小数
    status VARCHAR(50) DEFAULT 'normal',  -- 状态：normal, warning, alarm, offline
    data_quality SMALLINT DEFAULT 100,    -- 数据质量（0-100）
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    -- 外键约束
    CONSTRAINT fk_flow_site FOREIGN KEY (site_id) 
        REFERENCES site_configs(id) ON DELETE CASCADE
);

-- 创建唯一索引：同一站点同一时间只能有一条记录（防止重复插入）
CREATE UNIQUE INDEX IF NOT EXISTS idx_flow_site_timestamp 
    ON instantaneous_flows(site_id, timestamp);

-- 创建时间范围查询索引（非唯一，用于高效查询）
CREATE INDEX IF NOT EXISTS idx_flow_time_range 
    ON instantaneous_flows(site_id, timestamp);

-- 创建状态索引（用于快速筛选告警数据）
CREATE INDEX IF NOT EXISTS idx_flow_status 
    ON instantaneous_flows(site_id, status) 
    WHERE status != 'normal';

-- 添加注释
COMMENT ON TABLE instantaneous_flows IS '瞬时流量历史数据表';
COMMENT ON COLUMN instantaneous_flows.id IS '主键ID';
COMMENT ON COLUMN instantaneous_flows.site_id IS '站点ID';
COMMENT ON COLUMN instantaneous_flows.timestamp IS '数据时间戳';
COMMENT ON COLUMN instantaneous_flows.flow_rate IS '瞬时流量值（m³/h）';
COMMENT ON COLUMN instantaneous_flows.status IS '状态：normal/warning/alarm/offline';
COMMENT ON COLUMN instantaneous_flows.data_quality IS '数据质量（0-100）';
COMMENT ON COLUMN instantaneous_flows.created_at IS '记录创建时间';

