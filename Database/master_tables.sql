-- ============================================================
-- Ascent Schools — MASTER DATABASE  (ascent_master)
-- SaaS Control Plane: school groups, schools, users, subscriptions,
--                     module access control
-- This DB is shared across all tenants.
-- ============================================================


-- ============================================================
-- 1. school_groups
--    One row per paying client (society / trust / chain)
--    subdomain uniquely identifies the tenant at login
-- ============================================================
CREATE TABLE school_groups (
    group_id        INT             NOT NULL IDENTITY(1,1),
    group_name      VARCHAR(100)    NOT NULL,
    subdomain       VARCHAR(50)     NOT NULL,   -- e.g. "srividya" → srividya.edu-care.in
    db_name         VARCHAR(100)    NULL,        -- e.g. "ascent_group_1" — set manually after DB creation
    login_code      VARCHAR(10)     NULL,        -- 4-digit code parents enter in the single (multi-school) app; set via control app
    description     VARCHAR(200)    NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_school_groups    PRIMARY KEY (group_id),
    CONSTRAINT UQ_school_groups_subdomain UNIQUE (subdomain)
);
GO

-- Unique login_code among groups that have one (NULLs allowed)
CREATE UNIQUE INDEX UQ_school_groups_login_code
    ON school_groups (login_code) WHERE login_code IS NOT NULL;
GO


-- ============================================================
-- 2. schools
--    Individual branches under a group
-- ============================================================
CREATE TABLE schools (
    school_id                       INT             NOT NULL IDENTITY(1,1),
    group_id                        INT             NOT NULL,
    school_name                     VARCHAR(150)    NOT NULL,
    school_caption                  VARCHAR(250)    NULL,
    address                         VARCHAR(205)    NULL,
    city                            VARCHAR(55)     NULL,
    district                        VARCHAR(55)     NULL,
    state                           VARCHAR(55)     NULL,
    pin_code                        VARCHAR(15)     NULL,
    landline                        VARCHAR(25)     NULL,
    mobile                          VARCHAR(35)     NULL,
    day_end_notification_mobiles    VARCHAR(66)     NULL,   -- Day-end SMS to admin persons
    state_reg_no                    VARCHAR(150)    NULL,
    central_reg_no                  VARCHAR(150)    NULL,
    email                           VARCHAR(150)    NULL,
    website                         VARCHAR(100)    NULL,
    organization_type               VARCHAR(20)     NULL,
    license_purchase_date           DATE            NULL,
    license_renewal_date            DATE            NULL,
    student_strength                INT             NULL,
    staff_strength                  INT             NULL,
    status                          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    created_by                      VARCHAR(25)     NULL,
    created_at                      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id                      VARCHAR(20)     NULL,
    CONSTRAINT PK_schools           PRIMARY KEY (school_id),
    CONSTRAINT FK_schools_group     FOREIGN KEY (group_id) REFERENCES school_groups(group_id)
);
GO


-- ============================================================
-- 3. subscriptions
--    SaaS plan per school group
-- ============================================================
CREATE TABLE subscriptions (
    subscription_id INT             NOT NULL IDENTITY(1,1),
    group_id        INT             NOT NULL,
    plan_name       VARCHAR(30)     NOT NULL,   -- Basic / Standard / Premium
    billing_cycle   VARCHAR(10)     NULL,        -- Monthly / Yearly
    amount          DECIMAL(12,2)   NULL,
    max_schools     INT             NULL,        -- Max branches allowed under this plan
    max_students    INT             NULL,        -- Max students allowed
    start_date      DATE            NOT NULL,
    expiry_date     DATE            NOT NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',  -- Active / Expired / Suspended
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_subscriptions         PRIMARY KEY (subscription_id),
    CONSTRAINT FK_subscriptions_group   FOREIGN KEY (group_id) REFERENCES school_groups(group_id)
);
GO


-- ============================================================
-- 4. master_audit_logs
--    Tracks SaaS-level events: logins, subscription changes,
--    school creation — not row-level data changes (those are in tenant DB)
--    NOTE: user_id FK added after users table is created below
-- ============================================================
CREATE TABLE master_audit_logs (
    log_id              INT             NOT NULL IDENTITY(1,1),
    reference_id        VARCHAR(20)     NULL,   -- ID of the record affected
    reference_type      VARCHAR(30)     NULL,   -- 'login' / 'subscription' / 'school' etc.
    transaction_type    VARCHAR(15)     NULL,   -- Insert / Update / Delete / Login
    transaction_time    DATETIME        NOT NULL DEFAULT GETDATE(),
    user_id             INT             NULL,   -- FK → users.user_id (added after users)
    system_ip           VARCHAR(15)     NULL,
    form_name           VARCHAR(30)     NULL,
    group_id            INT             NULL,
    school_id           INT             NULL,
    CONSTRAINT PK_master_audit_logs         PRIMARY KEY (log_id),
    CONSTRAINT FK_master_audit_logs_group   FOREIGN KEY (group_id)  REFERENCES school_groups(group_id),
    CONSTRAINT FK_master_audit_logs_school  FOREIGN KEY (school_id) REFERENCES schools(school_id)
);
GO


