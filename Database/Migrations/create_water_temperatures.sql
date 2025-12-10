CREATE TABLE IF NOT EXISTS water_temperatures (
    id BIGSERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    temperature NUMERIC(10, 3) NOT NULL,
    status VARCHAR(50) DEFAULT 'normal',
    data_quality SMALLINT DEFAULT 100,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_temp_site FOREIGN KEY (site_id) REFERENCES site_configs(id) ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_temp_site_timestamp ON water_temperatures(site_id, timestamp);
CREATE INDEX IF NOT EXISTS idx_temp_time_range ON water_temperatures(site_id, timestamp);
COMMENT ON TABLE water_temperatures IS '水温历史数据表';
COMMENT ON COLUMN water_temperatures.temperature IS '水温值（℃）';
