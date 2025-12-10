-- Add camera fields to site_configs
ALTER TABLE site_configs
ADD COLUMN IF NOT EXISTS camera_ip VARCHAR(45);

ALTER TABLE site_configs
ADD COLUMN IF NOT EXISTS camera_password VARCHAR(255);
