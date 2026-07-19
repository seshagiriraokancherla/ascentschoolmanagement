-- ============================================================
-- Ascent Schools — TENANT DATABASE  (ascent_group_{group_id})
-- One database per school group / society
-- school_id is kept on all tables to filter by branch within the group
-- group_id is NOT needed here — it is implied by the database itself
--
-- RBAC is fully managed here — each school group controls their own
-- roles, permissions and staff users independently.
-- ============================================================


-- ============================================================
-- 1. permissions
--    Granular permission codes — what actions can be performed.
--    Tied to module_code for consistency with master DB modules table.
--    Seeded with default permissions; groups can add custom ones.
--    Format: MODULE_CODE.ACTION  e.g. STUDENT_FEE.COLLECT
-- ============================================================
CREATE TABLE permissions (
    permission_id   INT             NOT NULL IDENTITY(1,1),
    permission_code VARCHAR(50)     NOT NULL,   -- e.g. STUDENT_FEE.COLLECT
    module_code     VARCHAR(30)     NOT NULL,   -- matches modules.module_code in master DB
    description     VARCHAR(100)    NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    CONSTRAINT PK_permissions           PRIMARY KEY (permission_id),
    CONSTRAINT UQ_permissions_code      UNIQUE (permission_code)
);
GO

-- Seed: default permissions aligned with known modules
INSERT INTO permissions (permission_code, module_code, description) VALUES
-- Student Profile
('STUDENT_PROFILE.VIEW',        'STUDENT_PROFILE',  'View student profile'),
('STUDENT_PROFILE.CREATE',      'STUDENT_PROFILE',  'Register new student'),
('STUDENT_PROFILE.EDIT',        'STUDENT_PROFILE',  'Edit student details'),
('STUDENT_PROFILE.DELETE',      'STUDENT_PROFILE',  'Delete / TC student'),
-- Student Fee
('STUDENT_FEE.VIEW',            'STUDENT_FEE',      'View fee details'),
('STUDENT_FEE.COLLECT',         'STUDENT_FEE',      'Collect fee and issue receipt'),
('STUDENT_FEE.EDIT',            'STUDENT_FEE',      'Edit fee structure'),
('STUDENT_FEE.CANCEL_RECEIPT',  'STUDENT_FEE',      'Cancel a fee receipt'),
('STUDENT_FEE.CONCESSION',      'STUDENT_FEE',      'Apply fee concession'),
-- Attendance
('ATTENDANCE.VIEW',             'ATTENDANCE',       'View attendance'),
('ATTENDANCE.MARK',             'ATTENDANCE',       'Mark daily attendance'),
('ATTENDANCE.EDIT',             'ATTENDANCE',       'Correct past attendance'),
-- Marks
('MARKS.VIEW',                  'MARKS',            'View marks and report cards'),
('MARKS.ENTER',                 'MARKS',            'Enter exam marks'),
('MARKS.EDIT',                  'MARKS',            'Edit submitted marks'),
('MARKS.PUBLISH',               'MARKS',            'Publish report cards'),
-- Library
('LIBRARY.VIEW',                'LIBRARY',          'View library records'),
('LIBRARY.ISSUE',               'LIBRARY',          'Issue books to students'),
('LIBRARY.RETURN',              'LIBRARY',          'Process book returns'),
('LIBRARY.MANAGE',              'LIBRARY',          'Add / edit book catalogue'),
-- Transport
('TRANSPORT.VIEW',              'TRANSPORT',        'View transport details'),
('TRANSPORT.MANAGE',            'TRANSPORT',        'Manage buses, routes and fees'),
-- Hostel
('HOSTEL.VIEW',                 'HOSTEL',           'View hostel details'),
('HOSTEL.MANAGE',               'HOSTEL',           'Manage hostel rooms and allocation'),
-- Master Data
('MASTER_DATA.VIEW',            'MASTER_DATA',      'View master data'),
('MASTER_DATA.MANAGE',          'MASTER_DATA',      'Manage master data'),
-- Homework
('HOMEWORK.VIEW',               'HOMEWORK',         'View homework'),
('HOMEWORK.MANAGE',             'HOMEWORK',         'Create / edit homework'),
-- Announcements
('ANNOUNCEMENT.VIEW',           'ANNOUNCEMENT',     'View announcements'),
('ANNOUNCEMENT.MANAGE',         'ANNOUNCEMENT',     'Create / edit announcements'),
-- Events
('EVENTS.VIEW',                 'EVENTS',           'View events gallery'),
('EVENTS.MANAGE',               'EVENTS',           'Manage events gallery'),
-- Staff
('STAFF.VIEW',                  'STAFF',            'View staff'),
('STAFF.MANAGE',                'STAFF',            'Manage staff, attendance and salaries'),
-- Reports
('REPORTS.VIEW',                'REPORTS',          'View reports'),
-- SMS
('SMS.SEND',                    'SMS',              'Send SMS and view history'),
-- Settings
('SETTINGS.MANAGE',             'SETTINGS',         'Manage school settings and payment gateway'),
-- User Management
('USER_MGMT.MANAGE',            'USER_MGMT',        'Manage roles, permissions and users');
GO


-- ============================================================
-- 2. roles
--    Roles are defined per school (branch) or group-wide (school_id NULL)
--    e.g. Principal, Admin Clerk, Fee Clerk, Teacher, Librarian
-- ============================================================
CREATE TABLE roles (
    role_id         INT             NOT NULL IDENTITY(1,1),
    role_name       VARCHAR(50)     NOT NULL,
    description     VARCHAR(100)    NULL,
    school_id       INT             NULL,   -- NULL = applies to all branches in this group
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_roles             PRIMARY KEY (role_id),
    CONSTRAINT UQ_roles_per_school  UNIQUE (role_name, school_id)
);
GO

-- Seed: common default roles (school_id NULL = group-wide)
INSERT INTO roles (role_name, description, school_id) VALUES
('Principal',       'Full access to all modules',               NULL),
('Admin Clerk',     'Student profile and general admin',        NULL),
('Fee Clerk',       'Fee collection and receipts only',         NULL),
('Class Teacher',   'Attendance and marks for assigned class',  NULL),
('Librarian',       'Library module access',                    NULL);
GO


-- ============================================================
-- 3. role_permissions
--    Which permissions each role has
-- ============================================================
CREATE TABLE role_permissions (
    role_permission_id  INT     NOT NULL IDENTITY(1,1),
    role_id             INT     NOT NULL,
    permission_id       INT     NOT NULL,
    CONSTRAINT PK_role_permissions          PRIMARY KEY (role_permission_id),
    CONSTRAINT UQ_role_permissions_pair     UNIQUE (role_id, permission_id),
    CONSTRAINT FK_role_permissions_role     FOREIGN KEY (role_id)       REFERENCES roles(role_id),
    CONSTRAINT FK_role_permissions_perm     FOREIGN KEY (permission_id) REFERENCES permissions(permission_id)
);
GO


-- ============================================================
-- 4. users
--    School staff — managed by the school group/branch admin.
--    Username unique within the tenant DB (i.e. within the group).
--    school_id NULL = user has access to all branches in this group.
-- ============================================================
CREATE TABLE users (
    user_id         INT             NOT NULL IDENTITY(1,1),
    school_id       INT             NULL,        -- NULL = all branches; set to branch school_id to restrict
    employee_id     INT             NULL,        -- FK → employee table (TBD)
    username        VARCHAR(50)     NOT NULL,
    password_hash   VARCHAR(255)    NOT NULL,
    full_name       VARCHAR(100)    NULL,
    email           VARCHAR(100)    NULL,
    mobile          VARCHAR(20)     NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    last_login_at   DATETIME        NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_users             PRIMARY KEY (user_id),
    CONSTRAINT UQ_users_username    UNIQUE (username)   -- unique within the group's tenant DB
);
GO


-- ============================================================
-- 5. refresh_tokens  (school app)
--    Stores hashed refresh tokens for school staff users.
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
    CONSTRAINT FK_refresh_tokens_user   FOREIGN KEY (user_id) REFERENCES users(user_id)
);
GO


-- ============================================================
-- 7. user_roles
--    Assigns roles to users, scoped to a specific branch.
--    A user can have different roles in different branches.
--    e.g. same person is Fee Clerk at Branch A, Admin Clerk at Branch B
-- ============================================================
CREATE TABLE user_roles (
    user_role_id    INT     NOT NULL IDENTITY(1,1),
    user_id         INT     NOT NULL,
    role_id         INT     NOT NULL,
    school_id       INT     NOT NULL,   -- Which branch this role assignment applies to
    CONSTRAINT PK_user_roles            PRIMARY KEY (user_role_id),
    CONSTRAINT UQ_user_roles_combo      UNIQUE (user_id, role_id, school_id),
    CONSTRAINT FK_user_roles_user       FOREIGN KEY (user_id)   REFERENCES users(user_id),
    CONSTRAINT FK_user_roles_role       FOREIGN KEY (role_id)   REFERENCES roles(role_id)
);
GO


