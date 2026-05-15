-- ============================================================
-- student_unique_id_migration.sql
-- Adds student_unique_id INT column to students table.
-- This is a stable cross-year identifier — same value across
-- all promoted rows for the same student, unlike student_id
-- (IDENTITY, changes each year) or admission_no (can change).
--
-- Backfill logic: groups existing rows by admission_no + school_id
-- and assigns the same sequential number to all rows for the
-- same student. Students with no admission_no get unique IDs.
-- ============================================================

-- 1. Add column (nullable initially for backfill)
ALTER TABLE students
    ADD student_unique_id INT NULL;
GO

-- 2. Backfill existing rows
--    All rows sharing the same admission_no within a school
--    get the same student_unique_id (they represent the same
--    student across academic years).
WITH BaseIds AS (
    SELECT   admission_no,
             school_id,
             MIN(student_id) AS first_student_id
    FROM     students
    WHERE    admission_no IS NOT NULL
    GROUP BY admission_no, school_id
),
Numbered AS (
    SELECT   admission_no,
             school_id,
             ROW_NUMBER() OVER (
                 PARTITION BY school_id
                 ORDER BY first_student_id
             ) AS seq_no
    FROM     BaseIds
)
UPDATE s
SET    s.student_unique_id = n.seq_no
FROM   students s
JOIN   Numbered n
    ON  n.admission_no = s.admission_no
    AND n.school_id    = s.school_id
WHERE  s.student_unique_id IS NULL;
GO

-- 3. Backfill any remaining rows with NULL admission_no
--    (assign unique IDs continuing from the max already assigned)
WITH NullRows AS (
    SELECT student_id,
           school_id,
           ROW_NUMBER() OVER (
               PARTITION BY school_id
               ORDER BY student_id
           ) AS rn
    FROM   students
    WHERE  student_unique_id IS NULL
),
MaxPerSchool AS (
    SELECT school_id, ISNULL(MAX(student_unique_id), 0) AS max_id
    FROM   students
    GROUP BY school_id
)
UPDATE s
SET    s.student_unique_id = m.max_id + nr.rn
FROM   students s
JOIN   NullRows      nr ON nr.student_id = s.student_id
JOIN   MaxPerSchool  m  ON m.school_id   = s.school_id
WHERE  s.student_unique_id IS NULL;
GO

-- 4. Verify backfill (should return 0)
SELECT COUNT(*) AS UnfilledRows
FROM   students
WHERE  student_unique_id IS NULL;
GO

PRINT 'student_unique_id column added and backfilled successfully.';
