-- 创建流速数据表
CREATE TABLE IF NOT EXISTS flow_velocities (
    id BIGSERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    velocity NUMERIC(10, 3) NOT NULL,
    status VARCHAR(50) DEFAULT 'normal',
    data_quality SMALLINT DEFAULT 100,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_velocity_site FOREIGN KEY (site_id) REFERENCES site_configs(id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_velocity_site_timestamp ON flow_velocities(site_id, timestamp);
CREATE INDEX IF NOT EXISTS idx_velocity_time_range ON flow_velocities(site_id, timestamp);
CREATE INDEX IF NOT EXISTS idx_velocity_status ON flow_velocities(site_id, status) WHERE status != 'normal';

COMMENT ON TABLE flow_velocities IS '流速历史数据表';
COMMENT ON COLUMN flow_velocities.velocity IS '流速值（m/s）';