-- ============================================================
-- 8. audit_logs
--    Row-level change tracking within the tenant.
--    user_id references users table in THIS tenant DB (proper FK now).
-- ============================================================
CREATE TABLE audit_logs (
    log_id              INT             NOT NULL IDENTITY(1,1),
    reference_id        VARCHAR(20)     NULL,   -- ID of the record that changed
    reference_type      VARCHAR(30)     NULL,   -- Table name / entity
    transaction_type    VARCHAR(15)     NULL,   -- Insert / Update / Delete
    transaction_time    DATETIME        NOT NULL DEFAULT GETDATE(),
    user_id             INT             NULL,   -- FK → users.user_id (same tenant DB)
    system_ip           VARCHAR(15)     NULL,
    form_name           VARCHAR(30)     NULL,   -- Screen / module name
    school_id           INT             NULL,   -- Which branch made the change
    CONSTRAINT PK_audit_logs        PRIMARY KEY (log_id),
    CONSTRAINT FK_audit_logs_user   FOREIGN KEY (user_id) REFERENCES users(user_id)
);
GO


-- ============================================================
-- 9. academic_years  (was SAS_AcdYear)
-- ============================================================
CREATE TABLE academic_years (
    academic_year_id            INT             NOT NULL IDENTITY(1,1),
    academic_year               VARCHAR(55)     NOT NULL,   -- "2025-2026" or "25-29 B-Pharm"
    start_month                 VARCHAR(15)     NULL,
    end_month                   VARCHAR(15)     NULL,
    status                      VARCHAR(10)      NULL,
    registration_fee_frequency  VARCHAR(10)     NULL,       -- Yearly / Monthly / Termwise
    transport_fee_frequency     VARCHAR(10)     NULL,
    hostel_fee_frequency        VARCHAR(10)     NULL,
    boarding_type               VARCHAR(10)     NULL,       -- Day School / Hostel
    new_admissions_enabled      VARCHAR(10)     NULL,       -- Enable / Disable
    school_id                   INT             NOT NULL,
    audit_log_id                INT             NULL,
    created_by                  VARCHAR(25)     NULL,
    created_at                  DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id                  VARCHAR(20)     NULL,
    CONSTRAINT PK_academic_years        PRIMARY KEY (academic_year_id),
    CONSTRAINT FK_academic_years_audit  FOREIGN KEY (audit_log_id) REFERENCES audit_logs(log_id)
);
GO


-- ============================================================
-- 10. class_groups  (was SAS_ClassGrps)
--    e.g. Pre-Primary, Primary, High-School, General
-- ============================================================
CREATE TABLE class_groups (
    class_group_id  INT             NOT NULL IDENTITY(1,1),
    group_name      VARCHAR(30)     NOT NULL,   -- Pre-Primary / Primary / High-School / General
    description     VARCHAR(50)     NULL,
    prefix          VARCHAR(5)      NULL,
    status          VARCHAR(10)      NULL,
    school_id       INT             NOT NULL,
    audit_log_id    INT             NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    CONSTRAINT PK_class_groups          PRIMARY KEY (class_group_id),
    CONSTRAINT FK_class_groups_audit    FOREIGN KEY (audit_log_id) REFERENCES audit_logs(log_id)
);
GO


-- ============================================================
-- 11. fee_categories  (was SAS_FeeCategory)
--    e.g. General Students Fee, Staff Child Fee, VTPS Staff Fee
--    Created before classes — classes reference fee_category_id
-- ============================================================
CREATE TABLE fee_categories (
    fee_category_id     INT             NOT NULL IDENTITY(1,1),
    category_name       VARCHAR(25)     NOT NULL,
    academic_year_id    INT             NULL,
    description         VARCHAR(30)     NULL,
    status              VARCHAR(10)      NULL,
    school_id           INT             NOT NULL,
    created_by          VARCHAR(25)     NULL,
    created_at          DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id          VARCHAR(20)     NULL,
    deleted_by          VARCHAR(60)     NULL,
    CONSTRAINT PK_fee_categories PRIMARY KEY (fee_category_id)
    -- FK to academic_years added after academic_years exists; see below
);
GO

ALTER TABLE fee_categories
    ADD CONSTRAINT FK_fee_categories_academic_year
    FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id);
GO


-- ============================================================
-- 12. classes  (was SAS_ClassNames)
-- ============================================================
CREATE TABLE classes (
    class_id        INT             NOT NULL IDENTITY(1,1),
    class_name      VARCHAR(55)     NOT NULL,   -- "1 Class", "XI th Grade Science"
    branch_name     VARCHAR(55)     NULL,        -- Stream: Science / Arts / Commerce
    description     VARCHAR(50)     NULL,
    status          VARCHAR(10)      NULL,
    class_group_id  INT             NULL,
    fee_category_id INT             NULL,        -- Default fee category for this class
    prefix_enabled  VARCHAR(5)      NULL,
    prefix_code     VARCHAR(5)      NULL,
    sequence_no     INT             NULL,
    school_id       INT             NOT NULL,
    audit_log_id    INT             NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    CONSTRAINT PK_classes               PRIMARY KEY (class_id),
    CONSTRAINT FK_classes_class_group   FOREIGN KEY (class_group_id)  REFERENCES class_groups(class_group_id),
    CONSTRAINT FK_classes_fee_category  FOREIGN KEY (fee_category_id) REFERENCES fee_categories(fee_category_id),
    CONSTRAINT FK_classes_audit         FOREIGN KEY (audit_log_id)    REFERENCES audit_logs(log_id)
);
GO


-- ============================================================
-- 13. sections  (school-defined sections per class, e.g. A / B / C)
-- ============================================================
CREATE TABLE sections (
    section_id      INT          NOT NULL IDENTITY(1,1),
    class_id        INT          NOT NULL,
    section_name    VARCHAR(10)  NOT NULL,
    description     VARCHAR(50)  NULL,
    sequence_no     INT          NULL,
    status          VARCHAR(10)   NOT NULL DEFAULT 'Active',
    school_id       INT          NOT NULL,
    created_by      VARCHAR(25)  NULL,
    created_at      DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_sections           PRIMARY KEY (section_id),
    CONSTRAINT FK_sections_class     FOREIGN KEY (class_id)  REFERENCES classes(class_id),
    CONSTRAINT UQ_sections_per_class UNIQUE (class_id, section_name, school_id)
);
GO


-- ============================================================
-- 14. fee_types  (was SAS_FeeTypes)
-- ============================================================
CREATE TABLE fee_types (
    fee_type_id         INT             NOT NULL IDENTITY(1,1),
    fee_type_name       VARCHAR(25)     NOT NULL,
    academic_year_id    INT             NULL,
    term_name           VARCHAR(25)     NULL,
    sequence_no         INT             NULL,
    description         VARCHAR(50)     NULL,
    status              VARCHAR(10)      NULL,
    school_id           INT             NOT NULL,
    created_by          VARCHAR(25)     NULL,
    created_at          DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id          VARCHAR(20)     NULL,
    deleted_by          VARCHAR(60)     NULL,
    CONSTRAINT PK_fee_types             PRIMARY KEY (fee_type_id),
    CONSTRAINT FK_fee_types_academic_year FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
);
GO


-- ============================================================
-- 14. terms  (was SAS_TermMonthData)
--    e.g. "1st Term", "2nd Term", "January-2025"
-- ============================================================
CREATE TABLE terms (
    term_id             INT             NOT NULL IDENTITY(1,1),
    term_name           VARCHAR(20)     NOT NULL,
    year_name           VARCHAR(10)     NULL,
    order_no            INT             NULL,
    description         VARCHAR(30)     NULL,
    academic_year_id    INT             NULL,
    status              VARCHAR(10)      NULL,
    school_id           INT             NOT NULL,
    created_by          VARCHAR(25)     NULL,
    created_at          DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id          VARCHAR(20)     NULL,
    deleted_by          VARCHAR(60)     NULL,
    CONSTRAINT PK_terms             PRIMARY KEY (term_id),
    CONSTRAINT FK_terms_academic_year FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
);
GO


-- ============================================================
-- 15. subjects  (was SAS_Subjects)
-- ============================================================
CREATE TABLE subjects (
    subject_id          INT             NOT NULL IDENTITY(1,1),
    subject_name        VARCHAR(30)     NOT NULL,
    short_name          VARCHAR(15)     NULL,
    subject_type        VARCHAR(20)     NULL,   -- Theory / Language / General / Practical / Others
    description         VARCHAR(50)     NULL,
    remarks             VARCHAR(50)     NULL,
    academic_year_id    INT             NULL,
    status              VARCHAR(10)     NULL,
    school_id           INT             NOT NULL,
    created_by          VARCHAR(25)     NULL,
    created_at          DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id          VARCHAR(20)     NULL,
    deleted_by          VARCHAR(60)     NULL,
    CONSTRAINT PK_subjects              PRIMARY KEY (subject_id),
    CONSTRAINT FK_subjects_academic_year FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
);
GO


-- ============================================================
-- 16. payment_modes  (was SAS_PaymentMods)
--    Configurable per school: Cash / Cheque / Online
-- ============================================================
CREATE TABLE payment_modes (
    payment_mode_id INT             NOT NULL IDENTITY(1,1),
    mode_name       VARCHAR(15)     NOT NULL,
    description     VARCHAR(25)     NULL,
    status          VARCHAR(10)      NULL,
    is_online       BIT             NOT NULL DEFAULT 0,  -- 1 = triggers gateway checkout
    school_id       INT             NOT NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    deleted_by      VARCHAR(60)     NULL,
    CONSTRAINT PK_payment_modes PRIMARY KEY (payment_mode_id)
);
GO


