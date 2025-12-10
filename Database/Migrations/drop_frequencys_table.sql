-- 删除 frequencys 表（可选）
-- Drop frequencys table (optional)
-- 如果确定不再需要频率数据，可以执行此脚本删除表

-- 警告：此操作将永久删除所有频率数据，请谨慎执行！
-- WARNING: This will permanently delete all frequency data!

-- 删除表
DROP TABLE IF EXISTS frequencys CASCADE;

-- 删除序列（如果存在）
DROP SEQUENCE IF EXISTS frequencys_id_seq;

-- 确认删除
-- SELECT 'frequencys 表已删除' AS status;

