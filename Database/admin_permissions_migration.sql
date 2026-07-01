-- ============================================================
-- admin_permissions_migration.sql
-- Run against each existing ascent_group_{N} tenant DB.
--
-- Adds the new admin / settings / supporting-module permission codes
-- introduced for page-level access control (Phase A), and maps them to
-- the default seeded roles. Fully idempotent — safe to re-run.
--
-- NOTE: roles are matched by role_name only (NOT "school_id IS NULL").
-- Some DBs have the default roles (Principal, Admin Clerk, ...) created
-- with a non-NULL school_id (e.g. migrated DBs); the old "school_id IS NULL"
-- filter matched nothing there, so NO permissions ever got mapped and users
-- saw only the Dashboard. Matching by name maps every same-named role row.
--
-- IMPORTANT: After running this, users must LOG OUT and LOG IN again
-- (or wait for their access token to refresh) so the new permissions
-- appear in their JWT and the school app shows the gated pages/nav.
-- ============================================================

-- ── 1. Insert the new permissions (skip any already present) ──────────────
INSERT INTO permissions (permission_code, module_code, description)
SELECT v.permission_code, v.module_code, v.description
FROM (VALUES
    ('MASTER_DATA.VIEW',    'MASTER_DATA',  'View master data'),
    ('MASTER_DATA.MANAGE',  'MASTER_DATA',  'Manage master data'),
    ('HOMEWORK.VIEW',       'HOMEWORK',     'View homework'),
    ('HOMEWORK.MANAGE',     'HOMEWORK',     'Create / edit homework'),
    ('ANNOUNCEMENT.VIEW',   'ANNOUNCEMENT', 'View announcements'),
    ('ANNOUNCEMENT.MANAGE', 'ANNOUNCEMENT', 'Create / edit announcements'),
    ('EVENTS.VIEW',         'EVENTS',       'View events gallery'),
    ('EVENTS.MANAGE',       'EVENTS',       'Manage events gallery'),
    ('STAFF.VIEW',          'STAFF',        'View staff'),
    ('STAFF.MANAGE',        'STAFF',        'Manage staff, attendance and salaries'),
    ('REPORTS.VIEW',        'REPORTS',      'View reports'),
    ('SMS.SEND',            'SMS',          'Send SMS and view history'),
    ('SETTINGS.MANAGE',     'SETTINGS',     'Manage school settings and payment gateway'),
    ('USER_MGMT.MANAGE',    'USER_MGMT',    'Manage roles, permissions and users')
) AS v(permission_code, module_code, description)
WHERE NOT EXISTS (
    SELECT 1 FROM permissions p WHERE p.permission_code = v.permission_code
);

PRINT 'New permissions inserted (existing ones skipped).';
GO

-- ── 2. Principal: gets ALL permissions (including the new ones) ────────────
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Principal'
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  );

-- ── 3. Admin Clerk: full intended set (original + new) ─────────────────────
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Admin Clerk'
  AND p.permission_code IN (
      'STUDENT_PROFILE.VIEW', 'STUDENT_PROFILE.CREATE', 'STUDENT_PROFILE.EDIT',
      'STUDENT_FEE.VIEW',
      'ATTENDANCE.VIEW',
      'MARKS.VIEW',
      'TRANSPORT.VIEW',
      'MASTER_DATA.VIEW',
      'HOMEWORK.VIEW', 'HOMEWORK.MANAGE',
      'ANNOUNCEMENT.VIEW', 'ANNOUNCEMENT.MANAGE',
      'EVENTS.VIEW', 'EVENTS.MANAGE',
      'STAFF.VIEW',
      'REPORTS.VIEW',
      'SMS.SEND'
  )
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  );

-- ── 4. Fee Clerk: full intended set (original + new) ───────────────────────
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Fee Clerk'
  AND p.permission_code IN (
      'STUDENT_PROFILE.VIEW',
      'STUDENT_FEE.VIEW', 'STUDENT_FEE.COLLECT', 'STUDENT_FEE.CANCEL_RECEIPT', 'STUDENT_FEE.CONCESSION',
      'REPORTS.VIEW'
  )
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  );

-- ── 5. Class Teacher: full intended set (original + new) ───────────────────
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Class Teacher'
  AND p.permission_code IN (
      'STUDENT_PROFILE.VIEW',
      'STUDENT_FEE.VIEW',
      'ATTENDANCE.VIEW', 'ATTENDANCE.MARK', 'ATTENDANCE.EDIT',
      'MARKS.VIEW', 'MARKS.ENTER', 'MARKS.EDIT',
      'HOMEWORK.VIEW', 'HOMEWORK.MANAGE',
      'ANNOUNCEMENT.VIEW',
      'EVENTS.VIEW',
      'REPORTS.VIEW'
  )
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  );

-- ── 6. Librarian: full intended set ────────────────────────────────────────
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Librarian'
  AND p.permission_code IN (
      'STUDENT_PROFILE.VIEW',
      'LIBRARY.VIEW', 'LIBRARY.ISSUE', 'LIBRARY.RETURN', 'LIBRARY.MANAGE'
  )
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  );

PRINT 'Permissions mapped to default roles (full intended sets).';
PRINT 'Reminder: users must re-login for the new permissions to take effect.';
GO