-- ============================================================
-- 17. fee_periods
--     Monthly fee periods per school per academic year
--     Used when fee_structures.payment_type = 'Monthly'
-- ============================================================
CREATE TABLE fee_periods (
    fee_period_id    INT          NOT NULL IDENTITY(1,1),
    school_id        INT          NOT NULL,
    academic_year_id INT          NULL,
    month_no         TINYINT      NOT NULL,   -- 1=Jan .. 12=Dec
    year_no          SMALLINT     NOT NULL,   -- e.g. 2024
    period_label     VARCHAR(20)  NOT NULL,   -- e.g. "April 2024"
    sequence_no      INT          NULL,
    status           VARCHAR(10)  NOT NULL DEFAULT 'Active',
    created_by       VARCHAR(25)  NULL,
    created_at       DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_fee_periods               PRIMARY KEY (fee_period_id),
    CONSTRAINT FK_fee_periods_academic_year FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
);
GO


-- ============================================================
-- 18. fee_structures  (was SAS_FeeMaster)
--     Fee amount per class + category + fee type + term/period
-- ============================================================
CREATE TABLE fee_structures (
    fee_structure_id    INT             NOT NULL IDENTITY(1,1),
    fee_category_id     INT             NULL,
    class_id            INT             NULL,
    fee_type_id         INT             NULL,
    fee_type_name       VARCHAR(25)     NULL,   -- Nullable; UI sends null
    term_id             INT             NULL,   -- set when payment_type = 'Term'
    fee_period_id       INT             NULL,   -- set when payment_type = 'Monthly'
    payment_type        VARCHAR(10)     NULL,   -- Term / Monthly
    amount              DECIMAL(12,2)   NULL,
    description         VARCHAR(50)     NULL,
    status              VARCHAR(10)      NULL,
    academic_year_id    INT             NULL,
    payment_mode_id     INT             NULL,
    admission_type      VARCHAR(5)      NULL,   -- New / Old student
    school_id           INT             NOT NULL,
    created_by          VARCHAR(25)     NULL,
    created_at          DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id          VARCHAR(20)     NULL,
    deleted_by          VARCHAR(60)     NULL,
    CONSTRAINT PK_fee_structures                PRIMARY KEY (fee_structure_id),
    CONSTRAINT FK_fee_structures_fee_category   FOREIGN KEY (fee_category_id)  REFERENCES fee_categories(fee_category_id),
    CONSTRAINT FK_fee_structures_class          FOREIGN KEY (class_id)         REFERENCES classes(class_id),
    CONSTRAINT FK_fee_structures_fee_type       FOREIGN KEY (fee_type_id)      REFERENCES fee_types(fee_type_id),
    CONSTRAINT FK_fee_structures_term           FOREIGN KEY (term_id)          REFERENCES terms(term_id),
    CONSTRAINT FK_fee_structures_fee_period     FOREIGN KEY (fee_period_id)    REFERENCES fee_periods(fee_period_id),
    CONSTRAINT FK_fee_structures_payment_mode   FOREIGN KEY (payment_mode_id)  REFERENCES payment_modes(payment_mode_id),
    CONSTRAINT FK_fee_structures_academic_year  FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
);
GO


-- ============================================================
-- 18. buses  (was SAS_BussData)
-- ============================================================
CREATE TABLE buses (
    bus_id          INT             NOT NULL IDENTITY(1,1),
    bus_name        VARCHAR(20)     NULL,
    model           VARCHAR(25)     NULL,
    driver_id       INT             NULL,   -- Refers to employee table (TBD)
    capacity        INT             NULL,
    description     VARCHAR(50)     NULL,
    status          VARCHAR(10)      NULL,
    purchase_date   DATE            NULL,
    registration_no VARCHAR(25)     NULL,
    trip_data       VARCHAR(10)     NULL,   -- Nullable; UI sends null
    cleaner_name    VARCHAR(10)     NULL,   -- Nullable; UI sends null
    owner_data      VARCHAR(60)     NULL,   -- Nullable; UI sends null
    route_name      VARCHAR(25)     NULL,   -- Nullable; UI sends null
    school_id       INT             NOT NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    deleted_by      VARCHAR(60)     NULL,
    CONSTRAINT PK_buses PRIMARY KEY (bus_id)
);
GO


-- ============================================================
-- 19. bus_routes  (was SAS_BusRoutes)
-- ============================================================
CREATE TABLE bus_routes (
    route_id        INT             NOT NULL IDENTITY(1,1),
    route_name      VARCHAR(55)     NOT NULL,
    route_code      VARCHAR(10)     NULL,
    route_category  VARCHAR(20)     NULL,
    description     VARCHAR(50)     NULL,
    status          VARCHAR(10)      NULL,
    bus_no_data     VARCHAR(15)     NULL,   -- Nullable; UI sends null
    school_id       INT             NOT NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    deleted_by      VARCHAR(60)     NULL,
    CONSTRAINT PK_bus_routes PRIMARY KEY (route_id)
);
GO


-- ============================================================
-- 20. bus_fee_structures  (was SAS_BusMaster)
--     Bus fee per route + term + academic year
-- ============================================================
CREATE TABLE bus_fee_structures (
    bus_fee_structure_id    INT             NOT NULL IDENTITY(1,1),
    route_id                INT             NULL,
    term_id                 INT             NULL,
    amount                  DECIMAL(12,2)   NULL,
    academic_year_id        INT             NULL,
    status                  VARCHAR(10)      NULL,
    bus_name                VARCHAR(15)     NULL,   -- Nullable; UI sends null
    category_name           VARCHAR(20)     NULL,   -- Nullable; UI sends null
    school_id               INT             NOT NULL,
    created_by              VARCHAR(25)     NULL,
    created_at              DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id              VARCHAR(20)     NULL,
    deleted_by              VARCHAR(60)     NULL,
    CONSTRAINT PK_bus_fee_structures                PRIMARY KEY (bus_fee_structure_id),
    CONSTRAINT FK_bus_fee_structures_route          FOREIGN KEY (route_id)         REFERENCES bus_routes(route_id),
    CONSTRAINT FK_bus_fee_structures_term           FOREIGN KEY (term_id)          REFERENCES terms(term_id),
    CONSTRAINT FK_bus_fee_structures_academic_year  FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
);
GO


-- ============================================================
-- 21. hostels
--     Hostel master per school branch.
--     Students are linked via hostel_id FK on the students table.
-- ============================================================
CREATE TABLE hostels (
    hostel_id    INT           NOT NULL IDENTITY(1,1),
    hostel_name  VARCHAR(100)  NOT NULL,
    description  VARCHAR(200)  NULL,
    capacity     INT           NULL,
    contact_no   VARCHAR(20)   NULL,
    address      VARCHAR(200)  NULL,
    no_of_rooms  INT           NULL,
    school_id    INT           NOT NULL,
    created_by   VARCHAR(25)   NULL,
    created_at   DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_hostels      PRIMARY KEY (hostel_id),
    CONSTRAINT UQ_hostels_name UNIQUE (hostel_name, school_id)
);
GO


-- ============================================================
-- 22. hostel_fee_structures
--     Hostel fee per hostel + academic year + term/period.
--     payment_type: Term | Monthly (same as fee_structures)
-- ============================================================
CREATE TABLE hostel_fee_structures (
    hostel_fee_structure_id INT           NOT NULL IDENTITY(1,1),
    hostel_id               INT           NOT NULL,
    academic_year_id        INT           NOT NULL,
    term_id                 INT           NULL,
    fee_period_id           INT           NULL,
    payment_type            VARCHAR(10)   NOT NULL DEFAULT 'Term',
    amount                  DECIMAL(12,2) NOT NULL DEFAULT 0,
    school_id               INT           NOT NULL,
    CONSTRAINT PK_hostel_fee_structures PRIMARY KEY (hostel_fee_structure_id),
    CONSTRAINT FK_hfs_hostel            FOREIGN KEY (hostel_id)        REFERENCES hostels(hostel_id),
    CONSTRAINT FK_hfs_year              FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
    CONSTRAINT FK_hfs_term              FOREIGN KEY (term_id)          REFERENCES terms(term_id),
    CONSTRAINT FK_hfs_period            FOREIGN KEY (fee_period_id)    REFERENCES fee_periods(fee_period_id)
);
GO


