-- ============================================================
-- messages_view_permission_migration.sql
-- Run against each existing ascent_group_{N} tenant DB.
--
-- Adds the MESSAGES.VIEW permission (read-only admin viewer of parent-teacher
-- conversations in the school web app) and maps it to the Principal role.
-- Fully idempotent — safe to re-run.
--
-- NOTE: roles are matched by role_name only (some DBs have the default roles
-- created with a non-NULL school_id — see admin_permissions_migration.sql).
--
-- IMPORTANT: After running this, any user who should see the Conversations page
-- must LOG OUT and LOG IN again so the new permission appears in their JWT.
-- Assign MESSAGES.VIEW to additional roles via Settings -> Roles & Permissions.
-- ============================================================

-- ── 1. Insert the permission (skip if already present) ────────────────────
INSERT INTO permissions (permission_code, module_code, description)
SELECT 'MESSAGES.VIEW', 'MESSAGES', 'View parent-teacher conversations (read-only)'
WHERE NOT EXISTS (
    SELECT 1 FROM permissions p WHERE p.permission_code = 'MESSAGES.VIEW'
);

PRINT 'MESSAGES.VIEW permission inserted (skipped if already present).';
GO

-- ── 2. Map it to Principal ────────────────────────────────────────────────
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.role_id, p.permission_id
FROM roles r
CROSS JOIN permissions p
WHERE r.role_name = 'Principal'
  AND p.permission_code = 'MESSAGES.VIEW'
  AND NOT EXISTS (
      SELECT 1 FROM role_permissions rp
      WHERE rp.role_id = r.role_id AND rp.permission_id = p.permission_id
  );

PRINT 'MESSAGES.VIEW mapped to Principal.';
PRINT 'Reminder: users must re-login for the new permission to take effect.';
GO
