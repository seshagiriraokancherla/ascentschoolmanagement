-- ============================================================
-- Demo Data — Ascent Schools Application
-- Run on: ascent_group_{N}  (after tenant_tables.sql + seed_tenant_data.sql)
--
-- Populates realistic data for UI and mobile app demos:
--   1.  Academic year
--   2.  Class groups
--   3.  Fee categories
--   4.  Classes (KG – Class X)
--   5.  Fee types
--   6.  Terms (4 per year)
--   7.  Subjects
--   8.  Students (15 students across 5 classes)
--   9.  Fee structures
--   10. Fee receipts (some paid, some outstanding)
--   11. Exam types
--   12. Student marks (Unit Test 1 + Midterm for all students)
--   13. Student attendance (Jan–Mar 2025)
--   14. Homework (6 recent assignments)
--   15. Announcements (school-wide + class-specific)
--   16. Student mobile accounts (mobile login, PIN = 1234)
--
-- Safe to re-run — uses IF NOT EXISTS checks throughout.
-- ============================================================

-- ─── Configure here ──────────────────────────────────────────
DECLARE @SchoolId   INT = 1       -- ← must match your school_id from ascent_master.schools
DECLARE @MobilePin  VARCHAR(10)   = '1234'   -- PIN for all demo mobile accounts
-- ─────────────────────────────────────────────────────────────

-- Compute PIN hash (SHA-256 + Base64, same as JwtHelper.HashRefreshToken)
DECLARE @PinHash VARCHAR(255)
DECLARE @PinHex  VARCHAR(64) = CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST(@MobilePin AS VARCHAR)), 2)
SELECT  @PinHash = CAST(N'' AS XML).value(
    'xs:base64Binary(xs:hexBinary(sql:variable("@PinHex")))', 'VARCHAR(255)')

PRINT 'Demo data for SchoolId: ' + CAST(@SchoolId AS VARCHAR)
PRINT 'Mobile PIN hash: ' + @PinHash


-- ============================================================
-- 1. Academic Year
-- ============================================================
DECLARE @AcadYearId INT

IF NOT EXISTS (SELECT 1 FROM academic_years WHERE academic_year = '2024-25' AND school_id = @SchoolId)
BEGIN
    INSERT INTO academic_years (academic_year, start_month, end_month, status, school_id, created_by)
    VALUES ('2024-25', 'June', 'March', 'Y', @SchoolId, 'demo')
END

SELECT @AcadYearId = academic_year_id
FROM   academic_years WHERE academic_year = '2024-25' AND school_id = @SchoolId

PRINT 'Academic year: ' + CAST(@AcadYearId AS VARCHAR)
GO


-- ============================================================
-- 2. Class Groups
-- ============================================================
DECLARE @SchoolId INT = 1

IF NOT EXISTS (SELECT 1 FROM class_groups WHERE group_name = 'Pre-Primary' AND school_id = @SchoolId)
    INSERT INTO class_groups (group_name, description, status, school_id, created_by)
    VALUES ('Pre-Primary', 'LKG and UKG', 'Y', @SchoolId, 'demo')

IF NOT EXISTS (SELECT 1 FROM class_groups WHERE group_name = 'Primary' AND school_id = @SchoolId)
    INSERT INTO class_groups (group_name, description, status, school_id, created_by)
    VALUES ('Primary', 'Class I to V', 'Y', @SchoolId, 'demo')

IF NOT EXISTS (SELECT 1 FROM class_groups WHERE group_name = 'Middle School' AND school_id = @SchoolId)
    INSERT INTO class_groups (group_name, description, status, school_id, created_by)
    VALUES ('Middle School', 'Class VI to VIII', 'Y', @SchoolId, 'demo')

IF NOT EXISTS (SELECT 1 FROM class_groups WHERE group_name = 'High School' AND school_id = @SchoolId)
    INSERT INTO class_groups (group_name, description, status, school_id, created_by)
    VALUES ('High School', 'Class IX to X', 'Y', @SchoolId, 'demo')

PRINT 'Class groups done.'
GO


-- ============================================================
-- 3. Fee Categories
-- ============================================================
DECLARE @SchoolId    INT = 1
DECLARE @AcadYearId  INT
SELECT  @AcadYearId = academic_year_id FROM academic_years WHERE academic_year='2024-25' AND school_id=@SchoolId

IF NOT EXISTS (SELECT 1 FROM fee_categories WHERE category_name = 'General' AND school_id = @SchoolId AND academic_year_id = @AcadYearId)
    INSERT INTO fee_categories (category_name, academic_year_id, description, status, school_id, created_by)
    VALUES ('General', @AcadYearId, 'Regular students', 'Y', @SchoolId, 'demo')

IF NOT EXISTS (SELECT 1 FROM fee_categories WHERE category_name = 'Staff Child' AND school_id = @SchoolId AND academic_year_id = @AcadYearId)
    INSERT INTO fee_categories (category_name, academic_year_id, description, status, school_id, created_by)
    VALUES ('Staff Child', @AcadYearId, 'Wards of school staff', 'Y', @SchoolId, 'demo')

PRINT 'Fee categories done.'
GO


-- ============================================================
-- 4. Classes
-- ============================================================
DECLARE @SchoolId     INT = 1
DECLARE @AcadYearId   INT
DECLARE @GrpPrePri    INT, @GrpPrimary INT, @GrpMiddle INT, @GrpHigh INT
DECLARE @CatGeneral   INT

SELECT @AcadYearId = academic_year_id FROM academic_years WHERE academic_year='2024-25' AND school_id=@SchoolId
SELECT @GrpPrePri  = class_group_id FROM class_groups WHERE group_name='Pre-Primary'  AND school_id=@SchoolId
SELECT @GrpPrimary = class_group_id FROM class_groups WHERE group_name='Primary'      AND school_id=@SchoolId
SELECT @GrpMiddle  = class_group_id FROM class_groups WHERE group_name='Middle School' AND school_id=@SchoolId
SELECT @GrpHigh    = class_group_id FROM class_groups WHERE group_name='High School'  AND school_id=@SchoolId
SELECT @CatGeneral = fee_category_id FROM fee_categories WHERE category_name='General' AND school_id=@SchoolId AND academic_year_id=@AcadYearId

