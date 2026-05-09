-- ============================================================
-- sample_sections.sql
-- Inserts sample sections (A and B) for every class in a school.
-- Run AFTER sections_migration.sql.
-- Change @schoolId to the school_id from ascent_master.schools.
-- ============================================================

DECLARE @schoolId INT = 1; -- ← Change to your school_id

-- Insert Section A for each class (skip if already exists)
INSERT INTO sections (class_id, section_name, sequence_no, status, school_id)
SELECT c.class_id, 'A', 1, 'Active', @schoolId
FROM   classes c
WHERE  c.school_id = @schoolId
  AND  NOT EXISTS (
        SELECT 1 FROM sections s
        WHERE  s.class_id     = c.class_id
          AND  s.section_name = 'A'
          AND  s.school_id    = @schoolId
       );

-- Insert Section B for each class (skip if already exists)
INSERT INTO sections (class_id, section_name, sequence_no, status, school_id)
SELECT c.class_id, 'B', 2, 'Active', @schoolId
FROM   classes c
WHERE  c.school_id = @schoolId
  AND  NOT EXISTS (
        SELECT 1 FROM sections s
        WHERE  s.class_id     = c.class_id
          AND  s.section_name = 'B'
          AND  s.school_id    = @schoolId
       );

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' sections inserted.';

-- Verify
SELECT c.class_name, s.section_name, s.sequence_no, s.status
FROM   sections s
JOIN   classes  c ON c.class_id = s.class_id
WHERE  s.school_id = @schoolId
ORDER  BY c.class_name, s.sequence_no;
