 -- ============================================================
-- Seed: Classes + Sections for StAnnsAsf (school_id = 4)
-- Classes: LKG, UKG, Class 1 … Class 10  (12 classes)
-- Sections: A, B, C, D per class          (48 sections)
-- Run on: ascent_group_{N} (StAnnsAsf tenant DB)
-- ============================================================

-- ── Classes ──────────────────────────────────────────────────
 DECLARE @SchoolId    INT = 5
INSERT INTO classes (class_name, status, sequence_no, school_id) VALUES
('LKG',      'Active',  1,  @SchoolId),
('UKG',      'Active',  2,  @SchoolId),
('Class 1',  'Active',  3,  @SchoolId),
('Class 2',  'Active',  4,  @SchoolId),
('Class 3',  'Active',  5,  @SchoolId),
('Class 4',  'Active',  6,  @SchoolId),
('Class 5',  'Active',  7,  @SchoolId),
('Class 6',  'Active',  8,  @SchoolId),
('Class 7',  'Active',  9,  @SchoolId),
('Class 8',  'Active',  10, @SchoolId),
('Class 9',  'Active',  11, @SchoolId),
('Class 10', 'Active',  12, @SchoolId);
 

-- ── Sections (A–D for every class) ───────────────────────────
-- Uses subquery so class_id values don't need to be known in advance.

INSERT INTO sections (class_id, section_name, sequence_no, status, school_id)
SELECT c.class_id, s.section_name, s.seq, 'Active', @SchoolId
FROM classes c
CROSS JOIN (
    VALUES ('A', 1), ('B', 2), ('C', 3), ('D', 4)
) AS s(section_name, seq)
WHERE c.school_id = @SchoolId
  AND c.class_name IN (
      'LKG','UKG',
      'Class 1','Class 2','Class 3','Class 4','Class 5',
      'Class 6','Class 7','Class 8','Class 9','Class 10'
  );
 

----Seed fee categories----

DECLARE @AcadYearId  INT
SELECT  @AcadYearId = academic_year_id FROM academic_years WHERE academic_year='2026-2027' AND school_id=@SchoolId

IF NOT EXISTS (SELECT 1 FROM fee_categories WHERE category_name = 'General' AND school_id = @SchoolId AND academic_year_id = @AcadYearId)
    INSERT INTO fee_categories (category_name, academic_year_id, description, status, school_id, created_by)
    VALUES ('General', @AcadYearId, 'Regular students', 'Y', @SchoolId, 'demo')

IF NOT EXISTS (SELECT 1 FROM fee_categories WHERE category_name = 'Staff Child' AND school_id = @SchoolId AND academic_year_id = @AcadYearId)
    INSERT INTO fee_categories (category_name, academic_year_id, description, status, school_id, created_by)
    VALUES ('Staff Child', @AcadYearId, 'Wards of school staff', 'Y', @SchoolId, 'demo')

PRINT 'Fee categories done.'
 
 
 