IF NOT EXISTS (SELECT 1 FROM classes WHERE class_name='LKG'       AND school_id=@SchoolId) INSERT INTO classes(class_name,class_group_id,fee_category_id,sequence_no,status,school_id,created_by) VALUES('LKG',@GrpPrePri,@CatGeneral,1,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM classes WHERE class_name='UKG'       AND school_id=@SchoolId) INSERT INTO classes(class_name,class_group_id,fee_category_id,sequence_no,status,school_id,created_by) VALUES('UKG',@GrpPrePri,@CatGeneral,2,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM classes WHERE class_name='Class I'   AND school_id=@SchoolId) INSERT INTO classes(class_name,class_group_id,fee_category_id,sequence_no,status,school_id,created_by) VALUES('Class I',@GrpPrimary,@CatGeneral,3,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM classes WHERE class_name='Class II'  AND school_id=@SchoolId) INSERT INTO classes(class_name,class_group_id,fee_category_id,sequence_no,status,school_id,created_by) VALUES('Class II',@GrpPrimary,@CatGeneral,4,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM classes WHERE class_name='Class III' AND school_id=@SchoolId) INSERT INTO classes(class_name,class_group_id,fee_category_id,sequence_no,status,school_id,created_by) VALUES('Class III',@GrpPrimary,@CatGeneral,5,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM classes WHERE class_name='Class IV'  AND school_id=@SchoolId) INSERT INTO classes(class_name,class_group_id,fee_category_id,sequence_no,status,school_id,created_by) VALUES('Class IV',@GrpPrimary,@CatGeneral,6,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM classes WHERE class_name='Class V'   AND school_id=@SchoolId) INSERT INTO classes(class_name,class_group_id,fee_category_id,sequence_no,status,school_id,created_by) VALUES('Class V',@GrpPrimary,@CatGeneral,7,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM classes WHERE class_name='Class VI'  AND school_id=@SchoolId) INSERT INTO classes(class_name,class_group_id,fee_category_id,sequence_no,status,school_id,created_by) VALUES('Class VI',@GrpMiddle,@CatGeneral,8,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM classes WHERE class_name='Class VII' AND school_id=@SchoolId) INSERT INTO classes(class_name,class_group_id,fee_category_id,sequence_no,status,school_id,created_by) VALUES('Class VII',@GrpMiddle,@CatGeneral,9,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM classes WHERE class_name='Class VIII' AND school_id=@SchoolId) INSERT INTO classes(class_name,class_group_id,fee_category_id,sequence_no,status,school_id,created_by) VALUES('Class VIII',@GrpMiddle,@CatGeneral,10,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM classes WHERE class_name='Class IX'  AND school_id=@SchoolId) INSERT INTO classes(class_name,class_group_id,fee_category_id,sequence_no,status,school_id,created_by) VALUES('Class IX',@GrpHigh,@CatGeneral,11,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM classes WHERE class_name='Class X'   AND school_id=@SchoolId) INSERT INTO classes(class_name,class_group_id,fee_category_id,sequence_no,status,school_id,created_by) VALUES('Class X',@GrpHigh,@CatGeneral,12,'Y',@SchoolId,'demo')

PRINT 'Classes done.'
GO


-- ============================================================
-- 5. Fee Types
-- ============================================================
DECLARE @SchoolId INT = 1
DECLARE @AcadYearId INT
SELECT @AcadYearId = academic_year_id FROM academic_years WHERE academic_year='2024-25' AND school_id=@SchoolId

