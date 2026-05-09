-- ============================================================
-- Seed: Mobile Fee Test Data — Class IX Students
-- Run on: ascent_group_{N}  (tenant DB, after demo_data.sql)
--
-- Adds fee structures for Class IX and partial receipts so
-- the mobile Fees screen shows a realistic paid/pending list.
--
-- After running this script you can test with:
--   Student login  : 2021-IX-001 / 1234  → Aisha   (Q1 paid, Q2–Q4 pending)
--   Student login  : 2021-IX-002 / 1234  → Surya   (all pending)
--   Student login  : 2021-IX-003 / 1234  → Meghana (Q1+Q2 paid, Q3–Q4 pending)
--   Parent login   : 9900001111 / 1234   → child: Aisha
--   Parent login   : 9900002222 / 1234   → child: Surya
--   Parent login   : 9900003333 / 1234   → child: Meghana
--
-- Safe to re-run — IF NOT EXISTS guards throughout.
-- ============================================================

-- ─── Configure here ───────────────────────────────────────────
DECLARE @SchoolId   INT = 1     -- ← must match your school_id
-- ─────────────────────────────────────────────────────────────

DECLARE @AcadYearId   INT
DECLARE @ClsIX        INT
DECLARE @CatGeneral   INT
DECLARE @FtTuition    INT
DECLARE @FtDevelopment INT
DECLARE @FtExam       INT
DECLARE @T1 INT, @T2 INT, @T3 INT, @T4 INT
DECLARE @CashModeId   INT
DECLARE @SAisha   BIGINT
DECLARE @SSurya   BIGINT
DECLARE @SMeghana BIGINT

SELECT @AcadYearId    = academic_year_id  FROM academic_years  WHERE academic_year  = '2024-25'       AND school_id = @SchoolId
SELECT @ClsIX         = class_id          FROM classes          WHERE class_name     = 'Class IX'      AND school_id = @SchoolId
SELECT @CatGeneral    = fee_category_id   FROM fee_categories   WHERE category_name  = 'General'       AND school_id = @SchoolId AND academic_year_id = @AcadYearId
SELECT @FtTuition     = fee_type_id       FROM fee_types        WHERE fee_type_name  = 'Tuition Fee'    AND school_id = @SchoolId AND academic_year_id = @AcadYearId
SELECT @FtDevelopment = fee_type_id       FROM fee_types        WHERE fee_type_name  = 'Development Fee'AND school_id = @SchoolId AND academic_year_id = @AcadYearId
SELECT @FtExam        = fee_type_id       FROM fee_types        WHERE fee_type_name  = 'Exam Fee'       AND school_id = @SchoolId AND academic_year_id = @AcadYearId
SELECT @T1            = term_id           FROM terms            WHERE term_name      = '1st Quarter'   AND school_id = @SchoolId AND academic_year_id = @AcadYearId
SELECT @T2            = term_id           FROM terms            WHERE term_name      = '2nd Quarter'   AND school_id = @SchoolId AND academic_year_id = @AcadYearId
SELECT @T3            = term_id           FROM terms            WHERE term_name      = '3rd Quarter'   AND school_id = @SchoolId AND academic_year_id = @AcadYearId
SELECT @T4            = term_id           FROM terms            WHERE term_name      = '4th Quarter'   AND school_id = @SchoolId AND academic_year_id = @AcadYearId
SELECT @CashModeId    = payment_mode_id   FROM payment_modes    WHERE mode_name      = 'Cash'          AND school_id IS NULL
SELECT @SAisha        = student_id        FROM students         WHERE admission_no   = '2021-IX-001'   AND school_id = @SchoolId
SELECT @SSurya        = student_id        FROM students         WHERE admission_no   = '2021-IX-002'   AND school_id = @SchoolId
SELECT @SMeghana      = student_id        FROM students         WHERE admission_no   = '2021-IX-003'   AND school_id = @SchoolId