-- ============================================================
-- 23. students  (was SAS_StudentMaster)
--     PK: BIGINT IDENTITY (was FLOAT in VB6 — critical fix)
-- ============================================================
CREATE TABLE students (
    student_id                  BIGINT          NOT NULL IDENTITY(1,1),
    student_unique_id           INT             NULL,   -- Stable cross-year identifier; auto-calculated MAX+1 on insert; same value retained on promotion
    admission_no                VARCHAR(20)     NULL,   -- School-issued human-readable ID (can change at 6th class)
    school_unique_id            VARCHAR(20)     NULL,   -- School-assigned student unique ID
    student_name                VARCHAR(105)    NOT NULL,
    short_name                  VARCHAR(55)     NULL,
    join_type                   VARCHAR(10)     NULL,   -- New / Transfer
    guardian_type               VARCHAR(10)     NULL,   -- Parents / Guardian
    father_name                 VARCHAR(50)     NULL,
    father_qualification        VARCHAR(50)     NULL,
    father_occupation           VARCHAR(50)     NULL,
    father_employment_type      VARCHAR(50)     NULL,
    father_mobile               VARCHAR(20)     NULL,
    mother_name                 VARCHAR(50)     NULL,
    mother_qualification        VARCHAR(50)     NULL,
    mother_occupation           VARCHAR(50)     NULL,
    mother_mobile               VARCHAR(20)     NULL,
    date_of_birth               DATE            NULL,
    date_of_joining             DATE            NULL,
    academic_year_id            INT             NULL,
    fee_category_id             INT             NULL,
    gender                      VARCHAR(10)     NULL,
    class_id                    INT             NULL,
    branch_name                 VARCHAR(20)     NULL,   -- Stream: Science / Arts / Commerce
    section                     VARCHAR(20)     NULL,
    roll_no                     VARCHAR(10)     NULL,
    caste                       VARCHAR(25)     NULL,
    caste_code                  VARCHAR(10)     NULL,
    religion                    VARCHAR(20)     NULL,
    nationality                 VARCHAR(10)     NULL,
    door_no                     VARCHAR(50)     NULL,
    address_area                VARCHAR(50)     NULL,
    address_city                VARCHAR(50)     NULL,
    address_state               VARCHAR(50)     NULL,
    permanent_address           VARCHAR(150)    NULL,
    email                       VARCHAR(50)     NULL,
    annual_income               DECIMAL(12,2)   NULL,
    family_children_count       INT             NULL,
    dob_proof_submitted         VARCHAR(10)     NULL,   -- Submitted / Not Submitted
    aadhar_no                   VARCHAR(25)     NULL,
    caste_cert_submitted        VARCHAR(10)     NULL,   -- Submitted / Not Submitted
    other_certificates          VARCHAR(50)     NULL,
    transport_type              VARCHAR(10)     NULL,   -- Bus / Walking / etc.
    bus_route_id                INT             NULL,
    joining_class               VARCHAR(20)     NULL,   -- Class at first admission
    remarks                     VARCHAR(50)     NULL,
    photo_path                  VARCHAR(255)    NULL,   -- File path or cloud URL
    admission_date              DATE            NULL,
    disability_status           VARCHAR(10)      NULL,   -- Y / N
    disability_type             VARCHAR(25)     NULL,
    reference_name              VARCHAR(30)     NULL,
    student_type                VARCHAR(15)     NULL,   -- DayScholar / Hosteler
    scholarship_status          VARCHAR(10)     NULL,
    scholarship_description     VARCHAR(30)     NULL,
    blood_group                 VARCHAR(10)     NULL,
    bus_id                      INT             NULL,
    bus_trip                    VARCHAR(10)     NULL,   -- 1st Trip / 2nd Trip / 3rd Trip
    join_term                   VARCHAR(20)     NULL,
    hostel_name                 VARCHAR(10)     NULL,   -- Legacy free-text; use hostel_id FK for new data
    hostel_id                   INT             NULL,   -- FK → hostels
    biometric_id                VARCHAR(5)      NULL,   -- Biometric device enrollment ID
    mother_tongue               VARCHAR(25)     NULL,
    first_language              VARCHAR(25)     NULL,
    second_language             VARCHAR(10)     NULL,
    third_language              VARCHAR(25)     NULL,
    udise_no                    VARCHAR(35)     NULL,   -- Govt UDISE number
    spare_field_1               VARCHAR(50)     NULL,   -- Reserved for future use
    spare_field_2               VARCHAR(50)     NULL,   -- Reserved for future use
    status                      VARCHAR(10)     NULL,
    school_id                   INT             NOT NULL,
    created_by                  VARCHAR(25)     NULL,
    created_at                  DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_by                  VARCHAR(25)     NULL,
    updated_at                  DATETIME        NULL,
    machine_id                  VARCHAR(25)     NULL,
    deleted_by                  VARCHAR(60)     NULL,
    section_id                  INT             NULL,
    blocked_reason              VARCHAR(200)    NULL,           -- set when status = 'Blocked'
    is_detained                 BIT             NOT NULL DEFAULT 0,
    detained_reason             VARCHAR(200)    NULL,           -- required when is_detained = 1
    CONSTRAINT PK_students                  PRIMARY KEY (student_id),
    CONSTRAINT FK_students_academic_year    FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
    CONSTRAINT FK_students_fee_category     FOREIGN KEY (fee_category_id)  REFERENCES fee_categories(fee_category_id),
    CONSTRAINT FK_students_class            FOREIGN KEY (class_id)         REFERENCES classes(class_id),
    CONSTRAINT FK_students_section          FOREIGN KEY (section_id)       REFERENCES sections(section_id),
    CONSTRAINT FK_students_bus_route        FOREIGN KEY (bus_route_id)     REFERENCES bus_routes(route_id),
    CONSTRAINT FK_students_bus              FOREIGN KEY (bus_id)           REFERENCES buses(bus_id),
    CONSTRAINT FK_students_hostel           FOREIGN KEY (hostel_id)        REFERENCES hostels(hostel_id)
);
GO

-- students → system-versioned temporal table (full-row audit trail in students_history).
-- Every change from any path is snapshotted automatically; each history row carries
-- updated_by/updated_at. Requires SQL Server 2016+. Query with FOR SYSTEM_TIME.
ALTER TABLE students ADD
    valid_from DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
        CONSTRAINT DF_students_valid_from DEFAULT SYSUTCDATETIME(),
    valid_to   DATETIME2 GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL
        CONSTRAINT DF_students_valid_to   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME (valid_from, valid_to);
GO
ALTER TABLE students SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.students_history));
GO


-- ============================================================
-- 22. fee_receipts
--     One row per payment session — the receipt header.
--     receipt_no is school-issued, formatted after insert.
-- ============================================================
CREATE TABLE fee_receipts (
    receipt_id          INT             NOT NULL IDENTITY(1,1),
    receipt_no          VARCHAR(20)     NOT NULL,
    student_id          BIGINT          NOT NULL,
    student_unique_id   INT             NULL,   -- Stable cross-year identifier copied from students.student_unique_id
    academic_year_id    INT             NULL,
    payment_date        DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    total_amount        DECIMAL(12,2)   NOT NULL DEFAULT 0,
    payment_mode_id     INT             NULL,
    cheque_no           VARCHAR(20)     NULL,
    cheque_date         DATE            NULL,
    bank_name           VARCHAR(50)     NULL,
    status              VARCHAR(10)     NOT NULL DEFAULT 'Active',  -- Active / Cancelled
    cancelled_by        VARCHAR(50)     NULL,
    cancelled_at        DATETIME        NULL,
    cancel_reason       VARCHAR(100)    NULL,
    remarks             VARCHAR(100)    NULL,
    source              VARCHAR(10)     NULL,   -- 'legacy' (VB6 EOD sync) | 'webapp' (school web app / parent portal)
    school_id           INT             NOT NULL,
    created_by          VARCHAR(25)     NULL,
    created_at          DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id          VARCHAR(25)     NULL,
    CONSTRAINT PK_fee_receipts                  PRIMARY KEY (receipt_id),
    CONSTRAINT FK_fee_receipts_student          FOREIGN KEY (student_id)       REFERENCES students(student_id),
    CONSTRAINT FK_fee_receipts_academic_year    FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
    CONSTRAINT FK_fee_receipts_payment_mode     FOREIGN KEY (payment_mode_id)  REFERENCES payment_modes(payment_mode_id)
);
GO


-- ============================================================
-- 23. fee_receipt_items
--     Line items for each receipt — one row per fee_type + term.
--     net_amount = amount - concession_amount (actual cash collected).
-- ============================================================
CREATE TABLE fee_receipt_items (
    item_id             INT             NOT NULL IDENTITY(1,1),
    receipt_id          INT             NOT NULL,
    fee_type_id         INT             NULL,
    term_id             INT             NULL,
    fee_period_id       INT             NULL,   -- set when payment_type = 'Monthly'
    bus_route_id        INT             NULL,   -- set for transport fee receipts
    hostel_id           INT             NULL,   -- set for hostel fee receipts
    amount              DECIMAL(12,2)   NOT NULL DEFAULT 0,   -- Face amount collected
    concession_amount   DECIMAL(12,2)   NOT NULL DEFAULT 0,   -- Concession applied
    net_amount          DECIMAL(12,2)   NOT NULL DEFAULT 0,   -- amount - concession
    school_id           INT             NOT NULL,
    CONSTRAINT PK_fee_receipt_items             PRIMARY KEY (item_id),
    CONSTRAINT FK_fee_receipt_items_receipt     FOREIGN KEY (receipt_id)    REFERENCES fee_receipts(receipt_id),
    CONSTRAINT FK_fee_receipt_items_fee_type    FOREIGN KEY (fee_type_id)   REFERENCES fee_types(fee_type_id),
    CONSTRAINT FK_fee_receipt_items_term        FOREIGN KEY (term_id)       REFERENCES terms(term_id),
    CONSTRAINT FK_fee_receipt_items_fee_period  FOREIGN KEY (fee_period_id) REFERENCES fee_periods(fee_period_id),
    CONSTRAINT FK_fee_receipt_items_bus_route   FOREIGN KEY (bus_route_id)  REFERENCES bus_routes(route_id),
    CONSTRAINT FK_fee_receipt_items_hostel      FOREIGN KEY (hostel_id)     REFERENCES hostels(hostel_id)
);
GO