IF NOT EXISTS (SELECT 1 FROM fee_types WHERE fee_type_name='Tuition Fee'    AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO fee_types(fee_type_name,academic_year_id,sequence_no,status,school_id,created_by) VALUES('Tuition Fee',@AcadYearId,1,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM fee_types WHERE fee_type_name='Development Fee' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO fee_types(fee_type_name,academic_year_id,sequence_no,status,school_id,created_by) VALUES('Development Fee',@AcadYearId,2,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM fee_types WHERE fee_type_name='Exam Fee'        AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO fee_types(fee_type_name,academic_year_id,sequence_no,status,school_id,created_by) VALUES('Exam Fee',@AcadYearId,3,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM fee_types WHERE fee_type_name='Computer Lab Fee' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO fee_types(fee_type_name,academic_year_id,sequence_no,status,school_id,created_by) VALUES('Computer Lab Fee',@AcadYearId,4,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM fee_types WHERE fee_type_name='Sports Fee'      AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO fee_types(fee_type_name,academic_year_id,sequence_no,status,school_id,created_by) VALUES('Sports Fee',@AcadYearId,5,'Y',@SchoolId,'demo')

PRINT 'Fee types done.'
GO


-- ============================================================
-- 6. Terms (quarterly)
-- ============================================================
DECLARE @SchoolId INT = 1
DECLARE @AcadYearId INT
SELECT @AcadYearId = academic_year_id FROM academic_years WHERE academic_year='2024-25' AND school_id=@SchoolId

IF NOT EXISTS (SELECT 1 FROM terms WHERE term_name='1st Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO terms(term_name,year_name,order_no,academic_year_id,status,school_id,created_by) VALUES('1st Quarter','2024-25',1,@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM terms WHERE term_name='2nd Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO terms(term_name,year_name,order_no,academic_year_id,status,school_id,created_by) VALUES('2nd Quarter','2024-25',2,@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM terms WHERE term_name='3rd Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO terms(term_name,year_name,order_no,academic_year_id,status,school_id,created_by) VALUES('3rd Quarter','2024-25',3,@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM terms WHERE term_name='4th Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO terms(term_name,year_name,order_no,academic_year_id,status,school_id,created_by) VALUES('4th Quarter','2024-25',4,@AcadYearId,'Y',@SchoolId,'demo')

PRINT 'Terms done.'
GO


-- ============================================================
-- 7. Subjects
-- ============================================================
DECLARE @SchoolId INT = 1
DECLARE @AcadYearId INT
SELECT @AcadYearId = academic_year_id FROM academic_years WHERE academic_year='2024-25' AND school_id=@SchoolId

IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='English'           AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('English','ENG','Language',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='Hindi'             AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('Hindi','HIN','Language',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='Mathematics'       AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('Mathematics','MATH','Theory',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='Science'           AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('Science','SCI','Theory',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='Social Studies'    AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('Social Studies','SST','Theory',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='Telugu'            AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('Telugu','TEL','Language',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='Computer Science'  AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('Computer Science','CS','Theory',@AcadYearId,'Y',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM subjects WHERE subject_name='General Knowledge' AND academic_year_id=@AcadYearId AND school_id=@SchoolId) INSERT INTO subjects(subject_name,short_name,subject_type,academic_year_id,status,school_id,created_by) VALUES('General Knowledge','GK','General',@AcadYearId,'Y',@SchoolId,'demo')

PRINT 'Subjects done.'
GO


-- ============================================================
-- 8. Students (15 students across Class VI, VII, VIII, IX, X)
-- ============================================================
DECLARE @SchoolId    INT = 1
DECLARE @AcadYearId  INT
DECLARE @CatGeneral  INT
DECLARE @ClsVI INT, @ClsVII INT, @ClsVIII INT, @ClsIX INT, @ClsX INT

SELECT @AcadYearId = academic_year_id FROM academic_years WHERE academic_year='2024-25' AND school_id=@SchoolId
SELECT @CatGeneral = fee_category_id FROM fee_categories WHERE category_name='General' AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @ClsVI   = class_id FROM classes WHERE class_name='Class VI'   AND school_id=@SchoolId
SELECT @ClsVII  = class_id FROM classes WHERE class_name='Class VII'  AND school_id=@SchoolId
SELECT @ClsVIII = class_id FROM classes WHERE class_name='Class VIII' AND school_id=@SchoolId
SELECT @ClsIX   = class_id FROM classes WHERE class_name='Class IX'   AND school_id=@SchoolId
SELECT @ClsX    = class_id FROM classes WHERE class_name='Class X'    AND school_id=@SchoolId

-- Class VI
IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2024-VI-001' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,email,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2024-VI-001','Arjun Sharma','Male','2013-04-12','Ramesh Sharma','9876501001','Sunita Sharma','9876501002','12, MG Road','Ameerpet','Hyderabad','Telangana','arjun.sharma@email.com','B+',@ClsVI,'A','01',@CatGeneral,@AcadYearId,'Active','2024-06-01',@SchoolId,'demo')

IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2024-VI-002' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2024-VI-002','Priya Reddy','Female','2013-07-25','Venkat Reddy','9876501003','Kavitha Reddy','9876501004','45, SR Nagar','Ameerpet','Hyderabad','Telangana','O+',@ClsVI,'A','02',@CatGeneral,@AcadYearId,'Active','2024-06-01',@SchoolId,'demo')

IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2024-VI-003' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2024-VI-003','Mohammed Irfan','Male','2013-02-18','Abdul Irfan','9876501005','Fatima Irfan','9876501006','78, Banjara Hills','Banjara Hills','Hyderabad','Telangana','A+',@ClsVI,'B','01',@CatGeneral,@AcadYearId,'Active','2024-06-01',@SchoolId,'demo')

-- Class VII
IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2023-VII-001' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2023-VII-001','Ananya Patel','Female','2012-09-03','Suresh Patel','9876501007','Meena Patel','9876501008','23, Jubilee Hills','Jubilee Hills','Hyderabad','Telangana','AB+',@ClsVII,'A','01',@CatGeneral,@AcadYearId,'Active','2023-06-01',@SchoolId,'demo')

IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2023-VII-002' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2023-VII-002','Rohit Kumar','Male','2012-11-14','Sanjeev Kumar','9876501009','Anita Kumar','9876501010','56, Himayat Nagar','Himayat Nagar','Hyderabad','Telangana','B-',@ClsVII,'A','02',@CatGeneral,@AcadYearId,'Active','2023-06-01',@SchoolId,'demo')

IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2023-VII-003' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2023-VII-003','Divya Nair','Female','2012-05-22','Praveen Nair','9876501011','Suja Nair','9876501012','34, Madhapur','Madhapur','Hyderabad','Telangana','O-',@ClsVII,'B','01',@CatGeneral,@AcadYearId,'Active','2023-06-01',@SchoolId,'demo')

-- Class VIII
IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2022-VIII-001' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2022-VIII-001','Vikram Singh','Male','2011-08-09','Rajveer Singh','9876501013','Poonam Singh','9876501014','89, Kukatpally','Kukatpally','Hyderabad','Telangana','A-',@ClsVIII,'A','01',@CatGeneral,@AcadYearId,'Active','2022-06-01',@SchoolId,'demo')

IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2022-VIII-002' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2022-VIII-002','Sneha Iyer','Female','2011-03-17','Krishnaswamy Iyer','9876501015','Lalitha Iyer','9876501016','12, Begumpet','Begumpet','Hyderabad','Telangana','B+',@ClsVIII,'A','02',@CatGeneral,@AcadYearId,'Active','2022-06-01',@SchoolId,'demo')

IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2022-VIII-003' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2022-VIII-003','Karthik Naidu','Male','2011-12-30','Nagaraju Naidu','9876501017','Saraswathi Naidu','9876501018','67, KPHB Colony','KPHB','Hyderabad','Telangana','AB-',@ClsVIII,'B','01',@CatGeneral,@AcadYearId,'Active','2022-06-01',@SchoolId,'demo')

-- Class IX
IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2021-IX-001' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2021-IX-001','Aisha Begum','Female','2010-06-08','Imran Khan','9876501019','Shabana Khan','9876501020','45, Tolichowki','Tolichowki','Hyderabad','Telangana','O+',@ClsIX,'A','01',@CatGeneral,@AcadYearId,'Active','2021-06-01',@SchoolId,'demo')

IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2021-IX-002' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2021-IX-002','Surya Prakash','Male','2010-01-20','Bhaskar Rao','9876501021','Usha Rani','9876501022','90, Dilsukhnagar','Dilsukhnagar','Hyderabad','Telangana','A+',@ClsIX,'A','02',@CatGeneral,@AcadYearId,'Active','2021-06-01',@SchoolId,'demo')

IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2021-IX-003' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2021-IX-003','Meghana Varma','Female','2010-10-05','Srinivas Varma','9876501023','Bharati Varma','9876501024','33, LB Nagar','LB Nagar','Hyderabad','Telangana','B+',@ClsIX,'B','01',@CatGeneral,@AcadYearId,'Active','2021-06-01',@SchoolId,'demo')

-- Class X
IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2020-X-001' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2020-X-001','Harini Reddy','Female','2009-04-15','Nagesh Reddy','9876501025','Saritha Reddy','9876501026','11, Secunderabad','Secunderabad','Hyderabad','Telangana','O-',@ClsX,'A','01',@CatGeneral,@AcadYearId,'Active','2020-06-01',@SchoolId,'demo')

IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2020-X-002' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2020-X-002','Aditya Joshi','Male','2009-08-28','Mahesh Joshi','9876501027','Rekha Joshi','9876501028','72, Kompally','Kompally','Hyderabad','Telangana','AB+',@ClsX,'A','02',@CatGeneral,@AcadYearId,'Active','2020-06-01',@SchoolId,'demo')

IF NOT EXISTS (SELECT 1 FROM students WHERE admission_no='2020-X-003' AND school_id=@SchoolId)
    INSERT INTO students (admission_no,student_name,gender,date_of_birth,father_name,father_mobile,mother_name,mother_mobile,door_no,address_area,address_city,address_state,blood_group,class_id,section,roll_no,fee_category_id,academic_year_id,status,date_of_joining,school_id,created_by)
    VALUES ('2020-X-003','Pavani Rao','Female','2009-12-11','Chandrasekhara Rao','9876501029','Vijayalakshmi Rao','9876501030','88, Malkajgiri','Malkajgiri','Hyderabad','Telangana','A-',@ClsX,'A','03',@CatGeneral,@AcadYearId,'Active','2020-06-01',@SchoolId,'demo')

PRINT 'Students done.'
GO


-- ============================================================
-- 9. Fee Structures  (Tuition + Development for 3 key classes)
-- ============================================================
DECLARE @SchoolId   INT = 1
DECLARE @AcadYearId INT
DECLARE @CatGeneral INT
DECLARE @ClsVI INT, @ClsVIII INT, @ClsX INT
DECLARE @FtTuition INT, @FtDevelopment INT, @FtExam INT
DECLARE @T1 INT, @T2 INT, @T3 INT, @T4 INT

SELECT @AcadYearId   = academic_year_id  FROM academic_years WHERE academic_year='2024-25' AND school_id=@SchoolId
SELECT @CatGeneral   = fee_category_id   FROM fee_categories WHERE category_name='General' AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @ClsVI        = class_id          FROM classes WHERE class_name='Class VI'   AND school_id=@SchoolId
SELECT @ClsVIII      = class_id          FROM classes WHERE class_name='Class VIII' AND school_id=@SchoolId
SELECT @ClsX         = class_id          FROM classes WHERE class_name='Class X'    AND school_id=@SchoolId
SELECT @FtTuition    = fee_type_id       FROM fee_types WHERE fee_type_name='Tuition Fee'    AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @FtDevelopment= fee_type_id       FROM fee_types WHERE fee_type_name='Development Fee' AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @FtExam       = fee_type_id       FROM fee_types WHERE fee_type_name='Exam Fee'        AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @T1 = term_id FROM terms WHERE term_name='1st Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @T2 = term_id FROM terms WHERE term_name='2nd Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @T3 = term_id FROM terms WHERE term_name='3rd Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @T4 = term_id FROM terms WHERE term_name='4th Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId

