-- ============================================================
-- Fee Periods Migration
-- Run on each existing tenant DB (ascent_group_N)
-- Adds: fee_periods table, payment_type + fee_period_id on fee_structures
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

-- 1. Create fee_periods table
CREATE TABLE fee_periods (
    fee_period_id   INT          NOT NULL IDENTITY(1,1),
    school_id       INT          NOT NULL,
    academic_year_id INT         NULL,
    month_no        TINYINT      NOT NULL,   -- 1=Jan .. 12=Dec
    year_no         SMALLINT     NOT NULL,   -- e.g. 2024
    period_label    VARCHAR(20)  NOT NULL,   -- e.g. "April 2024"
    sequence_no     INT          NULL,
    status          VARCHAR(10)  NOT NULL DEFAULT 'Active',
    created_by      VARCHAR(25)  NULL,
    created_at      DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_fee_periods               PRIMARY KEY (fee_period_id),
    CONSTRAINT FK_fee_periods_academic_year FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
);
GO

-- 2. Add payment_type and fee_period_id to fee_structures
ALTER TABLE fee_structures
    ADD payment_type   VARCHAR(10) NULL,   -- Term / Monthly
        fee_period_id  INT         NULL;
GO

-- 3. FK for fee_period_id on fee_structures
ALTER TABLE fee_structures
    ADD CONSTRAINT FK_fee_structures_fee_period
        FOREIGN KEY (fee_period_id) REFERENCES fee_periods(fee_period_id);
GO

-- 4. Add fee_period_id to fee_receipt_items
ALTER TABLE fee_receipt_items
    ADD fee_period_id INT NULL;
GO

ALTER TABLE fee_receipt_items
    ADD CONSTRAINT FK_fee_receipt_items_fee_period
        FOREIGN KEY (fee_period_id) REFERENCES fee_periods(fee_period_id);
GO
