-- ============================================================
-- Receipt payment-date change log (Fees → Receipts → "Date" button).
-- One row per change: old date, new date, reason, who, when (IST).
--
-- Run ONCE per tenant DB (ascent_group_{N}). Idempotent.
--
-- LOAD-BEARING for the new feature only: until this runs, "Date" on a receipt
-- and the drawer's date-change history fail. Everything else is unaffected.
-- ============================================================

IF OBJECT_ID('fee_receipt_date_changes', 'U') IS NULL
BEGIN
    CREATE TABLE fee_receipt_date_changes (
        change_id           INT           NOT NULL IDENTITY(1,1),
        receipt_id          INT           NOT NULL,
        school_id           INT           NOT NULL,
        old_payment_date    DATE          NOT NULL,
        new_payment_date    DATE          NOT NULL,
        reason              VARCHAR(200)  NOT NULL,
        changed_by_user_id  INT           NULL,          -- users.user_id (NULL for the VB6 API-key integration)
        changed_by_name     VARCHAR(100)  NOT NULL,      -- display snapshot, survives a user leaving
        changed_at          DATETIME      NOT NULL DEFAULT CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME),  -- IST
        CONSTRAINT PK_fee_receipt_date_changes PRIMARY KEY (change_id),
        CONSTRAINT FK_frdc_receipts            FOREIGN KEY (receipt_id) REFERENCES fee_receipts(receipt_id)
    );
    CREATE INDEX IX_frdc_receipt ON fee_receipt_date_changes (receipt_id);
    PRINT 'fee_receipt_date_changes created.';
END
ELSE
    PRINT 'fee_receipt_date_changes already exists — skipped.';
GO