-- Class VI: Tuition 2500/quarter, Development 500/quarter, Exam 200/quarter
IF NOT EXISTS (SELECT 1 FROM fee_structures WHERE class_id=@ClsVI AND fee_category_id=@CatGeneral AND fee_type_id=@FtTuition AND term_id=@T1 AND academic_year_id=@AcadYearId)
BEGIN
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVI,@FtTuition,   @T1,2500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVI,@FtTuition,   @T2,2500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVI,@FtTuition,   @T3,2500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVI,@FtTuition,   @T4,2500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVI,@FtDevelopment,@T1, 500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVI,@FtDevelopment,@T2, 500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVI,@FtDevelopment,@T3, 500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVI,@FtDevelopment,@T4, 500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVI,@FtExam,       @T1, 200,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVI,@FtExam,       @T2, 200,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVI,@FtExam,       @T3, 200,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVI,@FtExam,       @T4, 200,@AcadYearId,'Y',@SchoolId,'demo')
END

-- Class VIII: Tuition 3500/quarter, Development 800/quarter, Exam 300/quarter
IF NOT EXISTS (SELECT 1 FROM fee_structures WHERE class_id=@ClsVIII AND fee_category_id=@CatGeneral AND fee_type_id=@FtTuition AND term_id=@T1 AND academic_year_id=@AcadYearId)
BEGIN
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVIII,@FtTuition,   @T1,3500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVIII,@FtTuition,   @T2,3500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVIII,@FtTuition,   @T3,3500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVIII,@FtTuition,   @T4,3500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVIII,@FtDevelopment,@T1, 800,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVIII,@FtDevelopment,@T2, 800,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVIII,@FtDevelopment,@T3, 800,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVIII,@FtDevelopment,@T4, 800,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVIII,@FtExam,       @T1, 300,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVIII,@FtExam,       @T2, 300,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVIII,@FtExam,       @T3, 300,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsVIII,@FtExam,       @T4, 300,@AcadYearId,'Y',@SchoolId,'demo')
END

-- Class X: Tuition 5000/quarter, Development 1200/quarter, Exam 500/quarter
IF NOT EXISTS (SELECT 1 FROM fee_structures WHERE class_id=@ClsX AND fee_category_id=@CatGeneral AND fee_type_id=@FtTuition AND term_id=@T1 AND academic_year_id=@AcadYearId)
BEGIN
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsX,@FtTuition,   @T1,5000,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsX,@FtTuition,   @T2,5000,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsX,@FtTuition,   @T3,5000,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsX,@FtTuition,   @T4,5000,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsX,@FtDevelopment,@T1,1200,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsX,@FtDevelopment,@T2,1200,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsX,@FtDevelopment,@T3,1200,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsX,@FtDevelopment,@T4,1200,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsX,@FtExam,       @T1, 500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsX,@FtExam,       @T2, 500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsX,@FtExam,       @T3, 500,@AcadYearId,'Y',@SchoolId,'demo')
    INSERT INTO fee_structures(fee_category_id,class_id,fee_type_id,term_id,amount,academic_year_id,status,school_id,created_by) VALUES(@CatGeneral,@ClsX,@FtExam,       @T4, 500,@AcadYearId,'Y',@SchoolId,'demo')
END

PRINT 'Fee structures done.'
GO


-- ============================================================
-- 10. Fee Receipts
--     Arjun (VI): Q1 paid. Vikram (VIII): Q1+Q2 paid.
--     Harini (X): Q1 paid. Aditya (X): Q1+Q2 paid (1 receipt cancelled).
--     Others: Q1 dues pending (shows in dues screen).
-- ============================================================
DECLARE @SchoolId   INT = 1
DECLARE @AcadYearId INT
DECLARE @T1 INT, @T2 INT
DECLARE @FtTuition INT, @FtDevelopment INT, @FtExam INT
DECLARE @CashModeId INT
DECLARE @SArjun BIGINT, @SVikram BIGINT, @SSneha BIGINT, @SHarini BIGINT, @SAditya BIGINT

SELECT @AcadYearId   = academic_year_id FROM academic_years WHERE academic_year='2024-25' AND school_id=@SchoolId
SELECT @T1           = term_id FROM terms WHERE term_name='1st Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @T2           = term_id FROM terms WHERE term_name='2nd Quarter' AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @FtTuition    = fee_type_id FROM fee_types WHERE fee_type_name='Tuition Fee'     AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @FtDevelopment= fee_type_id FROM fee_types WHERE fee_type_name='Development Fee' AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @FtExam       = fee_type_id FROM fee_types WHERE fee_type_name='Exam Fee'        AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @CashModeId   = payment_mode_id FROM payment_modes WHERE mode_name='Cash' AND school_id IS NULL
SELECT @SArjun       = student_id FROM students WHERE admission_no='2024-VI-001'   AND school_id=@SchoolId
SELECT @SVikram      = student_id FROM students WHERE admission_no='2022-VIII-001' AND school_id=@SchoolId
SELECT @SSneha       = student_id FROM students WHERE admission_no='2022-VIII-002' AND school_id=@SchoolId
SELECT @SHarini      = student_id FROM students WHERE admission_no='2020-X-001'    AND school_id=@SchoolId
SELECT @SAditya      = student_id FROM students WHERE admission_no='2020-X-002'    AND school_id=@SchoolId

-- Arjun (Class VI) — Q1 paid: 2500+500+200 = 3200
IF NOT EXISTS (SELECT 1 FROM fee_receipts WHERE student_id=@SArjun AND academic_year_id=@AcadYearId AND status='Active')
BEGIN
    DECLARE @Rid1 INT
    INSERT INTO fee_receipts(receipt_no,student_id,academic_year_id,payment_date,total_amount,payment_mode_id,status,school_id,created_by)
    VALUES('TMP',@SArjun,@AcadYearId,'2024-06-05',3200,@CashModeId,'Active',@SchoolId,'admin')
    SET @Rid1 = SCOPE_IDENTITY()
    UPDATE fee_receipts SET receipt_no = '2024-' + RIGHT('00000'+CAST(@Rid1 AS VARCHAR),5) WHERE receipt_id=@Rid1
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid1,@FtTuition,   @T1,2500,0,2500,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid1,@FtDevelopment,@T1, 500,0, 500,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid1,@FtExam,      @T1, 200,0, 200,@SchoolId)
END

