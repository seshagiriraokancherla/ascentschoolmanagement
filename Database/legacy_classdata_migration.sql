-- ============================================================
-- legacy_classdata_migration.sql
-- Run against ascent_group_{N} (tenant DB)
-- Replace @SchoolId with the actual school_id from ascent_master.schools
-- ============================================================

DECLARE @SchoolId INT = 7   -- ← replace with your school_id

-- ── 1. Class Groups ──────────────────────────────────────────
-- Using IDENTITY_INSERT so class_group_ids are predictable (1-4)
-- and the classes inserts below can reference them directly.

SET IDENTITY_INSERT class_groups ON

INSERT INTO class_groups (class_group_id, group_name, prefix, status, school_id)
VALUES
(1, 'Pre-Primary', 'KFR', 'Active', @SchoolId),
(2, 'Primary',     'PFR', 'Active', @SchoolId),
(3, 'High School', 'HFR', 'Active', @SchoolId),
(4, 'General',     'FBN', 'Active', @SchoolId)

SET IDENTITY_INSERT class_groups OFF

-- ── 2. Classes ───────────────────────────────────────────────
-- Sequence numbers reassigned to be unique within each group.
-- "8 class" (duplicate of "8 Class") dropped.
-- description populated from legacy classdesc where present.

INSERT INTO classes (class_name, class_group_id, description, sequence_no, school_id, status)
VALUES

-- Pre-Primary (class_group_id = 1)
('Pre-KG',               1, NULL,   1,  @SchoolId, 'Active'),
('LKG',                  1, 'ICSE', 2,  @SchoolId, 'Active'),
('UKG',                  1, 'ICSE', 3,  @SchoolId, 'Active'),

-- Primary (class_group_id = 2)
('1 Class',              2, NULL,   1,  @SchoolId, 'Active'),
('2 Class',              2, NULL,   2,  @SchoolId, 'Active'),
('3 Class',              2, NULL,   3,  @SchoolId, 'Active'),
('4 Class',              2, NULL,   4,  @SchoolId, 'Active'),
('5 Class',              2, NULL,   5,  @SchoolId, 'Active'),
('6 Class',              2, NULL,   6,  @SchoolId, 'Active'),
('7 Class',              2, NULL,   7,  @SchoolId, 'Active'),
('8 Class',              2, NULL,   8,  @SchoolId, 'Active'),
('8 ICSE',               2, 'ICSE', 9,  @SchoolId, 'Active'),
('9 Class',              2, 'ICSE', 10, @SchoolId, 'Active'),
('9 ICSE',               2, 'ICSE', 11, @SchoolId, 'Active'),
('10 Class',             2, 'ICSE', 12, @SchoolId, 'Active'),

-- High School (class_group_id = 3)
-- No classes in legacy data mapped to this group.
-- Add here if needed later.

-- General (class_group_id = 4)
('PCMB 1st Year',        4, NULL,   1,  @SchoolId, 'Active'),
('PCMB 2nd Year',        4, NULL,   2,  @SchoolId, 'Active'),
('PCMC 1st Year',        4, NULL,   3,  @SchoolId, 'Active'),
('PCMC 2nd Year',        4, NULL,   4,  @SchoolId, 'Active'),
('HEBA 1st Year',        4, NULL,   5,  @SchoolId, 'Active'),
('HEBA 2nd Year',        4, NULL,   6,  @SchoolId, 'Active'),
('CEBA 1st Year',        4, NULL,   7,  @SchoolId, 'Active'),
('CEBA 2nd Year',        4, NULL,   8,  @SchoolId, 'Active'),
('XI th Grade',          4, 'State', 9,  @SchoolId, 'Active'),
('XI th Grade Arts',     4, 'State', 10, @SchoolId, 'Active'),
('XI th Grade Science',  4, 'State', 11, @SchoolId, 'Active'),
('XII th Grade Arts',    4, 'State', 12, @SchoolId, 'Active'),
('XII th Grade Science', 4, 'State', 13, @SchoolId, 'Active')

-- ── 3. Verify ────────────────────────────────────────────────
SELECT
    cg.group_name,
    c.class_name,
    c.sequence_no,
    c.description,
    c.status
FROM classes c
JOIN class_groups cg ON cg.class_group_id = c.class_group_id
WHERE c.school_id = @SchoolId
ORDER BY cg.class_group_id, c.sequence_no
