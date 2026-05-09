DECLARE @SchoolId       INT = 4   -- ← set your school_id here
DECLARE @AcademicYearId INT = 1  -- ← 0 = auto-pick latest, or set explicitly

-- Auto-pick latest active academic year if not set
IF @AcademicYearId = 0
BEGIN
    SELECT TOP 1 @AcademicYearId = academic_year_id
    FROM   academic_years
    WHERE  school_id = @SchoolId
    ORDER  BY academic_year_id DESC
END

IF @AcademicYearId = 0
BEGIN
    RAISERROR('No academic year found for school_id %d. Create one first.', 16, 1, @SchoolId)
    RETURN
END

PRINT 'Using AcademicYearId = ' + CAST(@AcademicYearId AS VARCHAR)

-- ── 1. Insert Test CLASS (skip if already exists) ─────────────────────────────

DECLARE @ClassId INT

SELECT @ClassId = class_id
FROM   classes
WHERE  class_name = 'Test CLASS'
  AND  school_id  = @SchoolId

IF @ClassId IS NULL
BEGIN
    INSERT INTO classes (class_name, status, school_id, created_by, created_at)
    VALUES ('Test CLASS', 'Active', @SchoolId, 'seed_script', GETDATE())

    SET @ClassId = SCOPE_IDENTITY()
    PRINT 'Created Test CLASS with class_id = ' + CAST(@ClassId AS VARCHAR)
END
ELSE
    PRINT 'Test CLASS already exists with class_id = ' + CAST(@ClassId AS VARCHAR)

-- ── 2. Insert Section A for Test CLASS (skip if already exists) ───────────────

DECLARE @SectionId INT

SELECT @SectionId = section_id
FROM   sections
WHERE  class_id     = @ClassId
  AND  section_name = 'A'
  AND  school_id    = @SchoolId

IF @SectionId IS NULL
BEGIN
    INSERT INTO sections (class_id, section_name, status, school_id, created_by, created_at)
    VALUES (@ClassId, 'A', 'Active', @SchoolId, 'seed_script', GETDATE())

    SET @SectionId = SCOPE_IDENTITY()
    PRINT 'Created Section A with section_id = ' + CAST(@SectionId AS VARCHAR)
END
ELSE
    PRINT 'Section A already exists with section_id = ' + CAST(@SectionId AS VARCHAR)

-- ── 3. Insert 12 test students ────────────────────────────────────────────────
-- Skips any student whose admission_no already exists for this school + year.

DECLARE @Students TABLE (
    student_name  VARCHAR(50),
    admission_no  VARCHAR(20),
    father_mobile VARCHAR(20)
)

INSERT INTO @Students VALUES
    ('Test1',  'TEST001', '7660805459'),
    ('Test2',  'TEST002', '9392521213'),
    ('Test3',  'TEST003', '9533212322'),
    ('Test4',  'TEST004', '9392123644'),
    ('Test5',  'TEST005', '9652376688'),
    ('Test6',  'TEST006', '8333956039'),
    ('Test7',  'TEST007', '9912123644'),
    ('Test8',  'TEST008', '9491553644'),
    ('Test9',  'TEST009', '9985118786'),
    ('Test10', 'TEST010', '7981999407'),
     ('Test11', 'TEST011', '8500206173'),
    ('Test12', 'TEST012', '9160659510'),
    -- Google Play reviewer account — matches ReviewMobile constant in MobileAuthController.cs
    -- OTP is fixed (123456) — no SMS sent; used in Play Console App Access section
    ('Review Student', 'TESTREVIEW', '9999999999')

INSERT INTO students (
    student_name,
    admission_no,
    father_mobile,
    class_id,
    section_id,
    section,
    academic_year_id,
    school_id,
    status,
    created_by,
    created_at
)
SELECT
    s.student_name,
    s.admission_no,
    s.father_mobile,
    @ClassId,
    @SectionId,
    'A',
    @AcademicYearId,
    @SchoolId,
    'Active',
    'seed_script',
    GETDATE()
FROM @Students s
WHERE NOT EXISTS (
    SELECT 1
    FROM   students x
    WHERE  x.admission_no      = s.admission_no
      AND  x.school_id         = @SchoolId
      AND  x.academic_year_id  = @AcademicYearId
)

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' student(s) inserted.'

-- ── 4. Review teacher user (Google Play staff login bypass) ──────────────────
-- Username : reviewteacher
-- Password : review123
-- Matches ReviewTeacherUsername / ReviewTeacherPassword constants in MobileTeacherAuthController.cs
-- The bypass skips the password hash check, so any hash stored here is safe.
-- We still store the real hash so the account works for normal login too.

DECLARE @ReviewPwd       VARCHAR(100)  = 'review123'
DECLARE @ReviewHashBytes VARBINARY(32) = HASHBYTES('SHA2_256', CAST(@ReviewPwd AS VARCHAR(100)))
DECLARE @ReviewHexStr    VARCHAR(64)   = CONVERT(VARCHAR(64), @ReviewHashBytes, 2)
DECLARE @ReviewPwdHash   VARCHAR(255)
SELECT  @ReviewPwdHash   = CAST(N'' AS XML).value(
    'xs:base64Binary(xs:hexBinary(sql:variable("@ReviewHexStr")))', 'VARCHAR(255)')

IF NOT EXISTS (
    SELECT 1 FROM users
    WHERE  username  = 'reviewteacher'
      AND  school_id = @SchoolId
)
BEGIN
    INSERT INTO users (school_id, username, password_hash, full_name, status, created_by)
    VALUES (@SchoolId, 'reviewteacher', @ReviewPwdHash, 'Review Teacher', 'Active', 'seed_script')

    PRINT 'Created review teacher user (username: reviewteacher, password: review123)'
END
ELSE
    PRINT 'Review teacher user already exists.'

-- ── Summary ───────────────────────────────────────────────────────────────────

SELECT
    s.student_id,
    s.admission_no,
    s.student_name,
    s.father_mobile,
    c.class_name,
    sec.section_name,
    ay.academic_year
FROM   students       s
JOIN   classes        c   ON c.class_id        = s.class_id
JOIN   sections       sec ON sec.section_id    = s.section_id
JOIN   academic_years ay  ON ay.academic_year_id = s.academic_year_id
WHERE  s.school_id      = @SchoolId
  AND  s.class_id       = @ClassId
  AND  s.section_id     = @SectionId
  AND  s.academic_year_id = @AcademicYearId
ORDER  BY s.admission_no