-- Validate key lookups
IF @AcadYearId IS NULL   BEGIN PRINT 'ERROR: Academic year 2024-25 not found. Run demo_data.sql first.' RETURN END
IF @ClsIX IS NULL        BEGIN PRINT 'ERROR: Class IX not found. Run demo_data.sql first.' RETURN END
IF @CatGeneral IS NULL   BEGIN PRINT 'ERROR: General fee category not found.' RETURN END
IF @FtTuition IS NULL    BEGIN PRINT 'ERROR: Tuition Fee type not found.' RETURN END
IF @T1 IS NULL           BEGIN PRINT 'ERROR: 1st Quarter term not found.' RETURN END
IF @SAisha IS NULL       BEGIN PRINT 'ERROR: Student 2021-IX-001 (Aisha) not found.' RETURN END
IF @SSurya IS NULL       BEGIN PRINT 'ERROR: Student 2021-IX-002 (Surya) not found.' RETURN END
IF @SMeghana IS NULL     BEGIN PRINT 'ERROR: Student 2021-IX-003 (Meghana) not found.' RETURN END

PRINT 'Lookup IDs resolved successfully.'
PRINT '  AcadYearId=' + CAST(@AcadYearId AS VARCHAR) + '  ClassIX=' + CAST(@ClsIX AS VARCHAR)
PRINT '  Aisha=' + CAST(@SAisha AS VARCHAR) + '  Surya=' + CAST(@SSurya AS VARCHAR) + '  Meghana=' + CAST(@SMeghana AS VARCHAR)
GO


-- ============================================================
-- 1. Fee Structures for Class IX — General category
--    Tuition: ₹4500/quarter | Development: ₹900/quarter | Exam: ₹400/quarter
--    Total per quarter: ₹5800  |  Full year: ₹23,200
-- ============================================================
DECLARE @SchoolId     INT = 1
DECLARE @AcadYearId   INT
DECLARE @ClsIX        INT
DECLARE @CatGeneral   INT
DECLARE @FtTuition    INT
DECLARE @FtDevelopment INT
DECLARE @FtExam       INT
DECLARE @T1 INT, @T2 INT, @T3 INT, @T4 INT

SELECT @AcadYearId    = academic_year_id  FROM academic_years  WHERE academic_year  = '2024-25'        AND school_id = @SchoolId
SELECT @ClsIX         = class_id          FROM classes          WHERE class_name     = 'Class IX'       AND school_id = @SchoolId
SELECT @CatGeneral    = fee_category_id   FROM fee_categories   WHERE category_name  = 'General'        AND school_id = @SchoolId AND academic_year_id = @AcadYearId
SELECT @FtTuition     = fee_type_id       FROM fee_types        WHERE fee_type_name  = 'Tuition Fee'    AND school_id = @SchoolId AND academic_year_id = @AcadYearId
SELECT @FtDevelopment = fee_type_id       FROM fee_types        WHERE fee_type_name  = 'Development Fee'AND school_id = @SchoolId AND academic_year_id = @AcadYearId
SELECT @FtExam        = fee_type_id       FROM fee_types        WHERE fee_type_name  = 'Exam Fee'       AND school_id = @SchoolId AND academic_year_id = @AcadYearId
SELECT @T1 = term_id FROM terms WHERE term_name='1st Quarter' AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @T2 = term_id FROM terms WHERE term_name='2nd Quarter' AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @T3 = term_id FROM terms WHERE term_name='3rd Quarter' AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @T4 = term_id FROM terms WHERE term_name='4th Quarter' AND school_id=@SchoolId AND academic_year_id=@AcadYearId