-- Vikram (Class VIII) — Q1 paid: 3500+800+300 = 4600
IF NOT EXISTS (SELECT 1 FROM fee_receipts WHERE student_id=@SVikram AND academic_year_id=@AcadYearId AND status='Active')
BEGIN
    DECLARE @Rid2 INT
    INSERT INTO fee_receipts(receipt_no,student_id,academic_year_id,payment_date,total_amount,payment_mode_id,status,school_id,created_by)
    VALUES('TMP',@SVikram,@AcadYearId,'2024-06-10',4600,@CashModeId,'Active',@SchoolId,'admin')
    SET @Rid2 = SCOPE_IDENTITY()
    UPDATE fee_receipts SET receipt_no = '2024-' + RIGHT('00000'+CAST(@Rid2 AS VARCHAR),5) WHERE receipt_id=@Rid2
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid2,@FtTuition,   @T1,3500,0,3500,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid2,@FtDevelopment,@T1, 800,0, 800,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid2,@FtExam,      @T1, 300,0, 300,@SchoolId)
END

-- Vikram — Q2 also paid
IF NOT EXISTS (SELECT 1 FROM fee_receipts r JOIN fee_receipt_items i ON i.receipt_id=r.receipt_id WHERE r.student_id=@SVikram AND i.term_id=@T2 AND r.status='Active')
BEGIN
    DECLARE @Rid3 INT
    INSERT INTO fee_receipts(receipt_no,student_id,academic_year_id,payment_date,total_amount,payment_mode_id,status,school_id,created_by)
    VALUES('TMP',@SVikram,@AcadYearId,'2024-09-08',4600,@CashModeId,'Active',@SchoolId,'admin')
    SET @Rid3 = SCOPE_IDENTITY()
    UPDATE fee_receipts SET receipt_no = '2024-' + RIGHT('00000'+CAST(@Rid3 AS VARCHAR),5) WHERE receipt_id=@Rid3
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid3,@FtTuition,   @T2,3500,0,3500,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid3,@FtDevelopment,@T2, 800,0, 800,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid3,@FtExam,      @T2, 300,0, 300,@SchoolId)
END

-- Harini (Class X) — Q1 paid: 5000+1200+500 = 6700
IF NOT EXISTS (SELECT 1 FROM fee_receipts WHERE student_id=@SHarini AND academic_year_id=@AcadYearId AND status='Active')
BEGIN
    DECLARE @Rid4 INT
    INSERT INTO fee_receipts(receipt_no,student_id,academic_year_id,payment_date,total_amount,payment_mode_id,status,school_id,created_by)
    VALUES('TMP',@SHarini,@AcadYearId,'2024-06-15',6700,@CashModeId,'Active',@SchoolId,'admin')
    SET @Rid4 = SCOPE_IDENTITY()
    UPDATE fee_receipts SET receipt_no = '2024-' + RIGHT('00000'+CAST(@Rid4 AS VARCHAR),5) WHERE receipt_id=@Rid4
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid4,@FtTuition,   @T1,5000,0,5000,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid4,@FtDevelopment,@T1,1200,0,1200,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid4,@FtExam,      @T1, 500,0, 500,@SchoolId)
END

-- Aditya (Class X) — Q1 cancelled, then re-issued; Q2 paid
IF NOT EXISTS (SELECT 1 FROM fee_receipts WHERE student_id=@SAditya AND academic_year_id=@AcadYearId)
BEGIN
    -- Cancelled receipt
    DECLARE @Rid5 INT
    INSERT INTO fee_receipts(receipt_no,student_id,academic_year_id,payment_date,total_amount,payment_mode_id,status,cancelled_by,cancelled_at,cancel_reason,school_id,created_by)
    VALUES('TMP',@SAditya,@AcadYearId,'2024-06-07',6700,@CashModeId,'Cancelled','admin',GETDATE(),'Data entry error',@SchoolId,'admin')
    SET @Rid5 = SCOPE_IDENTITY()
    UPDATE fee_receipts SET receipt_no = '2024-' + RIGHT('00000'+CAST(@Rid5 AS VARCHAR),5) WHERE receipt_id=@Rid5
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid5,@FtTuition,   @T1,5000,0,5000,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid5,@FtDevelopment,@T1,1200,0,1200,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid5,@FtExam,      @T1, 500,0, 500,@SchoolId)
    -- Re-issued active receipt (with ₹100 concession on Exam fee)
    DECLARE @Rid6 INT
    INSERT INTO fee_receipts(receipt_no,student_id,academic_year_id,payment_date,total_amount,payment_mode_id,status,school_id,created_by)
    VALUES('TMP',@SAditya,@AcadYearId,'2024-06-08',6600,@CashModeId,'Active',@SchoolId,'admin')
    SET @Rid6 = SCOPE_IDENTITY()
    UPDATE fee_receipts SET receipt_no = '2024-' + RIGHT('00000'+CAST(@Rid6 AS VARCHAR),5) WHERE receipt_id=@Rid6
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid6,@FtTuition,   @T1,5000,  0,5000,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid6,@FtDevelopment,@T1,1200,  0,1200,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid6,@FtExam,      @T1, 500,100, 400,@SchoolId)
    -- Q2 paid
    DECLARE @Rid7 INT
    INSERT INTO fee_receipts(receipt_no,student_id,academic_year_id,payment_date,total_amount,payment_mode_id,status,school_id,created_by)
    VALUES('TMP',@SAditya,@AcadYearId,'2024-09-12',6700,@CashModeId,'Active',@SchoolId,'admin')
    SET @Rid7 = SCOPE_IDENTITY()
    UPDATE fee_receipts SET receipt_no = '2024-' + RIGHT('00000'+CAST(@Rid7 AS VARCHAR),5) WHERE receipt_id=@Rid7
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid7,@FtTuition,   @T2,5000,0,5000,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid7,@FtDevelopment,@T2,1200,0,1200,@SchoolId)
    INSERT INTO fee_receipt_items(receipt_id,fee_type_id,term_id,amount,concession_amount,net_amount,school_id) VALUES(@Rid7,@FtExam,      @T2, 500,0, 500,@SchoolId)
END

PRINT 'Fee receipts done.'
GO


-- ============================================================
-- 11. Exam Types
-- ============================================================
DECLARE @SchoolId INT = 1
DECLARE @AcadYearId INT
SELECT @AcadYearId = academic_year_id FROM academic_years WHERE academic_year='2024-25' AND school_id=@SchoolId

IF NOT EXISTS (SELECT 1 FROM exam_types WHERE exam_type_name='Unit Test 1' AND school_id=@SchoolId AND academic_year_id=@AcadYearId)
    INSERT INTO exam_types(exam_type_name,academic_year_id,display_order,status,school_id,created_by) VALUES('Unit Test 1',@AcadYearId,1,'Active',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM exam_types WHERE exam_type_name='Unit Test 2' AND school_id=@SchoolId AND academic_year_id=@AcadYearId)
    INSERT INTO exam_types(exam_type_name,academic_year_id,display_order,status,school_id,created_by) VALUES('Unit Test 2',@AcadYearId,2,'Active',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM exam_types WHERE exam_type_name='Midterm Exam' AND school_id=@SchoolId AND academic_year_id=@AcadYearId)
    INSERT INTO exam_types(exam_type_name,academic_year_id,display_order,status,school_id,created_by) VALUES('Midterm Exam',@AcadYearId,3,'Active',@SchoolId,'demo')
