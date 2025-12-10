-- ============================================
-- 上游液位时序数据表
-- 说明：存储分钟级液位数据，优化时间范围和聚合查询
-- ============================================

-- 1. 创建主表
CREATE TABLE IF NOT EXISTS upstream_water_levels (
    id BIGSERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    water_level NUMERIC(10, 3) NOT NULL,  -- 液位值（米），精确到毫米
    status VARCHAR(20),  -- 状态：normal, warning, alarm, offline
    data_quality SMALLINT DEFAULT 100,  -- 数据质量 0-100
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    -- 外键约束
    CONSTRAINT fk_upstream_site FOREIGN KEY (site_id) 
        REFERENCES site_configs(id) ON DELETE CASCADE
);

-- 2. 创建关键索引
-- 组合索引：site_id + timestamp（最重要，支持按站点的时间范围查询）
CREATE INDEX IF NOT EXISTS idx_upstream_levels_site_time 
    ON upstream_water_levels(site_id, timestamp DESC);

-- 时间索引：用于全局时间查询和分区剪枝
CREATE INDEX IF NOT EXISTS idx_upstream_levels_time 
    ON upstream_water_levels(timestamp DESC);

-- 3. 唯一约束（防止同一时间点重复插入）
CREATE UNIQUE INDEX IF NOT EXISTS uq_upstream_site_timestamp 
    ON upstream_water_levels(site_id, timestamp);

-- 4. 添加表注释
COMMENT ON TABLE upstream_water_levels IS '上游液位时序数据表（分钟级）';
COMMENT ON COLUMN upstream_water_levels.id IS '主键ID';
COMMENT ON COLUMN upstream_water_levels.site_id IS '站点ID';
COMMENT ON COLUMN upstream_water_levels.timestamp IS '数据时间戳';
COMMENT ON COLUMN upstream_water_levels.water_level IS '液位值（米）';
COMMENT ON COLUMN upstream_water_levels.status IS '状态：normal, warning, alarm, offline';
COMMENT ON COLUMN upstream_water_levels.data_quality IS '数据质量（0-100）';
COMMENT ON COLUMN upstream_water_levels.created_at IS '记录创建时间';

-- ============================================
-- 聚合查询视图（可选，用于常用的聚合查询）
-- ============================================

-- 小时聚合视图
CREATE OR REPLACE VIEW upstream_water_levels_hourly AS
SELECT 
    site_id,
    date_trunc('hour', timestamp) as time_bucket,
    COUNT(*) as data_count,
    AVG(water_level) as avg_level,
    MIN(water_level) as min_level,
    MAX(water_level) as max_level,
    STDDEV(water_level) as stddev_level
FROM upstream_water_levels
GROUP BY site_id, date_trunc('hour', timestamp);

-- 日聚合视图
CREATE OR REPLACE VIEW upstream_water_levels_daily AS
SELECT 
    site_id,
    date_trunc('day', timestamp) as time_bucket,
    COUNT(*) as data_count,
    AVG(water_level) as avg_level,
    MIN(water_level) as min_level,
    MAX(water_level) as max_level,
    STDDEV(water_level) as stddev_level
FROM upstream_water_levels
GROUP BY site_id, date_trunc('day', timestamp);

-- ============================================
-- 数据保留策略（可选）
-- ============================================

-- 创建函数：删除超过指定天数的数据
CREATE OR REPLACE FUNCTION cleanup_old_upstream_water_levels(retention_days INTEGER DEFAULT 365)
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM upstream_water_levels
    WHERE timestamp < NOW() - INTERVAL '1 day' * retention_days;
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION cleanup_old_upstream_water_levels IS '清理超过保留期限的上游液位数据';

-- ============================================
-- 性能优化建议
-- ============================================

/*
1. 表分区（推荐用于大数据量）：
   如果单表数据量超过1000万条，建议按月分区：
   
   CREATE TABLE upstream_water_levels (
       ...字段定义...
   ) PARTITION BY RANGE (timestamp);
   
   CREATE TABLE upstream_water_levels_2025_10 PARTITION OF upstream_water_levels
       FOR VALUES FROM ('2025-10-01') TO ('2025-11-01');

2. 定期维护：
   - 定期 VACUUM 和 ANALYZE
   - 定期重建索引（REINDEX）
   
3. 查询优化：
   - 始终在WHERE子句中包含site_id和时间范围
   - 使用date_trunc进行时间聚合
   - 考虑使用TimescaleDB扩展（专为时序数据优化）

4. 数据采集优化：
   - 使用批量插入（COPY或INSERT ... VALUES (多行)）
   - 考虑使用异步写入队列
*/