-- ============================================================
-- 5. control_users
--    Ascent internal staff only — for the SaaS control/admin app.
--    School staff users are managed separately in each tenant DB.
--    Simple role column is sufficient here — no complex RBAC needed.
-- ============================================================
CREATE TABLE control_users (
    user_id         INT             NOT NULL IDENTITY(1,1),
    full_name       VARCHAR(100)    NOT NULL,
    username        VARCHAR(50)     NOT NULL,
    password_hash   VARCHAR(255)    NOT NULL,
    role            VARCHAR(20)     NOT NULL,   -- super_admin / support / billing
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    last_login_at   DATETIME        NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_control_users         PRIMARY KEY (user_id),
    CONSTRAINT UQ_control_users_username UNIQUE (username)
);
GO

-- Back-fill FK: master_audit_logs → control_users
ALTER TABLE master_audit_logs
    ADD CONSTRAINT FK_master_audit_logs_user
    FOREIGN KEY (user_id) REFERENCES control_users(user_id);
GO


-- ============================================================
-- 6. refresh_tokens  (control app)
--    Stores hashed refresh tokens for control_users.
--    HttpOnly cookie holds the raw token; DB holds the hash for validation.
-- ============================================================
CREATE TABLE refresh_tokens (
    token_id        INT             NOT NULL IDENTITY(1,1),
    user_id         INT             NOT NULL,
    token_hash      VARCHAR(255)    NOT NULL,   -- SHA-256 hash of the raw token
    expires_at      DATETIME        NOT NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    revoked_at      DATETIME        NULL,       -- NULL = still valid
    replaced_by     VARCHAR(255)    NULL,       -- hash of the new token (on refresh)
    CONSTRAINT PK_refresh_tokens        PRIMARY KEY (token_id),
    CONSTRAINT FK_refresh_tokens_user   FOREIGN KEY (user_id) REFERENCES control_users(user_id)
);
GO


-- ============================================================
-- 7. school_settings
--    One row per school branch — app configuration
-- ============================================================
CREATE TABLE school_settings (
    school_id                       INT             NOT NULL,
    fee_receipt_lock                INT             NULL,
    fee_receipt_print               VARCHAR(10)     NULL,
    print_mode                      VARCHAR(25)     NULL,
    admission_no_type               VARCHAR(15)     NULL,
    receipt_printer                 VARCHAR(30)     NULL,
    reports_printer                 VARCHAR(30)     NULL,
    category_wise_admissions        VARCHAR(5)      NULL,   -- Y / N
    transport_fee_included          VARCHAR(5)      NULL,   -- Y / N
    fine_enabled                    VARCHAR(5)      NULL,   -- Y / N
    receipt_fee_type_separator      VARCHAR(5)      NULL,
    new_student_entry_mode          VARCHAR(10)     NULL,   -- Fast Entry / Normal Entry
    misc_print_mode                 VARCHAR(25)     NULL,
    backup_drive                    VARCHAR(25)     NULL,
    receipt_print_copies            INT             NULL,
    progress_report_type            VARCHAR(25)     NULL,
    billing_status                  VARCHAR(10)     NULL,
    other_subjects_type             VARCHAR(10)     NULL,
    pre_primary_admission_prefix    VARCHAR(15)     NULL,
    primary_admission_prefix        VARCHAR(15)     NULL,
    high_school_admission_prefix    VARCHAR(15)     NULL,
    general_admission_prefix        VARCHAR(15)     NULL,
    bill_no_series_type             VARCHAR(25)     NULL,   -- New Year/Academic Year/Financial Year/Continues/Cash-Bank/Category-wise
    webcam_name                     VARCHAR(25)     NULL,
    institution_head_signature      VARCHAR(255)    NULL,   -- File path / URL
    institution_head_name           VARCHAR(20)     NULL,
    fee_message_to_teacher          VARCHAR(5)      NULL,   -- Y / N
    student_concession_enabled      VARCHAR(25)     NULL,   -- Enable / Disable
    created_by                      VARCHAR(25)     NULL,
    created_at                      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id                      VARCHAR(20)     NULL,
    CONSTRAINT PK_school_settings       PRIMARY KEY (school_id),
    CONSTRAINT FK_school_settings_school FOREIGN KEY (school_id) REFERENCES schools(school_id)
);
GO


