-- Add camera_username column to site_configs if not exists
ALTER TABLE site_configs
ADD COLUMN IF NOT EXISTS camera_username VARCHAR(100);