IF NOT EXISTS (SELECT 1 FROM fee_structures WHERE class_id=@ClsIX AND fee_category_id=@CatGeneral AND fee_type_id=@FtTuition AND term_id=@T1 AND academic_year_id=@AcadYearId)
BEGIN
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsIX,@FtTuition,    @T1,4500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsIX,@FtTuition,    @T2,4500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsIX,@FtTuition,    @T3,4500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsIX,@FtTuition,    @T4,4500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsIX,@FtDevelopment,@T1, 900,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsIX,@FtDevelopment,@T2, 900,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsIX,@FtDevelopment,@T3, 900,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsIX,@FtDevelopment,@T4, 900,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsIX,@FtExam,       @T1, 400,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsIX,@FtExam,       @T2, 400,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsIX,@FtExam,       @T3, 400,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsIX,@FtExam,       @T4, 400,@AcadYearId,'Y',@SchoolId,'demo')
    PRINT 'Class IX fee structures inserted (₹5800/quarter × 4 = ₹23,200/year).'
END
ELSE
    PRINT 'Class IX fee structures already exist — skipped.'
GO


-- ============================================================
-- 2. Fee Receipts — Aisha (2021-IX-001)
--    Q1 paid in full (₹5800). Q2, Q3, Q4 outstanding.
-- ============================================================
DECLARE @SchoolId   INT = 1
DECLARE @AcadYearId INT
DECLARE @T1 INT, @FtTuition INT, @FtDevelopment INT, @FtExam INT
DECLARE @CashModeId INT
DECLARE @SAisha     BIGINT

SELECT @AcadYearId    = academic_year_id FROM academic_years WHERE academic_year='2024-25' AND school_id=@SchoolId
SELECT @T1            = term_id FROM terms WHERE term_name='1st Quarter' AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @FtTuition     = fee_type_id FROM fee_types WHERE fee_type_name='Tuition Fee'     AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @FtDevelopment = fee_type_id FROM fee_types WHERE fee_type_name='Development Fee' AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @FtExam        = fee_type_id FROM fee_types WHERE fee_type_name='Exam Fee'        AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @CashModeId    = payment_mode_id FROM payment_modes WHERE mode_name='Cash' AND school_id IS NULL
SELECT @SAisha        = student_id FROM students WHERE admission_no='2021-IX-001' AND school_id=@SchoolId

IF NOT EXISTS (SELECT 1 FROM fee_receipts WHERE student_id=@SAisha AND academic_year_id=@AcadYearId AND status='Active')
BEGIN
    DECLARE @Rid_A1 INT
    INSERT INTO fee_receipts(receipt_no,student_id,academic_year_id,payment_date,total_amount,payment_mode_id,status,school_id,created_by)
    VALUES('TMP',@SAisha,@AcadYearId,'2024-06-10',5800,@CashModeId,'Active',@SchoolId,'admin')
    SET @Rid_A1 = SCOPE_IDENTITY()
    UPDATE fee_receipts SET receipt_no = '2024-' + RIGHT('00000'+CAST(@Rid_A1 AS VARCHAR),5) WHERE receipt_id=@Rid_A1
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid_A1,@FtTuition,    @T1,4500,0,4500,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid_A1,@FtDevelopment,@T1, 900,0, 900,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid_A1,@FtExam,       @T1, 400,0, 400,@SchoolId)
    PRINT 'Aisha — Q1 receipt created (₹5,800 paid). Q2/Q3/Q4 pending.'
END
ELSE
    PRINT 'Aisha — receipt already exists, skipped.'
GO


-- ============================================================
-- 3. Fee Receipts — Meghana (2021-IX-003)
--    Q1 + Q2 paid. Q3, Q4 outstanding.
-- ============================================================
DECLARE @SchoolId   INT = 1
DECLARE @AcadYearId INT
DECLARE @T1 INT, @T2 INT
DECLARE @FtTuition INT, @FtDevelopment INT, @FtExam INT
DECLARE @CashModeId INT
DECLARE @SMeghana   BIGINT

SELECT @AcadYearId    = academic_year_id FROM academic_years WHERE academic_year='2024-25' AND school_id=@SchoolId
SELECT @T1            = term_id FROM terms WHERE term_name='1st Quarter' AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @T2            = term_id FROM terms WHERE term_name='2nd Quarter' AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @FtTuition     = fee_type_id FROM fee_types WHERE fee_type_name='Tuition Fee'     AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @FtDevelopment = fee_type_id FROM fee_types WHERE fee_type_name='Development Fee' AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @FtExam        = fee_type_id FROM fee_types WHERE fee_type_name='Exam Fee'        AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @CashModeId    = payment_mode_id FROM payment_modes WHERE mode_name='Cash' AND school_id IS NULL
SELECT @SMeghana      = student_id FROM students WHERE admission_no='2021-IX-003' AND school_id=@SchoolId