IF NOT EXISTS (SELECT 1 FROM exam_types WHERE exam_type_name='Final Exam' AND school_id=@SchoolId AND academic_year_id=@AcadYearId)
    INSERT INTO exam_types(exam_type_name,academic_year_id,display_order,status,school_id,created_by) VALUES('Final Exam',@AcadYearId,4,'Active',@SchoolId,'demo')

PRINT 'Exam types done.'
GO


-- ============================================================
-- 12. Student Marks  (Unit Test 1 + Midterm for Class IX students)
-- ============================================================
DECLARE @SchoolId   INT = 1
DECLARE @AcadYearId INT
DECLARE @ExUT1 INT, @ExMid INT
DECLARE @SEngId INT, @SHinId INT, @SMathId INT, @SSciId INT, @SSSTId INT, @STelId INT
DECLARE @SAisha BIGINT, @SSurya BIGINT, @SMeghana BIGINT

SELECT @AcadYearId = academic_year_id FROM academic_years WHERE academic_year='2024-25' AND school_id=@SchoolId
SELECT @ExUT1      = exam_type_id FROM exam_types WHERE exam_type_name='Unit Test 1'  AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @ExMid      = exam_type_id FROM exam_types WHERE exam_type_name='Midterm Exam' AND school_id=@SchoolId AND academic_year_id=@AcadYearId
SELECT @SEngId     = subject_id FROM subjects WHERE subject_name='English'        AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @SHinId     = subject_id FROM subjects WHERE subject_name='Hindi'          AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @SMathId    = subject_id FROM subjects WHERE subject_name='Mathematics'    AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @SSciId     = subject_id FROM subjects WHERE subject_name='Science'        AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @SSSTId     = subject_id FROM subjects WHERE subject_name='Social Studies' AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @STelId     = subject_id FROM subjects WHERE subject_name='Telugu'         AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @SAisha     = student_id FROM students WHERE admission_no='2021-IX-001' AND school_id=@SchoolId
SELECT @SSurya     = student_id FROM students WHERE admission_no='2021-IX-002' AND school_id=@SchoolId
SELECT @SMeghana   = student_id FROM students WHERE admission_no='2021-IX-003' AND school_id=@SchoolId

-- Aisha Begum — Unit Test 1 (max 25)
IF NOT EXISTS (SELECT 1 FROM student_marks WHERE student_id=@SAisha AND exam_type_id=@ExUT1 AND academic_year_id=@AcadYearId AND school_id=@SchoolId)
BEGIN
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SAisha,@SEngId, @ExUT1,@AcadYearId,22,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SAisha,@SHinId, @ExUT1,@AcadYearId,20,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SAisha,@SMathId,@ExUT1,@AcadYearId,23,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SAisha,@SSciId, @ExUT1,@AcadYearId,21,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SAisha,@SSSTId, @ExUT1,@AcadYearId,24,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SAisha,@STelId, @ExUT1,@AcadYearId,19,25,0,@SchoolId,'admin')
END
-- Aisha Begum — Midterm (max 100)
IF NOT EXISTS (SELECT 1 FROM student_marks WHERE student_id=@SAisha AND exam_type_id=@ExMid AND academic_year_id=@AcadYearId AND school_id=@SchoolId)
BEGIN
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SAisha,@SEngId, @ExMid,@AcadYearId,85,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SAisha,@SHinId, @ExMid,@AcadYearId,78,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SAisha,@SMathId,@ExMid,@AcadYearId,91,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SAisha,@SSciId, @ExMid,@AcadYearId,88,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SAisha,@SSSTId, @ExMid,@AcadYearId,82,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SAisha,@STelId, @ExMid,@AcadYearId,74,100,0,@SchoolId,'admin')
END

-- Surya Prakash — Unit Test 1
IF NOT EXISTS (SELECT 1 FROM student_marks WHERE student_id=@SSurya AND exam_type_id=@ExUT1 AND academic_year_id=@AcadYearId AND school_id=@SchoolId)
BEGIN
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SSurya,@SEngId, @ExUT1,@AcadYearId,18,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SSurya,@SHinId, @ExUT1,@AcadYearId,17,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SSurya,@SMathId,@ExUT1,@AcadYearId,20,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SSurya,@SSciId, @ExUT1,@AcadYearId,22,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SSurya,@SSSTId, @ExUT1,@AcadYearId,19,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SSurya,@STelId, @ExUT1,@AcadYearId,15,25,0,@SchoolId,'admin')
END
-- Surya Prakash — Midterm (was absent for Hindi)
IF NOT EXISTS (SELECT 1 FROM student_marks WHERE student_id=@SSurya AND exam_type_id=@ExMid AND academic_year_id=@AcadYearId AND school_id=@SchoolId)
BEGIN
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SSurya,@SEngId, @ExMid,@AcadYearId,71,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SSurya,@SHinId, @ExMid,@AcadYearId,  0,100,1,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SSurya,@SMathId,@ExMid,@AcadYearId,82,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SSurya,@SSciId, @ExMid,@AcadYearId,87,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SSurya,@SSSTId, @ExMid,@AcadYearId,75,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SSurya,@STelId, @ExMid,@AcadYearId,68,100,0,@SchoolId,'admin')
END

-- Meghana Varma — Unit Test 1
IF NOT EXISTS (SELECT 1 FROM student_marks WHERE student_id=@SMeghana AND exam_type_id=@ExUT1 AND academic_year_id=@AcadYearId AND school_id=@SchoolId)
BEGIN
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SMeghana,@SEngId, @ExUT1,@AcadYearId,24,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SMeghana,@SHinId, @ExUT1,@AcadYearId,23,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SMeghana,@SMathId,@ExUT1,@AcadYearId,25,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SMeghana,@SSciId, @ExUT1,@AcadYearId,24,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SMeghana,@SSSTId, @ExUT1,@AcadYearId,22,25,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SMeghana,@STelId, @ExUT1,@AcadYearId,21,25,0,@SchoolId,'admin')
END
-- Meghana — Midterm (topper)
IF NOT EXISTS (SELECT 1 FROM student_marks WHERE student_id=@SMeghana AND exam_type_id=@ExMid AND academic_year_id=@AcadYearId AND school_id=@SchoolId)
BEGIN
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SMeghana,@SEngId, @ExMid,@AcadYearId,95,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SMeghana,@SHinId, @ExMid,@AcadYearId,90,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SMeghana,@SMathId,@ExMid,@AcadYearId,98,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SMeghana,@SSciId, @ExMid,@AcadYearId,93,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SMeghana,@SSSTId, @ExMid,@AcadYearId,88,100,0,@SchoolId,'admin')
    INSERT INTO student_marks(student_id,subject_id,exam_type_id,academic_year_id,marks_obtained,max_marks,is_absent,school_id,entered_by) VALUES(@SMeghana,@STelId, @ExMid,@AcadYearId,86,100,0,@SchoolId,'admin')
END

PRINT 'Student marks done.'
GO


