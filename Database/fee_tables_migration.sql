-- ============================================================
-- Migration: Add fee_receipts + fee_receipt_items
-- Run once against each existing ascent_group_{id} database.
-- Safe to run multiple times (IF NOT EXISTS checks).
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'fee_receipts')
BEGIN
    CREATE TABLE fee_receipts (
        receipt_id          INT             NOT NULL IDENTITY(1,1),
        receipt_no          VARCHAR(20)     NOT NULL,
        student_id          BIGINT          NOT NULL,
        academic_year_id    INT             NULL,
        payment_date        DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
        total_amount        DECIMAL(12,2)   NOT NULL DEFAULT 0,
        payment_mode_id     INT             NULL,
        cheque_no           VARCHAR(20)     NULL,
        cheque_date         DATE            NULL,
        bank_name           VARCHAR(50)     NULL,
        status              VARCHAR(10)     NOT NULL DEFAULT 'Active',
        cancelled_by        VARCHAR(50)     NULL,
        cancelled_at        DATETIME        NULL,
        cancel_reason       VARCHAR(100)    NULL,
        remarks             VARCHAR(100)    NULL,
        school_id           INT             NOT NULL,
        created_by          VARCHAR(25)     NULL,
        created_at          DATETIME        NOT NULL DEFAULT GETDATE(),
        machine_id          VARCHAR(25)     NULL,
        CONSTRAINT PK_fee_receipts                  PRIMARY KEY (receipt_id),
        CONSTRAINT FK_fee_receipts_student          FOREIGN KEY (student_id)       REFERENCES students(student_id),
        CONSTRAINT FK_fee_receipts_academic_year    FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
        CONSTRAINT FK_fee_receipts_payment_mode     FOREIGN KEY (payment_mode_id)  REFERENCES payment_modes(payment_mode_id)
    );
    PRINT 'Created fee_receipts';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'fee_receipt_items')
BEGIN
    CREATE TABLE fee_receipt_items (
        item_id             INT             NOT NULL IDENTITY(1,1),
        receipt_id          INT             NOT NULL,
        fee_type_id         INT             NULL,
        term_id             INT             NULL,
        amount              DECIMAL(12,2)   NOT NULL DEFAULT 0,
        concession_amount   DECIMAL(12,2)   NOT NULL DEFAULT 0,
        net_amount          DECIMAL(12,2)   NOT NULL DEFAULT 0,
        school_id           INT             NOT NULL,
        CONSTRAINT PK_fee_receipt_items             PRIMARY KEY (item_id),
        CONSTRAINT FK_fee_receipt_items_receipt     FOREIGN KEY (receipt_id)  REFERENCES fee_receipts(receipt_id),
        CONSTRAINT FK_fee_receipt_items_fee_type    FOREIGN KEY (fee_type_id) REFERENCES fee_types(fee_type_id),
        CONSTRAINT FK_fee_receipt_items_term        FOREIGN KEY (term_id)     REFERENCES terms(term_id)
    );
    PRINT 'Created fee_receipt_items';
END
GO
