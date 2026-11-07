-- ============================================================
-- Phase 146 — Easebuzz payment gateway
-- Adds gateway_configs.environment ('test' = sandbox, NULL = live).
-- Easebuzz has separate sandbox / live servers, so the school's
-- Settings → Payment Gateway now stores which one its keys belong to.
--
-- Run ONCE per tenant DB (ascent_group_{N}). Idempotent.
--
-- LOAD-BEARING: run this BEFORE deploying the Phase 146 API — the API
-- reads/writes gateway_configs.environment on every online payment.
-- ============================================================

IF COL_LENGTH('gateway_configs', 'environment') IS NULL
BEGIN
    ALTER TABLE gateway_configs ADD environment VARCHAR(10) NULL;   -- 'test' | NULL (= live)
    PRINT 'gateway_configs.environment added.';
END
ELSE
    PRINT 'gateway_configs.environment already exists — skipped.';
GO