-- ============================================================
-- 24. gateway_configs
--     API keys for payment gateways, per school branch.
--     school_id NULL = applies to all branches in this group.
-- ============================================================
CREATE TABLE gateway_configs (
    gateway_config_id  INT             NOT NULL IDENTITY(1,1),
    gateway_name       VARCHAR(50)     NOT NULL,    -- 'Razorpay' | 'Cashfree' | 'PayU'
    display_name       VARCHAR(100)    NOT NULL,
    key_id             VARCHAR(200)    NOT NULL,
    key_secret         VARCHAR(500)    NOT NULL,    -- never expose in API responses
    webhook_secret     VARCHAR(500)    NULL,
    is_active          BIT             NOT NULL DEFAULT 1,
    school_id          INT             NULL,        -- NULL = group-wide
    created_at         DATETIME        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_gateway_configs PRIMARY KEY (gateway_config_id)
);
GO


-- ============================================================
-- 25. payment_gateway_orders
--     Tracks the lifecycle of online payments.
--     Created before checkout opens; updated to Paid/Failed after result.
-- ============================================================
CREATE TABLE payment_gateway_orders (
    gateway_order_id   INT             NOT NULL IDENTITY(1,1),
    gateway_name       VARCHAR(50)     NOT NULL,
    external_order_id  VARCHAR(100)    NOT NULL,   -- Razorpay order_id
    amount             DECIMAL(12,2)   NOT NULL,
    student_id         BIGINT          NOT NULL,
    academic_year_id   INT             NOT NULL,
    payment_mode_id    INT             NOT NULL,
    payload_json       NVARCHAR(MAX)   NOT NULL,   -- full request JSON for receipt replay
    created_by         VARCHAR(100)    NOT NULL,
    status             VARCHAR(10)     NOT NULL DEFAULT 'Pending',  -- Pending/Paid/Failed
    payment_id         VARCHAR(100)    NULL,
    receipt_id         INT             NULL,
    school_id          INT             NOT NULL,
    created_at         DATETIME        NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_payment_gateway_orders            PRIMARY KEY (gateway_order_id),
    CONSTRAINT FK_gw_orders_students                FOREIGN KEY (student_id)       REFERENCES students(student_id),
    CONSTRAINT FK_gw_orders_academic_years          FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
    CONSTRAINT FK_gw_orders_receipts                FOREIGN KEY (receipt_id)       REFERENCES fee_receipts(receipt_id)
);
GO


-- ============================================================
-- 26. student_mobile_accounts
-- ============================================================
CREATE TABLE student_mobile_accounts (
    account_id      INT             NOT NULL IDENTITY(1,1),
    student_id      BIGINT          NOT NULL,
    pin_hash        VARCHAR(255)    NOT NULL,
    mobile          VARCHAR(20)     NULL,
    email           VARCHAR(100)    NULL,
    otp_code        VARCHAR(10)     NULL,
    otp_expires_at  DATETIME        NULL,
    is_active       BIT             NOT NULL DEFAULT 1,
    last_login_at   DATETIME        NULL,
    school_id       INT             NOT NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_student_mobile_accounts           PRIMARY KEY (account_id),
    CONSTRAINT UQ_student_mobile_accounts_student   UNIQUE (student_id),
    CONSTRAINT FK_student_mobile_accounts_student   FOREIGN KEY (student_id) REFERENCES students(student_id)
);
GO


-- ============================================================
-- 27. student_refresh_tokens
-- ============================================================
CREATE TABLE student_refresh_tokens (
    token_id        INT             NOT NULL IDENTITY(1,1),
    account_id      INT             NOT NULL,
    token_hash      VARCHAR(255)    NOT NULL,
    expires_at      DATETIME        NOT NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    revoked_at      DATETIME        NULL,
    replaced_by     VARCHAR(255)    NULL,
    CONSTRAINT PK_student_refresh_tokens            PRIMARY KEY (token_id),
    CONSTRAINT FK_student_refresh_tokens_account    FOREIGN KEY (account_id) REFERENCES student_mobile_accounts(account_id)
);
GO


-- ============================================================
-- 28. student_attendance
-- ============================================================
CREATE TABLE student_attendance (
    attendance_id   INT             NOT NULL IDENTITY(1,1),
    student_id      BIGINT          NOT NULL,
    attendance_date DATE            NOT NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Present',
    remarks         VARCHAR(100)    NULL,
    school_id       INT             NOT NULL,
    marked_by       VARCHAR(100)    NOT NULL,
    marked_at       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_student_attendance            PRIMARY KEY (attendance_id),
    CONSTRAINT UQ_student_attendance_date       UNIQUE (student_id, attendance_date, school_id),
    CONSTRAINT FK_student_attendance_student    FOREIGN KEY (student_id) REFERENCES students(student_id)
);
CREATE INDEX IX_student_attendance_student_date ON student_attendance (student_id, attendance_date);
GO


-- ============================================================
-- 29. exam_types
-- ============================================================
CREATE TABLE exam_types (
    exam_type_id    INT             NOT NULL IDENTITY(1,1),
    exam_type_name  VARCHAR(50)     NOT NULL,
    academic_year_id INT            NULL,
    display_order   INT             NULL,
    school_id       INT             NOT NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    created_by      VARCHAR(100)    NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_exam_types    PRIMARY KEY (exam_type_id)
);
GO


-- ============================================================
-- 30. student_marks
-- ============================================================
CREATE TABLE student_marks (
    mark_id             INT             NOT NULL IDENTITY(1,1),
    student_id          BIGINT          NOT NULL,
    subject_id          INT             NOT NULL,
    exam_type_id        INT             NOT NULL,
    academic_year_id    INT             NOT NULL,
    marks_obtained      DECIMAL(6,2)    NOT NULL,
    max_marks           DECIMAL(6,2)    NOT NULL DEFAULT 100,
    is_absent           BIT             NOT NULL DEFAULT 0,
    school_id           INT             NOT NULL,
    entered_by          VARCHAR(100)    NOT NULL,
    entered_at          DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_by          VARCHAR(100)    NULL,
    updated_at          DATETIME        NULL,
    CONSTRAINT PK_student_marks         PRIMARY KEY (mark_id),
    CONSTRAINT UQ_student_marks         UNIQUE (student_id, subject_id, exam_type_id, academic_year_id, school_id),
    CONSTRAINT FK_student_marks_student FOREIGN KEY (student_id)       REFERENCES students(student_id),
    CONSTRAINT FK_student_marks_subject FOREIGN KEY (subject_id)       REFERENCES subjects(subject_id),
    CONSTRAINT FK_student_marks_exam    FOREIGN KEY (exam_type_id)     REFERENCES exam_types(exam_type_id),
    CONSTRAINT FK_student_marks_year    FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
);
CREATE INDEX IX_student_marks_student ON student_marks (student_id, academic_year_id);
GO


-- ============================================================
-- 31. homework
-- ============================================================
CREATE TABLE homework (
    homework_id     INT             NOT NULL IDENTITY(1,1),
    title           VARCHAR(200)    NOT NULL,
    description     NVARCHAR(MAX)   NULL,
    subject_id      INT             NULL,
    class_id        INT             NULL,
    section_id      INT             NULL,       -- NULL = class-wide; set for section-specific homework
    assigned_date   DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    due_date        DATE            NULL,          -- retired; kept nullable for legacy rows
    school_id       INT             NOT NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    created_by      VARCHAR(100)    NOT NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_by      VARCHAR(100)    NULL,
    updated_at      DATETIME        NULL,
    attachment_url  VARCHAR(4000)   NULL,       -- optional PDF/doc link (Google Drive, Cloudinary)
    CONSTRAINT PK_homework          PRIMARY KEY (homework_id),
    CONSTRAINT FK_homework_subject  FOREIGN KEY (subject_id) REFERENCES subjects(subject_id),
    CONSTRAINT FK_homework_class    FOREIGN KEY (class_id)   REFERENCES classes(class_id),
    CONSTRAINT FK_homework_section  FOREIGN KEY (section_id) REFERENCES sections(section_id)
);
-- Supports the paged web list (school-scoped, newest first) and the mobile feed
-- (class + section + newest first). Ordered by assigned_date since due_date is retired.
CREATE INDEX IX_homework_school_assigned ON homework (school_id, assigned_date DESC, homework_id DESC);
CREATE INDEX IX_homework_class_assigned  ON homework (class_id, school_id, status, assigned_date DESC);
GO


-- ============================================================
-- 32. homework_attachments
-- ============================================================
CREATE TABLE homework_attachments (
    attachment_id   INT             NOT NULL IDENTITY(1,1),
    homework_id     INT             NOT NULL,
    file_name       VARCHAR(200)    NOT NULL,
    file_path       VARCHAR(500)    NOT NULL,
    file_size_kb    INT             NULL,
    CONSTRAINT PK_homework_attachments      PRIMARY KEY (attachment_id),
    CONSTRAINT FK_homework_attachments_hw   FOREIGN KEY (homework_id) REFERENCES homework(homework_id)
);
GO