-- ============================================================
-- 8. modules
--    Master list of all features/modules available in the system.
--    plan_tier controls which subscription plan unlocks the module.
--    Seeded with known modules — new modules added here as system grows.
-- ============================================================
CREATE TABLE modules (
    module_id       INT             NOT NULL IDENTITY(1,1),
    module_name     VARCHAR(50)     NOT NULL,   -- Display name: "Student Fee"
    module_code     VARCHAR(30)     NOT NULL,   -- API key: "STUDENT_FEE" — used in middleware
    description     VARCHAR(100)    NULL,
    plan_tier       VARCHAR(10)     NOT NULL DEFAULT 'Basic', -- Basic / Standard / Premium
    display_order   INT             NULL,        -- UI menu ordering
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_modules            PRIMARY KEY (module_id),
    CONSTRAINT UQ_modules_code       UNIQUE (module_code)
);
GO

-- Seed: known modules
INSERT INTO modules (module_name, module_code, description, plan_tier, display_order) VALUES
('Student Profile',     'STUDENT_PROFILE',   'Student registration and profile management',  'Basic',    1),
('Student Fee',         'STUDENT_FEE',       'Fee structure, collection and receipts',        'Basic',    2),
('Student Attendance',  'ATTENDANCE',        'Daily attendance tracking',                     'Basic',    3),
('Student Marks',       'MARKS',             'Exam marks and report cards',                   'Standard', 4),
('Library',             'LIBRARY',           'Book issuance and library management',          'Standard', 5),
('Transport',           'TRANSPORT',         'Bus routes, fee and student transport mapping', 'Basic',    6),
('Hostel',              'HOSTEL',            'Hostel room allocation and management',         'Premium',  7);
GO


-- ============================================================
-- 9. group_modules
--    Controls which modules are enabled for each school group.
--    Managed by SaaS admin (you).
--    A module disabled here cannot be enabled at branch level.
-- ============================================================
CREATE TABLE group_modules (
    group_module_id INT             NOT NULL IDENTITY(1,1),
    group_id        INT             NOT NULL,
    module_id       INT             NOT NULL,
    is_enabled      BIT             NOT NULL DEFAULT 1,
    remarks         VARCHAR(100)    NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_by      VARCHAR(25)     NULL,
    updated_at      DATETIME        NULL,
    CONSTRAINT PK_group_modules             PRIMARY KEY (group_module_id),
    CONSTRAINT UQ_group_modules_pair        UNIQUE (group_id, module_id),
    CONSTRAINT FK_group_modules_group       FOREIGN KEY (group_id)  REFERENCES school_groups(group_id),
    CONSTRAINT FK_group_modules_module      FOREIGN KEY (module_id) REFERENCES modules(module_id)
);
GO


-- ============================================================
-- 10. school_modules
--    Branch-level module overrides.
--    Managed by the group admin.
--    Can only DISABLE a module that the group has enabled.
--    Cannot ENABLE a module that the group has disabled.
--    If no row exists for a branch, it inherits the group-level setting.
-- ============================================================
CREATE TABLE school_modules (
    school_module_id    INT             NOT NULL IDENTITY(1,1),
    school_id           INT             NOT NULL,
    module_id           INT             NOT NULL,
    is_enabled          BIT             NOT NULL DEFAULT 1,
    remarks             VARCHAR(100)    NULL,
    created_by          VARCHAR(25)     NULL,
    created_at          DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_by          VARCHAR(25)     NULL,
    updated_at          DATETIME        NULL,
    CONSTRAINT PK_school_modules            PRIMARY KEY (school_module_id),
    CONSTRAINT UQ_school_modules_pair       UNIQUE (school_id, module_id),
    CONSTRAINT FK_school_modules_school     FOREIGN KEY (school_id)  REFERENCES schools(school_id),
    CONSTRAINT FK_school_modules_module     FOREIGN KEY (module_id)  REFERENCES modules(module_id)
);
GO


