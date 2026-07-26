-- ============================================================
-- Fee receipts: stamp payment_date + created_at in IST, not server-local time.
--
-- The production server runs in US Eastern time (UTC-4/-5) while the schools are
-- in India (UTC+5:30). The old column defaults used GETDATE() (server-local), so a
-- receipt whose date fell back to the default in the early IST morning was stamped
-- with the PREVIOUS calendar day. This replaces both defaults with an IST conversion
-- (SYSDATETIMEOFFSET() carries the server offset, so AT TIME ZONE is correct on any
-- server timezone). Requires SQL Server 2016+ (AT TIME ZONE).
--
-- Note: the fee-collection INSERT (FeeRepository.CollectFee) now sets both columns
-- explicitly in IST, so this default only matters for other/legacy insert paths and
-- as a safety net. Run on each existing tenant DB (ascent_group_N). Idempotent.
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

-- ── payment_date default → IST ──────────────────────────────
DECLARE @pdCon SYSNAME =
    (SELECT dc.name
       FROM sys.default_constraints dc
       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
      WHERE dc.parent_object_id = OBJECT_ID('fee_receipts') AND c.name = 'payment_date');

IF @pdCon IS NOT NULL
    EXEC('ALTER TABLE fee_receipts DROP CONSTRAINT ' + @pdCon);

ALTER TABLE fee_receipts
    ADD CONSTRAINT DF_fee_receipts_payment_date
    DEFAULT CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATE) FOR payment_date;
PRINT 'fee_receipts.payment_date default set to IST.';
GO

-- ── created_at default → IST ────────────────────────────────
DECLARE @caCon SYSNAME =
    (SELECT dc.name
       FROM sys.default_constraints dc
       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
      WHERE dc.parent_object_id = OBJECT_ID('fee_receipts') AND c.name = 'created_at');

IF @caCon IS NOT NULL
    EXEC('ALTER TABLE fee_receipts DROP CONSTRAINT ' + @caCon);

ALTER TABLE fee_receipts
    ADD CONSTRAINT DF_fee_receipts_created_at
    DEFAULT CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'India Standard Time' AS DATETIME) FOR created_at;
PRINT 'fee_receipts.created_at default set to IST.';
GO