-- ============================================================
-- 33. announcements
-- ============================================================
CREATE TABLE announcements (
    announcement_id INT             NOT NULL IDENTITY(1,1),
    title           VARCHAR(200)    NOT NULL,
    description     NVARCHAR(MAX)   NULL,
    scope           VARCHAR(10)     NOT NULL DEFAULT 'School',
    class_id        INT             NULL,
    section_id      INT             NULL,       -- optional; NULL = whole class (all sections)
    is_pinned       BIT             NOT NULL DEFAULT 0,
    school_id       INT             NOT NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    created_by      VARCHAR(100)    NOT NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_by      VARCHAR(100)    NULL,
    updated_at      DATETIME        NULL,
    attachment_url  VARCHAR(4000)   NULL,       -- optional PDF/doc link (Google Drive, Cloudinary)
    CONSTRAINT PK_announcements         PRIMARY KEY (announcement_id),
    CONSTRAINT FK_announcements_class   FOREIGN KEY (class_id) REFERENCES classes(class_id),
    CONSTRAINT FK_announcements_section FOREIGN KEY (section_id) REFERENCES sections(section_id)
);
CREATE INDEX IX_announcements_school_date ON announcements (school_id, created_at DESC);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_students_admno_school_year'
      AND object_id = OBJECT_ID('students')
)
BEGIN
    CREATE UNIQUE INDEX UQ_students_admno_school_year
        ON students (admission_no, school_id, academic_year_id)
        WHERE admission_no IS NOT NULL AND academic_year_id IS NOT NULL;
    PRINT 'UQ_students_admno_school_year index created.';
END
ELSE
    PRINT 'UQ_students_admno_school_year already exists — skipping.';
GO


-- ============================================================
-- 34. school_events
--     Media stored externally (YouTube / Cloudinary).
--     Only metadata + URLs stored here.
-- ============================================================
CREATE TABLE school_events (
    event_id        INT             NOT NULL IDENTITY(1,1),
    school_id       INT             NOT NULL,
    title           VARCHAR(200)    NOT NULL,
    description     VARCHAR(1000)   NULL,
    event_date      DATE            NOT NULL,
    media_type      VARCHAR(10)     NOT NULL DEFAULT 'image',  -- image / video
    media_url       VARCHAR(4000)   NULL,                       -- optional YouTube URL (R2 uploads live in media_uploads)
    thumbnail_url   VARCHAR(4000)   NULL,                       -- optional override thumbnail
    attachment_url  VARCHAR(4000)   NULL,                       -- optional PDF/doc link
    scope           VARCHAR(10)     NOT NULL DEFAULT 'School',  -- School / Class
    class_id        INT             NULL,
    is_pinned       BIT             NOT NULL DEFAULT 0,
    status          VARCHAR(20)     NOT NULL DEFAULT 'Active',  -- Active / Inactive
    created_by      VARCHAR(100)    NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_by      VARCHAR(100)    NULL,
    updated_at      DATETIME        NULL,
    CONSTRAINT PK_school_events       PRIMARY KEY (event_id),
    CONSTRAINT FK_school_events_class FOREIGN KEY (class_id) REFERENCES classes(class_id)
);
CREATE INDEX IX_school_events_school_id  ON school_events (school_id);
CREATE INDEX IX_school_events_event_date ON school_events (event_date DESC);
GO


-- ============================================================
-- 35. staff
--     Employee master per school branch.
-- ============================================================
CREATE TABLE staff (
    staff_id        INT             NOT NULL IDENTITY(1,1),
    staff_name      VARCHAR(100)    NOT NULL,
    employee_code   VARCHAR(20)     NULL,
    designation     VARCHAR(50)     NULL,
    department      VARCHAR(50)     NULL,
    mobile          VARCHAR(20)     NULL,
    email           VARCHAR(100)    NULL,
    join_date       DATE            NULL,
    school_id       INT             NOT NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',  -- Active / Inactive
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_at      DATETIME        NULL,
    CONSTRAINT PK_staff PRIMARY KEY (staff_id)
);
CREATE INDEX IX_staff_school_status ON staff (school_id, status);
GO

-- Link users → staff (school-app logins are created from a staff record).
-- Added here (not on the users table above) because staff is created after users.
ALTER TABLE users
    ADD CONSTRAINT FK_users_staff FOREIGN KEY (employee_id) REFERENCES staff(staff_id);
GO
-- One login per staff member (filtered so multiple NULLs are allowed, e.g. the admin).
CREATE UNIQUE INDEX UQ_users_employee ON users (employee_id) WHERE employee_id IS NOT NULL;
GO


-- ============================================================
-- 36. staff_attendance
--     One row per staff member per date.
--     status: Present / Absent / Late / HalfDay / OnLeave
-- ============================================================
CREATE TABLE staff_attendance (
    attendance_id   INT             NOT NULL IDENTITY(1,1),
    staff_id        INT             NOT NULL,
    attendance_date DATE            NOT NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Present',
    remarks         VARCHAR(100)    NULL,
    school_id       INT             NOT NULL,
    marked_by       VARCHAR(100)    NOT NULL,
    marked_at       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_staff_attendance              PRIMARY KEY (attendance_id),
    CONSTRAINT UQ_staff_attendance_date         UNIQUE (staff_id, attendance_date, school_id),
    CONSTRAINT FK_staff_attendance_staff        FOREIGN KEY (staff_id) REFERENCES staff(staff_id)
);
CREATE INDEX IX_staff_attendance_school_date ON staff_attendance (school_id, attendance_date);
GO


-- ============================================================
-- 37. staff_advances
--     Records each advance / loan given to a staff member.
--     status: Active / Cancelled
-- ============================================================
CREATE TABLE staff_advances (
    advance_id      INT             NOT NULL IDENTITY(1,1),
    staff_id        INT             NOT NULL,
    advance_date    DATE            NOT NULL,
    amount          DECIMAL(12,2)   NOT NULL,
    purpose         VARCHAR(200)    NULL,
    remarks         VARCHAR(200)    NULL,
    school_id       INT             NOT NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    created_by      VARCHAR(100)    NOT NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_staff_advances            PRIMARY KEY (advance_id),
    CONSTRAINT FK_staff_advances_staff      FOREIGN KEY (staff_id) REFERENCES staff(staff_id)
);
CREATE INDEX IX_staff_advances_staff ON staff_advances (staff_id, school_id, status);
GO


-- ============================================================
-- 38. staff_advance_repayments
--     Records each repayment against an advance.
-- ============================================================
CREATE TABLE staff_advance_repayments (
    repayment_id    INT             NOT NULL IDENTITY(1,1),
    advance_id      INT             NOT NULL,
    staff_id        INT             NOT NULL,
    repayment_date  DATE            NOT NULL,
    amount          DECIMAL(12,2)   NOT NULL,
    remarks         VARCHAR(200)    NULL,
    school_id       INT             NOT NULL,
    created_by      VARCHAR(100)    NOT NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_staff_advance_repayments          PRIMARY KEY (repayment_id),
    CONSTRAINT FK_staff_advance_repayments_advance  FOREIGN KEY (advance_id) REFERENCES staff_advances(advance_id),
    CONSTRAINT FK_staff_advance_repayments_staff    FOREIGN KEY (staff_id)   REFERENCES staff(staff_id)
);
CREATE INDEX IX_staff_advance_repayments_advance ON staff_advance_repayments (advance_id);
GO


-- ============================================================
-- 39. staff_salary_components
--     Salary structure template per staff member.
--     component_type: Earning / Deduction
-- ============================================================
CREATE TABLE staff_salary_components (
    component_id    INT             NOT NULL IDENTITY(1,1),
    staff_id        INT             NOT NULL,
    component_name  VARCHAR(80)     NOT NULL,
    component_type  VARCHAR(10)     NOT NULL,
    amount          DECIMAL(12,2)   NOT NULL,
    display_order   INT             NOT NULL DEFAULT 0,
    school_id       INT             NOT NULL,
    is_active       BIT             NOT NULL DEFAULT 1,
    CONSTRAINT PK_staff_salary_components           PRIMARY KEY (component_id),
    CONSTRAINT FK_staff_salary_components_staff     FOREIGN KEY (staff_id) REFERENCES staff(staff_id),
    CONSTRAINT UQ_staff_salary_components           UNIQUE (staff_id, component_name, school_id)
);
CREATE INDEX IX_staff_salary_components_staff ON staff_salary_components (staff_id, school_id);
GO


-- ============================================================
-- 40. staff_salaries
--     Monthly salary header per staff member.
--     status: Draft / Paid / Cancelled
-- ============================================================
CREATE TABLE staff_salaries (
    salary_id           INT             NOT NULL IDENTITY(1,1),
    staff_id            INT             NOT NULL,
    month               INT             NOT NULL,
    year                INT             NOT NULL,
    gross_earnings      DECIMAL(12,2)   NOT NULL DEFAULT 0,
    total_deductions    DECIMAL(12,2)   NOT NULL DEFAULT 0,
    advance_deducted    DECIMAL(12,2)   NOT NULL DEFAULT 0,
    net_salary          DECIMAL(12,2)   NOT NULL DEFAULT 0,
    school_id           INT             NOT NULL,
    status              VARCHAR(10)     NOT NULL DEFAULT 'Draft',
    remarks             VARCHAR(200)    NULL,
    processed_by        VARCHAR(100)    NOT NULL,
    processed_at        DATETIME        NOT NULL DEFAULT GETDATE(),
    paid_date           DATE            NULL,
    CONSTRAINT PK_staff_salaries        PRIMARY KEY (salary_id),
    CONSTRAINT FK_staff_salaries_staff  FOREIGN KEY (staff_id) REFERENCES staff(staff_id),
    CONSTRAINT UQ_staff_salaries        UNIQUE (staff_id, month, year, school_id)
);
CREATE INDEX IX_staff_salaries_month_year ON staff_salaries (month, year, school_id, status);
GO