-- ============================================================
-- 11. group_branding
--     Default branding for all branches under a school group.
--     Applied when a branch has no branch-level branding row.
-- ============================================================
CREATE TABLE group_branding (
    branding_id         INT             NOT NULL IDENTITY(1,1),
    group_id            INT             NOT NULL,
    logo_path           VARCHAR(255)    NULL,       -- URL or file path to group logo
    favicon_path        VARCHAR(255)    NULL,
    primary_color       VARCHAR(7)      NULL,       -- hex e.g. #1A73E8
    secondary_color     VARCHAR(7)      NULL,
    header_bg_color     VARCHAR(7)      NULL,
    nav_text_color      VARCHAR(7)      NULL,
    display_name        VARCHAR(150)    NULL,       -- group display name override
    tagline             VARCHAR(250)    NULL,       -- shown on login page
    receipt_footer_text VARCHAR(300)    NULL,       -- printed on fee receipts / reports
    login_bg_path       VARCHAR(255)    NULL,       -- custom login page background image
    created_by          VARCHAR(25)     NULL,
    created_at          DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_by          VARCHAR(25)     NULL,
    updated_at          DATETIME        NULL,
    CONSTRAINT PK_group_branding        PRIMARY KEY (branding_id),
    CONSTRAINT UQ_group_branding_group  UNIQUE (group_id),
    CONSTRAINT FK_group_branding_group  FOREIGN KEY (group_id) REFERENCES school_groups(group_id)
);
GO


-- ============================================================
-- 12. school_branding
--     Branch-level branding — overrides group_branding when set.
--     Loaded via public /branding endpoint before login (no auth needed).
--     Hierarchy: school_branding → fallback → group_branding.
-- ============================================================
CREATE TABLE school_branding (
    branding_id         INT             NOT NULL IDENTITY(1,1),
    school_id           INT             NOT NULL,
    logo_path           VARCHAR(255)    NULL,       -- URL or file path to school logo
    favicon_path        VARCHAR(255)    NULL,
    primary_color       VARCHAR(7)      NULL,       -- hex e.g. #0D47A1
    secondary_color     VARCHAR(7)      NULL,
    header_bg_color     VARCHAR(7)      NULL,
    nav_text_color      VARCHAR(7)      NULL,
    display_name        VARCHAR(150)    NULL,       -- branch display name override
    tagline             VARCHAR(250)    NULL,       -- shown on login page below logo
    receipt_footer_text VARCHAR(300)    NULL,       -- printed on fee receipts / reports
    login_bg_path       VARCHAR(255)    NULL,       -- custom login page background image
    created_by          VARCHAR(25)     NULL,
    created_at          DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_by          VARCHAR(25)     NULL,
    updated_at          DATETIME        NULL,
    CONSTRAINT PK_school_branding        PRIMARY KEY (branding_id),
    CONSTRAINT UQ_school_branding_school UNIQUE (school_id),
    CONSTRAINT FK_school_branding_school FOREIGN KEY (school_id) REFERENCES schools(school_id)
);
GO


-- ============================================================
-- 13. parent_accounts
--     One row per parent/guardian using the mobile app.
--     A parent can have children across different school groups.
--     PIN is SHA-256 + Base64 hashed (same as JwtHelper.HashRefreshToken).
-- ============================================================
CREATE TABLE parent_accounts (
    parent_id       INT             NOT NULL IDENTITY(1,1),
    full_name       VARCHAR(100)    NOT NULL,
    mobile          VARCHAR(20)     NULL,
    email           VARCHAR(100)    NULL,
    pin_hash        VARCHAR(255)    NOT NULL,
    otp_code        VARCHAR(10)     NULL,
    otp_expires_at  DATETIME        NULL,
    is_active       BIT             NOT NULL DEFAULT 1,
    last_login_at   DATETIME        NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_parent_accounts           PRIMARY KEY (parent_id),
    CONSTRAINT UQ_parent_accounts_mobile    UNIQUE (mobile)
    -- email uniqueness enforced by filtered index below (allows multiple NULLs)
);
GO
-- Filtered unique index: enforces uniqueness only for non-NULL emails
CREATE UNIQUE INDEX UX_parent_accounts_email ON parent_accounts(email) WHERE email IS NOT NULL;
GO


-- ============================================================
-- 14. parent_children
--     Links a parent account to one or more students.
--     student_id references the tenant DB; db_name + school_id
--     are cached here so data requests route to the right tenant.
-- ============================================================
CREATE TABLE parent_children (
    link_id         INT             NOT NULL IDENTITY(1,1),
    parent_id       INT             NOT NULL,
    student_id      BIGINT          NOT NULL,
    group_id        INT             NOT NULL,
    db_name         VARCHAR(100)    NOT NULL,
    school_id       INT             NOT NULL,
    student_name    VARCHAR(105)    NULL,
    class_name      VARCHAR(50)     NULL,
    admission_no    VARCHAR(20)     NULL,
    is_active       BIT             NOT NULL DEFAULT 1,
    linked_at       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_parent_children           PRIMARY KEY (link_id),
    CONSTRAINT UQ_parent_children_pair      UNIQUE (parent_id, student_id, group_id),
    CONSTRAINT FK_parent_children_parent    FOREIGN KEY (parent_id) REFERENCES parent_accounts(parent_id),
    CONSTRAINT FK_parent_children_group     FOREIGN KEY (group_id)  REFERENCES school_groups(group_id)
);
GO


