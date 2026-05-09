-- ============================================================
-- update_student_section_id.sql
-- Maps existing students' free-text section column to section_id FK.
-- Run AFTER sections_migration.sql and sample_sections.sql.
-- ============================================================

-- STEP 1: Preview which students will be updated (run this first)
SELECT
    s.student_id,
    s.student_name,
    s.section         AS section_text,
    sec.section_id    AS mapped_section_id,
    sec.section_name  AS mapped_section_name
FROM   students s
JOIN   sections sec ON  sec.class_id     = s.class_id
                    AND sec.school_id    = s.school_id
                    AND sec.section_name = s.section
WHERE  s.section    IS NOT NULL
  AND  s.section_id IS NULL;

-- STEP 2: Students whose section text has no matching section row (will NOT be updated)
SELECT
    s.student_id,
    s.student_name,
    s.section AS unmatched_section_text
FROM   students s
WHERE  s.section    IS NOT NULL
  AND  s.section_id IS NULL
  AND  NOT EXISTS (
        SELECT 1 FROM sections sec
        WHERE  sec.class_id     = s.class_id
          AND  sec.school_id    = s.school_id
          AND  sec.section_name = s.section
       );

-- STEP 3: Perform the update (uncomment when preview looks correct)
/*
UPDATE s
SET    s.section_id = sec.section_id
FROM   students s
JOIN   sections sec ON  sec.class_id     = s.class_id
                    AND sec.school_id    = s.school_id
                    AND sec.section_name = s.section
WHERE  s.section    IS NOT NULL
  AND  s.section_id IS NULL;

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' students updated with section_id.';
*/