-- ============================================================
-- 41. staff_salary_items
--     Snapshot of salary components at processing time.
--     Historical record — unaffected by future structure changes.
-- ============================================================
CREATE TABLE staff_salary_items (
    item_id         INT             NOT NULL IDENTITY(1,1),
    salary_id       INT             NOT NULL,
    component_name  VARCHAR(80)     NOT NULL,
    component_type  VARCHAR(10)     NOT NULL,
    amount          DECIMAL(12,2)   NOT NULL,
    CONSTRAINT PK_staff_salary_items            PRIMARY KEY (item_id),
    CONSTRAINT FK_staff_salary_items_salary     FOREIGN KEY (salary_id) REFERENCES staff_salaries(salary_id)
);
CREATE INDEX IX_staff_salary_items_salary ON staff_salary_items (salary_id);
GO


-- ============================================================
-- 42. sms_logs
--     Audit trail of every SMS sent from the school app.
-- ============================================================
CREATE TABLE sms_logs (
    log_id          INT             NOT NULL IDENTITY(1,1),
    school_id       INT             NOT NULL,
    sms_type        VARCHAR(20)     NOT NULL,   -- Absent / FeeDue / Custom
    student_id      BIGINT          NULL,
    student_name    VARCHAR(200)    NULL,
    mobile          VARCHAR(20)     NOT NULL,
    message         NVARCHAR(1000)  NOT NULL,
    status          VARCHAR(10)     NOT NULL,   -- Sent / Failed
    error_message   VARCHAR(500)    NULL,
    sent_by         VARCHAR(200)    NOT NULL,
    sent_at         DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_sms_logs PRIMARY KEY (log_id)
);
CREATE INDEX IX_sms_logs_school_type_date ON sms_logs (school_id, sms_type, sent_at DESC);
GO
-- ============================================================
-- 42a. sms_configs / sms_templates
--      Per-school SMS gateway account + DLT message templates (SMS Center).
--      Seeded per-school by seed_tenant_data.sql. OTP is unaffected
--      (it uses the hardcoded global account in SmsHelper.cs).
-- ============================================================
CREATE TABLE sms_configs (
    config_id   INT IDENTITY(1,1) NOT NULL,
    school_id   INT          NOT NULL,
    provider    VARCHAR(30)  NOT NULL DEFAULT 'smslogin',
    api_url     VARCHAR(200) NOT NULL,
    username    VARCHAR(100) NOT NULL,
    api_key     VARCHAR(200) NOT NULL,            -- never returned by the GET endpoint
    sender_id   VARCHAR(20)  NOT NULL,            -- school's DLT-approved header
    is_enabled  BIT          NOT NULL DEFAULT 1,
    created_by  VARCHAR(150) NULL,
    updated_at  DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_sms_configs        PRIMARY KEY (config_id),
    CONSTRAINT UQ_sms_configs_school UNIQUE (school_id)
);
GO
CREATE TABLE sms_templates (
    template_row_id INT IDENTITY(1,1) NOT NULL,
    school_id       INT           NOT NULL,
    template_key    VARCHAR(40)   NOT NULL,       -- 'ABSENT','FEE_DUE','CUSTOM', future keys...
    title           VARCHAR(100)  NULL,
    template_id     VARCHAR(50)   NOT NULL DEFAULT '',   -- DLT id ('' = not set yet)
    message_text    NVARCHAR(500) NOT NULL DEFAULT '',   -- {name},{date},{amount} placeholders
    placeholders    VARCHAR(200)  NULL,
    is_active       BIT           NOT NULL DEFAULT 1,
    updated_at      DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_sms_templates PRIMARY KEY (template_row_id),
    CONSTRAINT UQ_sms_templates UNIQUE (school_id, template_key)
);
GO
-- ============================================================
-- 44. fee_concessions
--     One active concession per student per fee type per school (school fees),
--     or per student per bus route per term (transport concessions).
--     Reduces outstanding in fee summary (structure - paid - concession).
--     receipt_no format: CFR{yr8}{D5}
--       yr8  = academic_year digits concatenated ("2026-2027" → "20262027")
--       D5   = 5-digit counter per school per academic year
-- ============================================================
CREATE TABLE fee_concessions (
    concession_id     INT           NOT NULL IDENTITY(1,1),
    school_id         INT           NOT NULL,
    academic_year_id  INT           NOT NULL,
    student_id        BIGINT        NOT NULL,
    student_unique_id INT           NULL,   -- copied from students.student_unique_id for cross-year reference
    fee_type_id       INT           NULL,   -- NULL for transport/hostel concessions
    bus_route_id      INT           NULL,   -- set for transport concessions
    hostel_id         INT           NULL,   -- set for hostel concessions
    term_id           INT           NULL,        -- FK → terms; set for Term-based fee structures
    fee_period_id     INT           NULL,        -- FK → fee_periods; set for Monthly fee structures
    concession_type   VARCHAR(30)   NOT NULL,   -- Poor / Not Applicable / Staff Child
    amount            DECIMAL(12,2) NOT NULL,
    remarks           VARCHAR(500)  NULL,
    receipt_no        VARCHAR(25)   NOT NULL,
    status            VARCHAR(10)   NOT NULL DEFAULT 'Active',
    created_by        VARCHAR(25)   NULL,
    created_at        DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_fee_concessions           PRIMARY KEY (concession_id),
    CONSTRAINT FK_fee_concessions_students  FOREIGN KEY (student_id)       REFERENCES students(student_id),
    CONSTRAINT FK_fee_concessions_types     FOREIGN KEY (fee_type_id)      REFERENCES fee_types(fee_type_id),
    CONSTRAINT FK_fee_concessions_ay        FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
    CONSTRAINT FK_fee_concessions_term      FOREIGN KEY (term_id)          REFERENCES terms(term_id),
    CONSTRAINT FK_fee_concessions_period    FOREIGN KEY (fee_period_id)    REFERENCES fee_periods(fee_period_id),
    CONSTRAINT FK_fee_concessions_bus_route FOREIGN KEY (bus_route_id)     REFERENCES bus_routes(route_id),
    CONSTRAINT FK_fee_concessions_hostel    FOREIGN KEY (hostel_id)        REFERENCES hostels(hostel_id)
);
GO

-- One active concession per student+fee_type+term (Term-based school fees)
CREATE UNIQUE INDEX UQ_fee_concessions_term
    ON fee_concessions (student_id, fee_type_id, school_id, term_id)
    WHERE status = 'Active' AND term_id IS NOT NULL AND fee_type_id IS NOT NULL;
GO

-- One active concession per student+fee_type+period (Monthly school fees)
CREATE UNIQUE INDEX UQ_fee_concessions_period
    ON fee_concessions (student_id, fee_type_id, school_id, fee_period_id)
    WHERE status = 'Active' AND fee_period_id IS NOT NULL AND fee_type_id IS NOT NULL;
GO

-- One active concession per student+bus_route+term (transport fees)
CREATE UNIQUE INDEX UQ_fee_concessions_transport
    ON fee_concessions (student_id, bus_route_id, school_id, term_id)
    WHERE status = 'Active' AND bus_route_id IS NOT NULL AND term_id IS NOT NULL;
GO

-- One active hostel concession per student+hostel+term (Term-based)
CREATE UNIQUE INDEX UQ_fee_concessions_hostel
    ON fee_concessions (student_id, hostel_id, school_id, term_id)
    WHERE status = 'Active' AND hostel_id IS NOT NULL AND term_id IS NOT NULL;
GO

-- One active hostel concession per student+hostel+period (Monthly)
CREATE UNIQUE INDEX UQ_fee_concessions_hostel_period
    ON fee_concessions (student_id, hostel_id, school_id, fee_period_id)
    WHERE status = 'Active' AND hostel_id IS NOT NULL AND fee_period_id IS NOT NULL;
GO


-- ============================================================
-- 45. grade_types
--     Marks-to-grade bands per subject (e.g. 90-100 → A+).
--     Migrated from legacy SAS_MarksGrade.
-- ============================================================
CREATE TABLE grade_types (
    id            INT          NOT NULL IDENTITY(1,1),
    grade_name    VARCHAR(200) NULL,
    subject_id    INT          NULL,        -- FK → subjects; NULL when legacy subject not matched
    max_marks     FLOAT        NULL,
    min_marks     FLOAT        NULL,
    grade         VARCHAR(100) NULL,
    remarks       VARCHAR(200) NULL,
    status        VARCHAR(10)  NOT NULL DEFAULT 'Active',
    created_date  DATETIME     NOT NULL DEFAULT GETDATE(),
    created_by    VARCHAR(200) NULL,
    school_id     INT          NOT NULL,
    CONSTRAINT PK_grade_types         PRIMARY KEY (id),
    CONSTRAINT FK_grade_types_subject FOREIGN KEY (subject_id) REFERENCES subjects(subject_id)
);
GO