-- ============================================================
-- 15. parent_refresh_tokens
--     Sliding-expiry refresh tokens for parent mobile sessions.
-- ============================================================
CREATE TABLE parent_refresh_tokens (
    token_id        INT             NOT NULL IDENTITY(1,1),
    parent_id       INT             NOT NULL,
    token_hash      VARCHAR(255)    NOT NULL,
    expires_at      DATETIME        NOT NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    revoked_at      DATETIME        NULL,
    replaced_by     VARCHAR(255)    NULL,
    CONSTRAINT PK_parent_refresh_tokens         PRIMARY KEY (token_id),
    CONSTRAINT FK_parent_refresh_tokens_parent  FOREIGN KEY (parent_id) REFERENCES parent_accounts(parent_id)
);
GO


-- ============================================================
-- 16. app_config
--     Drives the mobile force/soft update gate (Option 3).
--     The Android app sends applicationId + versionCode to
--     GET /mobile/app/config on launch; the server compares
--     against the matching row (or the '*' fallback) and tells
--     the app whether an update is REQUIRED or AVAILABLE.
--     Change min_supported_version_code here to force-update
--     all installed apps — no API redeploy needed.
-- ============================================================
CREATE TABLE app_config (
    config_id                   INT             NOT NULL IDENTITY(1,1),
    application_id              VARCHAR(100)    NOT NULL,   -- e.g. 'in.educare.app'; '*' = default fallback
    platform                   VARCHAR(10)     NOT NULL DEFAULT 'android',
    min_supported_version_code  INT             NOT NULL DEFAULT 1,   -- app below this  -> FORCE update
    latest_version_code         INT             NOT NULL DEFAULT 1,   -- app below this (>= min) -> SOFT update
    update_message             NVARCHAR(500)   NULL,
    store_url                  VARCHAR(300)    NULL,                  -- optional override; else app builds market:// link
    auto_update_enabled        BIT             NOT NULL DEFAULT 0,    -- 0 = never auto-prompt; 1 = prompt via version check
    updated_at                 DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_app_config          PRIMARY KEY (config_id),
    CONSTRAINT UQ_app_config_app_plat UNIQUE (application_id, platform)
);
GO

-- Seed one row per published applicationId + a '*' fallback (arms the gate, forces nobody)
INSERT INTO app_config (application_id, platform, min_supported_version_code, latest_version_code)
VALUES
    ('*',                     'android', 1, 12),
    ('in.educare.app',        'android', 1, 12),
    ('in.educare.stannsasf',  'android', 1, 12),
    ('in.educare.holyspiritjm','android', 1, 12),
    ('in.educare.depaulemyv', 'android', 1, 12),
    ('in.educare.demo',       'android', 1, 12);
GO


-- ============================================================
-- 17. device_push_tokens
--     FCM registration tokens for push notifications (Feature 3).
--     One row per device install. A parent OR a teacher may own a
--     token (a device that switches user re-binds via upsert on
--     fcm_token). Teacher tokens carry the tenant db_name so future
--     teacher-targeted pushes can resolve the branch.
-- ============================================================
CREATE TABLE device_push_tokens (
    token_id        INT             NOT NULL IDENTITY(1,1),
    fcm_token       VARCHAR(4000)   NOT NULL,
    user_type       VARCHAR(10)     NOT NULL,            -- 'parent' | 'teacher'
    parent_id       INT             NULL,                -- parent_accounts.parent_id (parent)
    teacher_user_id INT             NULL,                -- tenant users.user_id (teacher)
    group_id        INT             NULL,
    school_id       INT             NULL,
    db_name         VARCHAR(100)    NULL,                -- tenant DB (teacher token resolution)
    application_id  VARCHAR(100)    NULL,                -- e.g. 'in.educare.stannsasf'
    platform        VARCHAR(10)     NOT NULL DEFAULT 'android',
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_device_push_tokens PRIMARY KEY (token_id)
);
GO
-- One row per FCM token (upsert target when a device re-registers / switches user)
CREATE UNIQUE INDEX UQ_device_push_tokens_token ON device_push_tokens (fcm_token);
GO
-- Fast lookup when fanning out to a parent's devices
CREATE INDEX IX_device_push_tokens_parent ON device_push_tokens (parent_id) WHERE parent_id IS NOT NULL;
GO
