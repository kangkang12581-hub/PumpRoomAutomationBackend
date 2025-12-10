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
-- 创建 frequencys 表
CREATE TABLE IF NOT EXISTS frequencys (
    id SERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL REFERENCES site_configs(id) ON DELETE CASCADE,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    frequency NUMERIC(10, 3) NOT NULL,
    status VARCHAR(20) DEFAULT 'normal',
    data_quality INTEGER DEFAULT 100,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 创建唯一索引（防止重复数据）
CREATE UNIQUE INDEX IF NOT EXISTS uq_frequency_site_timestamp 
    ON frequencys(site_id, timestamp);

-- 创建时间范围查询索引
CREATE INDEX IF NOT EXISTS idx_frequencys_site_time 
    ON frequencys(site_id, timestamp);

-- 添加注释
COMMENT ON TABLE frequencys IS '频率数据表';
COMMENT ON COLUMN frequencys.frequency IS '频率值 (Hz)';
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
-- 创建 motorwindingtemps 表
CREATE TABLE IF NOT EXISTS motorwindingtemps (
    id SERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL REFERENCES site_configs(id) ON DELETE CASCADE,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    motorwindingtemp NUMERIC(10, 3) NOT NULL,
    status VARCHAR(20) DEFAULT 'normal',
    data_quality INTEGER DEFAULT 100,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 创建唯一索引（防止重复数据）
CREATE UNIQUE INDEX IF NOT EXISTS uq_motorwindingtemp_site_timestamp 
    ON motorwindingtemps(site_id, timestamp);

-- 创建时间范围查询索引
CREATE INDEX IF NOT EXISTS idx_motorwindingtemps_site_time 
    ON motorwindingtemps(site_id, timestamp);

-- 添加注释
COMMENT ON TABLE motorwindingtemps IS '绕组温度数据表';
COMMENT ON COLUMN motorwindingtemps.motorwindingtemp IS '绕组温度值 (℃)';
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
-- 创建 internaltemps 表
CREATE TABLE IF NOT EXISTS internaltemps (
    id SERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL REFERENCES site_configs(id) ON DELETE CASCADE,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    internaltemp NUMERIC(10, 3) NOT NULL,
    status VARCHAR(20) DEFAULT 'normal',
    data_quality INTEGER DEFAULT 100,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 创建唯一索引（防止重复数据）
CREATE UNIQUE INDEX IF NOT EXISTS uq_internaltemp_site_timestamp 
    ON internaltemps(site_id, timestamp);

-- 创建时间范围查询索引
CREATE INDEX IF NOT EXISTS idx_internaltemps_site_time 
    ON internaltemps(site_id, timestamp);

-- 添加注释
COMMENT ON TABLE internaltemps IS '柜内温度数据表';
COMMENT ON COLUMN internaltemps.internaltemp IS '柜内温度值 (℃)';
-- 创建 externalhumiditys 表
CREATE TABLE IF NOT EXISTS externalhumiditys (
    id SERIAL PRIMARY KEY,
    site_id INTEGER NOT NULL REFERENCES site_configs(id) ON DELETE CASCADE,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    externalhumidity NUMERIC(10, 3) NOT NULL,
    status VARCHAR(20) DEFAULT 'normal',
    data_quality INTEGER DEFAULT 100,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 创建唯一索引（防止重复数据）
CREATE UNIQUE INDEX IF NOT EXISTS uq_externalhumidity_site_timestamp 
    ON externalhumiditys(site_id, timestamp);

-- 创建时间范围查询索引
CREATE INDEX IF NOT EXISTS idx_externalhumiditys_site_time 
    ON externalhumiditys(site_id, timestamp);

-- 添加注释
COMMENT ON TABLE externalhumiditys IS '柜外湿度数据表';
COMMENT ON COLUMN externalhumiditys.externalhumidity IS '柜外湿度值 (%)';