IF NOT EXISTS (SELECT 1 FROM fee_receipts WHERE student_id=@SMeghana AND academic_year_id=@AcadYearId AND status='Active')
BEGIN
    -- Q1
    DECLARE @Rid_M1 INT
    INSERT INTO fee_receipts(receipt_no,student_id,academic_year_id,payment_date,total_amount,payment_mode_id,status,school_id,created_by)
    VALUES('TMP',@SMeghana,@AcadYearId,'2024-06-12',5800,@CashModeId,'Active',@SchoolId,'admin')
    SET @Rid_M1 = SCOPE_IDENTITY()
    UPDATE fee_receipts SET receipt_no = '2024-' + RIGHT('00000'+CAST(@Rid_M1 AS VARCHAR),5) WHERE receipt_id=@Rid_M1
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid_M1,@FtTuition,    @T1,4500,0,4500,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid_M1,@FtDevelopment,@T1, 900,0, 900,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid_M1,@FtExam,       @T1, 400,0, 400,@SchoolId)
    -- Q2
    DECLARE @Rid_M2 INT
    INSERT INTO fee_receipts(receipt_no,student_id,academic_year_id,payment_date,total_amount,payment_mode_id,status,school_id,created_by)
    VALUES('TMP',@SMeghana,@AcadYearId,'2024-09-05',5800,@CashModeId,'Active',@SchoolId,'admin')
    SET @Rid_M2 = SCOPE_IDENTITY()
    UPDATE fee_receipts SET receipt_no = '2024-' + RIGHT('00000'+CAST(@Rid_M2 AS VARCHAR),5) WHERE receipt_id=@Rid_M2
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid_M2,@FtTuition,    @T2,4500,0,4500,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid_M2,@FtDevelopment,@T2, 900,0, 900,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid_M2,@FtExam,       @T2, 400,0, 400,@SchoolId)
    PRINT 'Meghana — Q1+Q2 receipts created (₹11,600 paid). Q3/Q4 pending.'
END
ELSE
    PRINT 'Meghana — receipts already exist, skipped.'
GO
-- Note: Surya (2021-IX-002) intentionally has no receipts → all 4 quarters pending (₹23,200 outstanding)


-- ============================================================
-- Summary
-- ============================================================
PRINT ''
PRINT '================================================='
PRINT ' Mobile fee test data loaded!'
PRINT '================================================='
PRINT ''
PRINT 'Class IX fee structure: ₹5,800/quarter (₹23,200/year)'
PRINT '  Tuition:     ₹4,500 | Development: ₹900 | Exam: ₹400'
PRINT ''
PRINT 'Student         Login           Paid    Outstanding'
PRINT '----------      --------        ------  -----------'
PRINT 'Aisha Begum     2021-IX-001     Q1      Q2, Q3, Q4  (₹17,400 due)'
PRINT 'Surya Prakash   2021-IX-002     —       All 4 qtrs  (₹23,200 due)'
PRINT 'Meghana Varma   2021-IX-003     Q1,Q2   Q3, Q4      (₹11,600 due)'
PRINT ''
PRINT 'Mobile PIN for all students: 1234'
PRINT 'Parent accounts (login via mobile number, PIN 1234):'
PRINT '  9900001111  → Aisha   |  9900002222 → Surya  |  9900003333 → Meghana'
PRINT ''
PRINT 'Also available (have mobile accounts + fee data from demo_data.sql):'
PRINT '  Vikram Reddy    2022-VIII-001  Q1,Q2 paid → Q3,Q4 pending'
PRINT '  Harini Rao      2020-X-001     Q1 paid    → Q2,Q3,Q4 pending'
GO