-- ============================================================
-- 46. exam_master
--     Per-class/per-subject exam definitions (marks, grade band, date).
--     Migrated from legacy SAS_ExamNam (full detail rows).
-- ============================================================
CREATE TABLE exam_master (
    id                INT          NOT NULL IDENTITY(1,1),
    exam_type_id      INT          NULL,        -- FK → exam_types (by name)
    class_id          INT          NULL,        -- FK → classes
    exam_total_marks  INT          NULL,
    exam_min_marks    INT          NULL,
    subject_min_marks INT          NULL,
    sub_max_marks     INT          NULL,
    exam_remarks      VARCHAR(300) NULL,
    academic_year_id  INT          NULL,        -- FK → academic_years
    subject_id        INT          NULL,        -- FK → subjects
    exam_status       VARCHAR(10)  NOT NULL DEFAULT 'Active',
    created_by        VARCHAR(200) NULL,
    created_date      DATETIME     NOT NULL DEFAULT GETDATE(),
    school_id         INT          NOT NULL,
    exam_category     VARCHAR(200) NULL,
    exam_date         DATE         NULL,
    grade_type_id     INT          NULL,        -- FK → grade_types
    CONSTRAINT PK_exam_master            PRIMARY KEY (id),
    CONSTRAINT FK_exam_master_exam_type  FOREIGN KEY (exam_type_id)     REFERENCES exam_types(exam_type_id),
    CONSTRAINT FK_exam_master_class      FOREIGN KEY (class_id)         REFERENCES classes(class_id),
    CONSTRAINT FK_exam_master_year       FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
    CONSTRAINT FK_exam_master_subject    FOREIGN KEY (subject_id)       REFERENCES subjects(subject_id),
    CONSTRAINT FK_exam_master_grade_type FOREIGN KEY (grade_type_id)    REFERENCES grade_types(id)
);
GO


-- ============================================================
-- 47. r2_configs
--     Per-school Cloudflare R2 (S3-compatible) storage credentials.
--     secret_access_key is never returned by the GET endpoint.
--     One bucket per school; folders: student-images/, homeworks/{year}/{id}/,
--     announcements/{id}/, events/{id}/.
-- ============================================================
CREATE TABLE r2_configs (
    config_id         INT           NOT NULL IDENTITY(1,1),
    school_id         INT           NOT NULL,
    account_id        VARCHAR(100)  NOT NULL,   -- Cloudflare account id (S3 endpoint host)
    access_key_id     VARCHAR(200)  NOT NULL,
    secret_access_key VARCHAR(500)  NOT NULL,   -- never returned by GET
    bucket_name       VARCHAR(100)  NOT NULL,
    public_base_url   VARCHAR(300)  NOT NULL,   -- e.g. https://pub-xxx.r2.dev (no trailing slash)
    is_enabled        BIT           NOT NULL DEFAULT 1,
    created_by        VARCHAR(150)  NULL,
    updated_at        DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_r2_configs        PRIMARY KEY (config_id),
    CONSTRAINT UQ_r2_configs_school UNIQUE (school_id)
);
GO


-- ============================================================
-- 48. media_uploads
--     Generic R2-uploaded files for homework / announcements / events.
--     file_url is the R2 public URL; shown to parents in the mobile app.
-- ============================================================
CREATE TABLE media_uploads (
    upload_id    INT           NOT NULL IDENTITY(1,1),
    entity_type  VARCHAR(20)   NOT NULL,     -- homework | announcement | event
    entity_id    BIGINT        NOT NULL,
    file_name    VARCHAR(300)  NOT NULL,
    file_url     VARCHAR(1000) NOT NULL,     -- R2 public URL
    file_type    VARCHAR(20)   NULL,         -- image | doc | audio | video
    file_size_kb INT           NULL,
    school_id    INT           NOT NULL,
    created_by   VARCHAR(150)  NULL,
    created_at   DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_media_uploads PRIMARY KEY (upload_id)
);
CREATE INDEX IX_media_uploads_entity ON media_uploads (entity_type, entity_id);
GO

-- ============================================================
-- 49. class_teacher_assignments
--     Maps a class (+ optional section) to its teacher(s) for an academic year.
--     section_id NULL = the assignment covers the whole class (all sections).
--     Several teachers may share one class+section; any of them can reply to a
--     parent's message. Unassigning is a hard DELETE — message history is kept
--     on the messages rows themselves, not here.
-- ============================================================
CREATE TABLE class_teacher_assignments (
    assignment_id    INT          NOT NULL IDENTITY(1,1),
    academic_year_id INT          NOT NULL,
    class_id         INT          NOT NULL,
    section_id       INT          NULL,        -- NULL = whole class (all sections)
    user_id          INT          NOT NULL,    -- teacher login (users.user_id)
    school_id        INT          NOT NULL,
    created_by       VARCHAR(150) NULL,
    created_at       DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_class_teacher_assignments PRIMARY KEY (assignment_id),
    CONSTRAINT FK_cta_academic_year FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
    CONSTRAINT FK_cta_class         FOREIGN KEY (class_id)         REFERENCES classes(class_id),
    CONSTRAINT FK_cta_section       FOREIGN KEY (section_id)       REFERENCES sections(section_id),
    CONSTRAINT FK_cta_user          FOREIGN KEY (user_id)          REFERENCES users(user_id)
);
GO
-- One row per teacher per class+section. TWO filtered indexes, not one: SQL Server
-- treats NULLs as distinct in a plain unique index, so a single index would let the
-- same teacher be assigned to the same class twice whenever section_id IS NULL.
CREATE UNIQUE INDEX UQ_cta_section ON class_teacher_assignments
    (school_id, academic_year_id, class_id, section_id, user_id) WHERE section_id IS NOT NULL;
GO
CREATE UNIQUE INDEX UQ_cta_class ON class_teacher_assignments
    (school_id, academic_year_id, class_id, user_id) WHERE section_id IS NULL;
GO
CREATE INDEX IX_cta_lookup ON class_teacher_assignments (school_id, academic_year_id, class_id, section_id);
GO

-- ============================================================
-- 50. message_threads
--     One continuous parent <-> teacher conversation per child.
--     Keyed on student_unique_id (STABLE across promotions), NOT student_id,
--     which is a fresh IDENTITY row each academic year.
--     parent_id points at ascent_master.parent_accounts — cross-DB, so no FK.
--     Which teachers see a thread is resolved LIVE from the student's current
--     class+section via class_teacher_assignments, so promotion re-routes it.
-- ============================================================
CREATE TABLE message_threads (
    thread_id         INT          NOT NULL IDENTITY(1,1),
    student_unique_id INT          NOT NULL,   -- stable cross-year student id
    parent_id         INT          NOT NULL,   -- master DB parent_accounts.parent_id (no FK: other DB)
    status            VARCHAR(10)  NOT NULL DEFAULT 'Active',  -- Active | Blocked
    blocked_by_type   VARCHAR(10)  NULL,       -- parent | teacher
    blocked_by_id     INT          NULL,
    blocked_at        DATETIME     NULL,
    last_message_at   DATETIME     NULL,
    school_id         INT          NOT NULL,
    created_at        DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_message_threads PRIMARY KEY (thread_id),
    CONSTRAINT UQ_message_threads_student_parent UNIQUE (school_id, student_unique_id, parent_id)
);
CREATE INDEX IX_message_threads_recent ON message_threads (school_id, last_message_at DESC);
GO

-- ============================================================
-- 51. messages
--     sender_id is a parent_accounts.parent_id OR a users.user_id depending on
--     sender_type — no FK for that reason. sender_name is a display snapshot so
--     history survives a staff member leaving.
--     status Removed = hidden by an admin after a report (Play UGC requirement).
-- ============================================================
CREATE TABLE messages (
    message_id  INT           NOT NULL IDENTITY(1,1),
    thread_id   INT           NOT NULL,
    sender_type VARCHAR(10)   NOT NULL,   -- parent | teacher
    sender_id   INT           NOT NULL,   -- parent_id or users.user_id (per sender_type)
    sender_name VARCHAR(150)  NULL,       -- display snapshot at send time
    body        VARCHAR(2000) NOT NULL,
    status      VARCHAR(10)   NOT NULL DEFAULT 'Active',  -- Active | Removed
    read_at     DATETIME      NULL,       -- when the OTHER side first read it
    school_id   INT           NOT NULL,
    created_at  DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_messages        PRIMARY KEY (message_id),
    CONSTRAINT FK_messages_thread FOREIGN KEY (thread_id) REFERENCES message_threads(thread_id)
);
CREATE INDEX IX_messages_thread ON messages (thread_id, created_at);
GO

-- ============================================================
-- 52. message_reports
--     Play Store UGC requirement: either party can report a message, and an
--     admin must have a path to review and remove it (school web app).
-- ============================================================
CREATE TABLE message_reports (
    report_id        INT          NOT NULL IDENTITY(1,1),
    message_id       INT          NOT NULL,
    thread_id        INT          NOT NULL,
    reported_by_type VARCHAR(10)  NOT NULL,   -- parent | teacher
    reported_by_id   INT          NOT NULL,
    reason           VARCHAR(500) NULL,
    status           VARCHAR(10)  NOT NULL DEFAULT 'Open',  -- Open | Reviewed | Removed
    reviewed_by      VARCHAR(150) NULL,
    reviewed_at      DATETIME     NULL,
    school_id        INT          NOT NULL,
    created_at       DATETIME     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_message_reports         PRIMARY KEY (report_id),
    CONSTRAINT FK_message_reports_message FOREIGN KEY (message_id) REFERENCES messages(message_id),
    CONSTRAINT FK_message_reports_thread  FOREIGN KEY (thread_id)  REFERENCES message_threads(thread_id)
);
CREATE INDEX IX_message_reports_open ON message_reports (school_id, status, created_at DESC);
GO
