-- ============================================================
-- Payment Gateway Tables Migration
-- Run directly on each tenant DB: ascent_group_{n}
-- All statements are idempotent (IF NOT EXISTS guards).
--
-- In SSMS: connect to the tenant DB first, then execute.
--   USE ascent_group_1;  -- change to your group id
--   GO
-- ============================================================


-- ── 1. Add is_online column to payment_modes ─────────────────

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('payment_modes') AND name = 'is_online'
)
    ALTER TABLE payment_modes ADD is_online BIT NOT NULL DEFAULT 0;
GO

-- Flag any existing 'Online' / 'UPI' mode as gateway-enabled
UPDATE payment_modes
SET    is_online = 1
WHERE  mode_name LIKE '%nline%'
    OR mode_name LIKE '%UPI%'
    OR mode_name LIKE '%gateway%';
GO


-- ── 2. gateway_configs ───────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'gateway_configs')
CREATE TABLE gateway_configs (
    gateway_config_id  INT             NOT NULL IDENTITY(1,1),
    gateway_name       VARCHAR(50)     NOT NULL,    -- 'Razorpay' | 'Cashfree' | 'PayU'
    display_name       VARCHAR(100)    NOT NULL,
    key_id             VARCHAR(200)    NOT NULL,
    key_secret         VARCHAR(500)    NOT NULL,    -- never expose in API responses
    webhook_secret     VARCHAR(500)    NULL,
    is_active          BIT             NOT NULL DEFAULT 1,
    school_id          INT             NULL,        -- NULL = group-wide
    created_at         DATETIME        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_gateway_configs PRIMARY KEY (gateway_config_id)
);
GO


-- ── 3. payment_gateway_orders ─────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'payment_gateway_orders')
CREATE TABLE payment_gateway_orders (
    gateway_order_id   INT             NOT NULL IDENTITY(1,1),
    gateway_name       VARCHAR(50)     NOT NULL,
    external_order_id  VARCHAR(100)    NOT NULL,
    amount             DECIMAL(12,2)   NOT NULL,
    student_id         BIGINT          NOT NULL,
    academic_year_id   INT             NOT NULL,
    payment_mode_id    INT             NOT NULL,
    payload_json       NVARCHAR(MAX)   NOT NULL,
    created_by         VARCHAR(100)    NOT NULL,
    status             VARCHAR(10)     NOT NULL DEFAULT 'Pending',
    payment_id         VARCHAR(100)    NULL,
    receipt_id         INT             NULL,
    school_id          INT             NOT NULL,
    created_at         DATETIME        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_payment_gateway_orders   PRIMARY KEY (gateway_order_id),
    CONSTRAINT FK_gw_orders_students       FOREIGN KEY (student_id)       REFERENCES students(student_id),
    CONSTRAINT FK_gw_orders_academic_years FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
    CONSTRAINT FK_gw_orders_receipts       FOREIGN KEY (receipt_id)       REFERENCES fee_receipts(receipt_id)
);
GO
