-- 修复数值字段精度问题
-- Fix numeric field precision issues
-- 将 NUMERIC(10, 3) 扩展为 NUMERIC(18, 3) 以支持更大范围的工业数据

-- 修改 netweights 表
ALTER TABLE netweights 
    ALTER COLUMN netweight TYPE NUMERIC(18, 3);

-- 修改 frequencys 表
ALTER TABLE frequencys 
    ALTER COLUMN frequency TYPE NUMERIC(18, 3);

-- 修改 currents 表  
ALTER TABLE currents 
    ALTER COLUMN current TYPE NUMERIC(18, 3);

-- 修改 motorwindingtemps 表
ALTER TABLE motorwindingtemps 
    ALTER COLUMN motorwindingtemp TYPE NUMERIC(18, 3);

-- 修改 externaltemps 表
ALTER TABLE externaltemps 
    ALTER COLUMN externaltemp TYPE NUMERIC(18, 3);

-- 修改 internaltemps 表
ALTER TABLE internaltemps 
    ALTER COLUMN internaltemp TYPE NUMERIC(18, 3);

-- 修改 externalhumiditys 表
ALTER TABLE externalhumiditys 
    ALTER COLUMN externalhumidity TYPE NUMERIC(18, 3);

-- 添加注释
COMMENT ON COLUMN netweights.netweight IS '净重值，支持大范围工业数据 (NUMERIC(18,3))';
COMMENT ON COLUMN frequencys.frequency IS '频率值 (Hz)，支持大范围工业数据 (NUMERIC(18,3))';
COMMENT ON COLUMN currents.current IS '电流值 (A)，支持大范围工业数据 (NUMERIC(18,3))';
COMMENT ON COLUMN motorwindingtemps.motorwindingtemp IS '绕组温度值 (℃)，支持大范围工业数据 (NUMERIC(18,3))';
COMMENT ON COLUMN externaltemps.externaltemp IS '柜外温度值 (℃)，支持大范围工业数据 (NUMERIC(18,3))';
COMMENT ON COLUMN internaltemps.internaltemp IS '柜内温度值 (℃)，支持大范围工业数据 (NUMERIC(18,3))';
COMMENT ON COLUMN externalhumiditys.externalhumidity IS '柜外湿度值 (%)，支持大范围工业数据 (NUMERIC(18,3))';