IF NOT EXISTS (SELECT 1 FROM fee_types WHERE fee_type_name='Tuition Fee'    AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO fee_types(fee_type_name,academic_year_id,sequence_no,status,school_id,created_by) VALUES('Tuition Fee',@AcadYearId,1,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM fee_types WHERE fee_type_name='Development Fee' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO fee_types(fee_type_name,academic_year_id,sequence_no,status,school_id,created_by) VALUES('Development Fee',@AcadYearId,2,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM fee_types WHERE fee_type_name='Exam Fee'        AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO fee_types(fee_type_name,academic_year_id,sequence_no,status,school_id,created_by) VALUES('Exam Fee',@AcadYearId,3,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM fee_types WHERE fee_type_name='Computer Lab Fee' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO fee_types(fee_type_name,academic_year_id,sequence_no,status,school_id,created_by) VALUES('Computer Lab Fee',@AcadYearId,4,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM fee_types WHERE fee_type_name='Sports Fee'      AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO fee_types(fee_type_name,academic_year_id,sequence_no,status,school_id,created_by) VALUES('Sports Fee',@AcadYearId,5,'Y',@SchoolId,'demo')

 
IF NOT EXISTS (SELECT 1 FROM terms WHERE term_name='1st Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO terms(term_name,year_name,order_no,academic_year_id,status,school_id,created_by) VALUES('1st Quarter','2024-25',1,@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM terms WHERE term_name='2nd Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO terms(term_name,year_name,order_no,academic_year_id,status,school_id,created_by) VALUES('2nd Quarter','2024-25',2,@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM terms WHERE term_name='3rd Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO terms(term_name,year_name,order_no,academic_year_id,status,school_id,created_by) VALUES('3rd Quarter','2024-25',3,@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM terms WHERE term_name='4th Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO terms(term_name,year_name,order_no,academic_year_id,status,school_id,created_by) VALUES('4th Quarter','2024-25',4,@AcadYearId,'Y',@SchoolId,'demo')

PRINT 'Terms done.'
 

 
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='English'           AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('English','ENG','Language',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='Hindi'             AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('Hindi','HIN','Language',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='Mathematics'       AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('Mathematics','MATH','Theory',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='Science'           AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('Science','SCI','Theory',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='Social Studies'    AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('Social Studies','SST','Theory',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='Telugu'            AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('Telugu','TEL','Language',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='Computer Science'  AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('Computer Science','CS','Theory',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='General Knowledge' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('General Knowledge','GK','General',@AcadYearId,'Y',@SchoolId,'demo')

PRINT 'Subjects done.'
 
 
IF NOT EXISTS (SELECT 1 FROM payment_modes WHERE mode_name = 'Cash')
    INSERT INTO payment_modes (mode_name, school_id, is_online) VALUES ('Cash', @SchoolId, 0)

IF NOT EXISTS (SELECT 1 FROM payment_modes WHERE mode_name = 'Cheque')
    INSERT INTO payment_modes (mode_name, school_id, is_online) VALUES ('Cheque', @SchoolId, 0)

IF NOT EXISTS (SELECT 1 FROM payment_modes WHERE mode_name = 'Online')
    INSERT INTO payment_modes (mode_name, school_id, is_online) VALUES ('Online', @SchoolId, 1)


    INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Principal'
  AND r.school_id IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  )

-- Admin Clerk: student profile + fee view/collect + master data implied
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Admin Clerk'
  AND r.school_id IS NULL
  AND p.permission_code IN (
      'STUDENT_PROFILE.VIEW', 'STUDENT_PROFILE.CREATE', 'STUDENT_PROFILE.EDIT',
      'STUDENT_FEE.VIEW',
      'ATTENDANCE.VIEW',
      'MARKS.VIEW',
      'TRANSPORT.VIEW'
  )
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  )

-- Fee Clerk: student profile view + all fee permissions
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Fee Clerk'
  AND r.school_id IS NULL
  AND p.permission_code IN (
      'STUDENT_PROFILE.VIEW',
      'STUDENT_FEE.VIEW', 'STUDENT_FEE.COLLECT', 'STUDENT_FEE.CANCEL_RECEIPT', 'STUDENT_FEE.CONCESSION'
  )
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  )

-- Class Teacher: attendance + marks + student view
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Class Teacher'
  AND r.school_id IS NULL
  AND p.permission_code IN (
      'STUDENT_PROFILE.VIEW',
      'STUDENT_FEE.VIEW',
      'ATTENDANCE.VIEW', 'ATTENDANCE.MARK', 'ATTENDANCE.EDIT',
      'MARKS.VIEW', 'MARKS.ENTER', 'MARKS.EDIT'
  )
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  )

-- Librarian: student view + library all
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Librarian'
  AND r.school_id IS NULL
  AND p.permission_code IN (
      'STUDENT_PROFILE.VIEW',
      'LIBRARY.VIEW', 'LIBRARY.ISSUE', 'LIBRARY.RETURN', 'LIBRARY.MANAGE'
  )
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  )

PRINT 'Role-permission mappings seeded.'
 
-- ── Verify ───────────────────────────────────────────────────

SELECT c.class_name, s.section_name
FROM sections s
JOIN classes c ON c.class_id = s.class_id
WHERE s.school_id = @SchoolId
ORDER BY c.sequence_no, s.sequence_no;
 

IF NOT EXISTS (SELECT 1 FROM academic_years WHERE academic_year = '2026-2027' AND school_id = @SchoolId)
BEGIN
    INSERT INTO academic_years (academic_year, start_month, end_month, status, school_id, created_by)
    VALUES ('2026-2027', 'June', 'March', 'Active', @SchoolId, 'admin')
END
GO
 