-- ============================================================
-- 13. Attendance  (Jan–Mar 2025 for 3 Class IX students)
--     Weekdays only; Sundays skipped; a few absences scattered.
--     ~65 working days across 3 months.
-- ============================================================
DECLARE @SchoolId INT = 1
DECLARE @SAisha   BIGINT, @SSurya BIGINT, @SMeghana BIGINT

SELECT @SAisha   = student_id FROM students WHERE admission_no='2021-IX-001' AND school_id=@SchoolId
SELECT @SSurya   = student_id FROM students WHERE admission_no='2021-IX-002' AND school_id=@SchoolId
SELECT @SMeghana = student_id FROM students WHERE admission_no='2021-IX-003' AND school_id=@SchoolId

-- Helper: insert only if date is Mon–Sat and record not already there
-- January 2025  (1st = Wed)
DECLARE @d DATE = '2025-01-01'
WHILE @d <= '2025-03-31'
BEGIN
    IF DATEPART(WEEKDAY,@d) NOT IN (1) -- skip Sunday (1=Sun in default)
    BEGIN
        -- Aisha: absent on 8-Jan and 14-Feb (simulate real absences)
        DECLARE @aStatus_A VARCHAR(10) = CASE WHEN @d IN ('2025-01-08','2025-02-14') THEN 'Absent' ELSE 'Present' END
        -- Surya: absent on 22-Jan, 5-Feb, 3-Mar, 4-Mar (2 consecutive absent)
        DECLARE @aStatus_S VARCHAR(10) = CASE WHEN @d IN ('2025-01-22','2025-02-05','2025-03-03','2025-03-04') THEN 'Absent'
                                               WHEN @d = '2025-01-15' THEN 'Late' ELSE 'Present' END
        -- Meghana: late on 10-Feb, otherwise full attendance
        DECLARE @aStatus_M VARCHAR(10) = CASE WHEN @d = '2025-02-10' THEN 'Late' ELSE 'Present' END

        IF NOT EXISTS (SELECT 1 FROM student_attendance WHERE student_id=@SAisha AND attendance_date=@d AND school_id=@SchoolId)
            INSERT INTO student_attendance(student_id,attendance_date,status,school_id,marked_by) VALUES(@SAisha,@d,@aStatus_A,@SchoolId,'admin')

        IF NOT EXISTS (SELECT 1 FROM student_attendance WHERE student_id=@SSurya AND attendance_date=@d AND school_id=@SchoolId)
            INSERT INTO student_attendance(student_id,attendance_date,status,school_id,marked_by) VALUES(@SSurya,@d,@aStatus_S,@SchoolId,'admin')

        IF NOT EXISTS (SELECT 1 FROM student_attendance WHERE student_id=@SMeghana AND attendance_date=@d AND school_id=@SchoolId)
            INSERT INTO student_attendance(student_id,attendance_date,status,school_id,marked_by) VALUES(@SMeghana,@d,@aStatus_M,@SchoolId,'admin')
    END
    SET @d = DATEADD(DAY,1,@d)
END

PRINT 'Attendance done.'
GO


-- ============================================================
-- 14. Homework  (6 recent assignments for Class IX)
-- ============================================================
DECLARE @SchoolId INT = 1
DECLARE @AcadYearId INT
DECLARE @ClsIX INT
DECLARE @SEngId INT, @SMathId INT, @SSciId INT, @SSSTId INT, @SHinId INT

SELECT @AcadYearId = academic_year_id FROM academic_years WHERE academic_year='2024-25' AND school_id=@SchoolId
SELECT @ClsIX   = class_id   FROM classes  WHERE class_name='Class IX'  AND school_id=@SchoolId
SELECT @SEngId  = subject_id FROM subjects WHERE subject_name='English'       AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @SMathId = subject_id FROM subjects WHERE subject_name='Mathematics'   AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @SSciId  = subject_id FROM subjects WHERE subject_name='Science'       AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @SSSTId  = subject_id FROM subjects WHERE subject_name='Social Studies' AND academic_year_id=@AcadYearId AND school_id=@SchoolId
SELECT @SHinId  = subject_id FROM subjects WHERE subject_name='Hindi'         AND academic_year_id=@AcadYearId AND school_id=@SchoolId

IF NOT EXISTS (SELECT 1 FROM homework WHERE title='Chapter 5 – Letter Writing' AND class_id=@ClsIX AND school_id=@SchoolId)
    INSERT INTO homework(title,description,subject_id,class_id,assigned_date,due_date,status,school_id,created_by)
    VALUES('Chapter 5 – Letter Writing','Write a formal letter to your principal requesting leave for 3 days. Follow the format discussed in class.',@SEngId,@ClsIX,'2025-03-17','2025-03-20','Active',@SchoolId,'admin')

IF NOT EXISTS (SELECT 1 FROM homework WHERE title='Algebra Practice – Quadratics' AND class_id=@ClsIX AND school_id=@SchoolId)
    INSERT INTO homework(title,description,subject_id,class_id,assigned_date,due_date,status,school_id,created_by)
    VALUES('Algebra Practice – Quadratics','Solve exercises 4.1 to 4.5 from the NCERT textbook. Show all steps clearly.',@SMathId,@ClsIX,'2025-03-17','2025-03-21','Active',@SchoolId,'admin')

IF NOT EXISTS (SELECT 1 FROM homework WHERE title='Digestion and Absorption – Q&A' AND class_id=@ClsIX AND school_id=@SchoolId)
    INSERT INTO homework(title,description,subject_id,class_id,assigned_date,due_date,status,school_id,created_by)
    VALUES('Digestion and Absorption – Q&A','Answer all review questions from Chapter 6. Draw a labelled diagram of the digestive system.',@SSciId,@ClsIX,'2025-03-15','2025-03-19','Active',@SchoolId,'admin')

IF NOT EXISTS (SELECT 1 FROM homework WHERE title='French Revolution – Map Work' AND class_id=@ClsIX AND school_id=@SchoolId)
    INSERT INTO homework(title,description,subject_id,class_id,assigned_date,due_date,status,school_id,created_by)
    VALUES('French Revolution – Map Work','Mark the key locations of the French Revolution on the outline map of Europe. Label Paris, Versailles, and at least 4 other sites.',@SSSTId,@ClsIX,'2025-03-12','2025-03-18','Active',@SchoolId,'admin')

IF NOT EXISTS (SELECT 1 FROM homework WHERE title='Hindi Patra Lekhan' AND class_id=@ClsIX AND school_id=@SchoolId)
    INSERT INTO homework(title,description,subject_id,class_id,assigned_date,due_date,status,school_id,created_by)
    VALUES('Hindi Patra Lekhan','अपने मित्र को पत्र लिखिए जिसमें आप उसे अपने विद्यालय की वार्षिक यात्रा का वर्णन करें।',@SHinId,@ClsIX,'2025-03-10','2025-03-14','Active',@SchoolId,'admin')

IF NOT EXISTS (SELECT 1 FROM homework WHERE title='Maths – Statistics Chapter' AND class_id=@ClsIX AND school_id=@SchoolId)
    INSERT INTO homework(title,description,subject_id,class_id,assigned_date,due_date,status,school_id,created_by)
    VALUES('Maths – Statistics Chapter','Collect daily temperature data for the past 7 days and represent it as a bar graph and a frequency table.',@SMathId,@ClsIX,'2025-03-05','2025-03-10','Active',@SchoolId,'admin')

