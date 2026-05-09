-- sms_otp_migration.sql
-- Run on ascent_master database
-- Adds device binding to parent_accounts and creates OTP table for SMS auth

USE ascent_master;
GO

-- 1. Add device binding columns to parent_accounts
ALTER TABLE parent_accounts
    ADD device_id       VARCHAR(255) NULL,
        device_bound_at DATETIME     NULL;
GO

-- 2. Create OTP table (hashed, per-request, rate-limitable)
CREATE TABLE parent_otp (
    otp_id     INT IDENTITY(1,1)  PRIMARY KEY,
    mobile     VARCHAR(20)  NOT NULL,
    otp_hash   VARCHAR(64)  NOT NULL,   -- SHA-256 of 6-digit OTP
    device_id  VARCHAR(255) NOT NULL,   -- device that requested OTP
    expires_at DATETIME     NOT NULL,   -- 5 minutes from created_at
    is_used    BIT          NOT NULL DEFAULT 0,
    created_at DATETIME     NOT NULL DEFAULT GETDATE()
);
GO

CREATE INDEX IX_parent_otp_lookup ON parent_otp (mobile, is_used, expires_at);
GO
