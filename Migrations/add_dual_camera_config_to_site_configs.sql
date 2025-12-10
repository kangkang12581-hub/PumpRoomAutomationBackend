-- Migration: Add dual camera configuration to site_configs
-- Date: 2025-11-20
-- Description: Replace single camera config with separate internal and global camera configs

-- Step 1: Add new camera columns for internal camera (机内摄像头)
ALTER TABLE site_configs
ADD COLUMN IF NOT EXISTS internal_camera_ip VARCHAR(45);

ALTER TABLE site_configs
ADD COLUMN IF NOT EXISTS internal_camera_username VARCHAR(100);

ALTER TABLE site_configs
ADD COLUMN IF NOT EXISTS internal_camera_password VARCHAR(255);

-- Step 2: Add new camera columns for global camera (全局摄像头)
ALTER TABLE site_configs
ADD COLUMN IF NOT EXISTS global_camera_ip VARCHAR(45);

ALTER TABLE site_configs
ADD COLUMN IF NOT EXISTS global_camera_username VARCHAR(100);

ALTER TABLE site_configs
ADD COLUMN IF NOT EXISTS global_camera_password VARCHAR(255);

-- Step 3: Migrate existing camera data to internal camera (if any exists)
UPDATE site_configs
SET internal_camera_ip = camera_ip,
    internal_camera_username = camera_username,
    internal_camera_password = camera_password
WHERE camera_ip IS NOT NULL;

-- Step 4: Drop old camera columns (optional - comment out if you want to keep for backup)
-- ALTER TABLE site_configs DROP COLUMN IF EXISTS camera_ip;
-- ALTER TABLE site_configs DROP COLUMN IF EXISTS camera_username;
-- ALTER TABLE site_configs DROP COLUMN IF EXISTS camera_password;

-- Note: The old columns are kept commented to allow rollback if needed
-- To complete the migration, uncomment the DROP statements after verifying the migration