PRINT 'Homework done.'
GO


-- ============================================================
-- 15. Announcements
-- ============================================================
DECLARE @SchoolId INT = 1
DECLARE @ClsIX    INT
SELECT @ClsIX = class_id FROM classes WHERE class_name='Class IX' AND school_id=@SchoolId

-- School-wide pinned
IF NOT EXISTS (SELECT 1 FROM announcements WHERE title='Annual Day Celebration – 25 March 2025' AND school_id=@SchoolId)
    INSERT INTO announcements(title,description,scope,class_id,is_pinned,status,school_id,created_by)
    VALUES('Annual Day Celebration – 25 March 2025',
           'All parents are invited to the Annual Day on Tuesday, 25 March 2025 at 5:00 PM in the school auditorium. Cultural programmes and prize distribution for academic toppers.',
           'School',NULL,1,'Active',@SchoolId,'admin')

-- School-wide
IF NOT EXISTS (SELECT 1 FROM announcements WHERE title='Summer Vacation Notice' AND school_id=@SchoolId)
    INSERT INTO announcements(title,description,scope,class_id,is_pinned,status,school_id,created_by)
    VALUES('Summer Vacation Notice',
           'School will remain closed from 1 April to 31 May 2025 for summer vacations. Classes will reopen on 2 June 2025.',
           'School',NULL,0,'Active',@SchoolId,'admin')

IF NOT EXISTS (SELECT 1 FROM announcements WHERE title='Fee Due Reminder – Q3' AND school_id=@SchoolId)
    INSERT INTO announcements(title,description,scope,class_id,is_pinned,status,school_id,created_by)
    VALUES('Fee Due Reminder – Q3',
           'This is a reminder that 3rd Quarter fees are due by 31 March 2025. Please ensure payment is made on time to avoid a fine of ₹50 per day.',
           'School',NULL,0,'Active',@SchoolId,'admin')

-- Class IX specific
IF NOT EXISTS (SELECT 1 FROM announcements WHERE title='Midterm Results – Class IX' AND school_id=@SchoolId AND class_id=@ClsIX)
    INSERT INTO announcements(title,description,scope,class_id,is_pinned,status,school_id,created_by)
    VALUES('Midterm Results – Class IX',
           'Midterm exam results have been uploaded on the portal. Students can check their marks in the Marks section of the app. Parent-teacher meeting scheduled for 22 March.',
           'Class',@ClsIX,0,'Active',@SchoolId,'admin')

IF NOT EXISTS (SELECT 1 FROM announcements WHERE title='Science Project Submission – Class IX' AND school_id=@SchoolId AND class_id=@ClsIX)
    INSERT INTO announcements(title,description,scope,class_id,is_pinned,status,school_id,created_by)
    VALUES('Science Project Submission – Class IX',
           'All Class IX students must submit their Science project (working model) by 28 March 2025. No extensions will be given. Refer to the project guidelines distributed in class.',
           'Class',@ClsIX,0,'Active',@SchoolId,'admin')

PRINT 'Announcements done.'
GO


-- ============================================================
-- 16. Student Mobile Accounts  (PIN = 1234 for all demo accounts)
--     Registered for 5 students: Aisha, Surya, Meghana, Vikram, Harini
-- ============================================================
DECLARE @SchoolId INT = 1
DECLARE @PinHash  VARCHAR(255)
DECLARE @PinHex   VARCHAR(64) = CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CAST('1234' AS VARCHAR)), 2)
SELECT  @PinHash  = CAST(N'' AS XML).value('xs:base64Binary(xs:hexBinary(sql:variable("@PinHex")))', 'VARCHAR(255)')

DECLARE @SAisha   BIGINT, @SSurya BIGINT, @SMeghana BIGINT, @SVikram BIGINT, @SHarini BIGINT

SELECT @SAisha   = student_id FROM students WHERE admission_no='2021-IX-001' AND school_id=@SchoolId
SELECT @SSurya   = student_id FROM students WHERE admission_no='2021-IX-002' AND school_id=@SchoolId
SELECT @SMeghana = student_id FROM students WHERE admission_no='2021-IX-003' AND school_id=@SchoolId
SELECT @SVikram  = student_id FROM students WHERE admission_no='2022-VIII-001' AND school_id=@SchoolId
SELECT @SHarini  = student_id FROM students WHERE admission_no='2020-X-001' AND school_id=@SchoolId

IF NOT EXISTS (SELECT 1 FROM student_mobile_accounts WHERE student_id=@SAisha)
    INSERT INTO student_mobile_accounts(student_id,pin_hash,mobile,school_id,is_active)
    VALUES(@SAisha,@PinHash,'9876501019',@SchoolId,1)

IF NOT EXISTS (SELECT 1 FROM student_mobile_accounts WHERE student_id=@SSurya)
    INSERT INTO student_mobile_accounts(student_id,pin_hash,mobile,school_id,is_active)
    VALUES(@SSurya,@PinHash,'9876501021',@SchoolId,1)

IF NOT EXISTS (SELECT 1 FROM student_mobile_accounts WHERE student_id=@SMeghana)
    INSERT INTO student_mobile_accounts(student_id,pin_hash,mobile,school_id,is_active)
    VALUES(@SMeghana,@PinHash,'9876501023',@SchoolId,1)

IF NOT EXISTS (SELECT 1 FROM student_mobile_accounts WHERE student_id=@SVikram)
    INSERT INTO student_mobile_accounts(student_id,pin_hash,mobile,school_id,is_active)
    VALUES(@SVikram,@PinHash,'9876501013',@SchoolId,1)

IF NOT EXISTS (SELECT 1 FROM student_mobile_accounts WHERE student_id=@SHarini)
    INSERT INTO student_mobile_accounts(student_id,pin_hash,mobile,school_id,is_active)
    VALUES(@SHarini,@PinHash,'9876501025',@SchoolId,1)

PRINT 'Student mobile accounts done.'
GO


PRINT ''
PRINT '======================================='
PRINT ' Demo data loaded successfully!'
PRINT '======================================='
PRINT ''
PRINT '--- Web App (School) ---'
PRINT 'Login: admin / Admin@123'
PRINT ''
PRINT '--- Mobile App (Student) ---'
PRINT 'All accounts PIN: 1234'
PRINT ''
PRINT 'Student          Admission No      Class'
PRINT '-----------      ------------      -----'
PRINT 'Aisha Begum      2021-IX-001       Class IX'
PRINT 'Surya Prakash    2021-IX-002       Class IX'
PRINT 'Meghana Varma    2021-IX-003       Class IX'
PRINT 'Vikram Singh     2022-VIII-001     Class VIII'
PRINT 'Harini Reddy     2020-X-001        Class X'
PRINT ''
PRINT 'Has marks data:  IX students (Unit Test 1 + Midterm)'
PRINT 'Has attendance:  IX students (Jan-Mar 2025)'
PRINT 'Has homework:    Class IX (6 assignments)'
PRINT 'Has receipts:    Arjun(VI), Vikram(VIII), Harini(X), Aditya(X)'
GO
