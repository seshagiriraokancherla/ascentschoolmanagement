-- ============================================================
-- Seed: Mobile App — Demo Parent Accounts
-- Run on: ascent_master
--
-- No cross-DB queries — fill in the student values below
-- by running this first on your TENANT DB:
--
--   SELECT student_id, student_name, admission_no FROM students
--   WHERE admission_no IN ('2021-IX-001','2021-IX-002','2021-IX-003')
--
--   SELECT class_id, class_name FROM classes WHERE class_name LIKE '%IX%'
--
-- Then fill in the ─── Configure here ─── section below.
-- Safe to re-run — IF NOT EXISTS guards throughout.
-- ============================================================

USE ascent_master;
GO

-- ─── Configure here ───────────────────────────────────────────────────────────
DECLARE @GroupId    INT          = 1              -- school_groups.group_id
DECLARE @DbName     VARCHAR(100) = 'EducareDemoDb' -- ascent_master.school_groups.db_name
DECLARE @SchoolId   INT          = 1              -- ascent_master.schools.school_id
DECLARE @MobilePin  VARCHAR(10)  = '1234'         -- PIN for all demo parent accounts

-- Fill these in from the tenant DB query above:
DECLARE @SAisha_Id    BIGINT       = 0            -- student_id for admission_no 2021-IX-001
DECLARE @SSurya_Id    BIGINT       = 0            -- student_id for admission_no 2021-IX-002
DECLARE @SMeghana_Id  BIGINT       = 0            -- student_id for admission_no 2021-IX-003

DECLARE @SAisha_Name    VARCHAR(105) = 'Aisha Begum'   -- student_name (verify from tenant DB)
DECLARE @SSurya_Name    VARCHAR(105) = 'Surya Prakash'
DECLARE @SMeghana_Name  VARCHAR(105) = 'Meghana Varma'

DECLARE @ClassName      VARCHAR(50)  = 'Class IX'      -- class_name from tenant DB classes table
-- ──────────────────────────────────────────────────────────────────────────────

-- Compute PIN hash (SHA-256 → hex → Base64, same as JwtHelper.HashRefreshToken)
DECLARE @PinHash VARCHAR(255)
DECLARE @PinHex  VARCHAR(64) = CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(@MobilePin AS VARCHAR)), 2)
SELECT  @PinHash = CAST(N'' AS XML).value(
    'xs:base64Binary(xs:hexBinary(sql:variable("@PinHex")))', 'VARCHAR(255)')

PRINT 'PIN hash: ' + @PinHash

-- ============================================================
-- 1. Parent Accounts  (3 demo parents)
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM parent_accounts WHERE mobile = '9900001111')
    INSERT INTO parent_accounts (full_name, mobile, pin_hash, is_active)
    VALUES ('Begum Sahib', '9900001111', @PinHash, 1)

IF NOT EXISTS (SELECT 1 FROM parent_accounts WHERE mobile = '9900002222')
    INSERT INTO parent_accounts (full_name, mobile, pin_hash, is_active)
    VALUES ('Prakash Rao', '9900002222', @PinHash, 1)

IF NOT EXISTS (SELECT 1 FROM parent_accounts WHERE mobile = '9900003333')
    INSERT INTO parent_accounts (full_name, mobile, pin_hash, is_active)
    VALUES ('Varma Garu', '9900003333', @PinHash, 1)

PRINT 'Parent accounts done.'

-- ============================================================
-- 2. Parent → Student Links  (parent_children)
-- ============================================================

DECLARE @PBegum   INT, @PPrakash INT, @PVarma INT
SELECT @PBegum   = parent_id FROM parent_accounts WHERE mobile = '9900001111'
SELECT @PPrakash = parent_id FROM parent_accounts WHERE mobile = '9900002222'
SELECT @PVarma   = parent_id FROM parent_accounts WHERE mobile = '9900003333'

-- Parent 1 (Begum Sahib) → Aisha Begum
IF @SAisha_Id > 0 AND NOT EXISTS (
    SELECT 1 FROM parent_children WHERE parent_id = @PBegum AND student_id = @SAisha_Id AND group_id = @GroupId)
    INSERT INTO parent_children (parent_id, student_id, group_id, db_name, school_id, student_name, class_name, admission_no, is_active)
    VALUES (@PBegum, @SAisha_Id, @GroupId, @DbName, @SchoolId, @SAisha_Name, @ClassName, '2021-IX-001', 1)

-- Parent 2 (Prakash Rao) → Surya Prakash
IF @SSurya_Id > 0 AND NOT EXISTS (
    SELECT 1 FROM parent_children WHERE parent_id = @PPrakash AND student_id = @SSurya_Id AND group_id = @GroupId)
    INSERT INTO parent_children (parent_id, student_id, group_id, db_name, school_id, student_name, class_name, admission_no, is_active)
    VALUES (@PPrakash, @SSurya_Id, @GroupId, @DbName, @SchoolId, @SSurya_Name, @ClassName, '2021-IX-002', 1)

-- Parent 3 (Varma Garu) → Meghana Varma
IF @SMeghana_Id > 0 AND NOT EXISTS (
    SELECT 1 FROM parent_children WHERE parent_id = @PVarma AND student_id = @SMeghana_Id AND group_id = @GroupId)
    INSERT INTO parent_children (parent_id, student_id, group_id, db_name, school_id, student_name, class_name, admission_no, is_active)
    VALUES (@PVarma, @SMeghana_Id, @GroupId, @DbName, @SchoolId, @SMeghana_Name, @ClassName, '2021-IX-003', 1)

PRINT 'Parent-child links done.'
GO


-- ============================================================
-- Summary
-- ============================================================
PRINT ''
PRINT '======================================='
PRINT ' Mobile parent seed loaded!'
PRINT '======================================='
PRINT ''
PRINT '--- Mobile App (Parent Login) ---'
PRINT 'All parent accounts PIN: 1234'
PRINT ''
PRINT 'Parent Name        Mobile        Child'
PRINT '-----------        ------        -----'
PRINT 'Begum Sahib        9900001111    Aisha Begum    (Class IX, 2021-IX-001)'
PRINT 'Prakash Rao        9900002222    Surya Prakash  (Class IX, 2021-IX-002)'
PRINT 'Varma Garu         9900003333    Meghana Varma  (Class IX, 2021-IX-003)'
GO
