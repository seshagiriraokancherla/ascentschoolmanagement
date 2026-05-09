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
('HOSTEL.MANAGE',               'HOSTEL',           'Manage hostel rooms and allocation');
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
-- 17. fee_structures  (was SAS_FeeMaster)
--     Fee amount per class + category + fee type + term
-- ============================================================
CREATE TABLE fee_structures (
    fee_structure_id    INT             NOT NULL IDENTITY(1,1),
    fee_category_id     INT             NULL,
    class_id            INT             NULL,
    fee_type_id         INT             NULL,
    fee_type_name       VARCHAR(25)     NULL,   -- Nullable; UI sends null
    term_id             INT             NULL,
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
-- 21. students  (was SAS_StudentMaster)
--     PK: BIGINT IDENTITY (was FLOAT in VB6 — critical fix)
-- ============================================================
CREATE TABLE students (
    student_id                  BIGINT          NOT NULL IDENTITY(1,1),
    admission_no                VARCHAR(20)     NULL,   -- School-issued human-readable ID
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
    hostel_name                 VARCHAR(10)     NULL,   -- NULL; hostel table TBD
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
    CONSTRAINT FK_students_bus              FOREIGN KEY (bus_id)           REFERENCES buses(bus_id)
);
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
    amount              DECIMAL(12,2)   NOT NULL DEFAULT 0,   -- Face amount collected
    concession_amount   DECIMAL(12,2)   NOT NULL DEFAULT 0,   -- Concession applied
    net_amount          DECIMAL(12,2)   NOT NULL DEFAULT 0,   -- amount - concession
    school_id           INT             NOT NULL,
    CONSTRAINT PK_fee_receipt_items             PRIMARY KEY (item_id),
    CONSTRAINT FK_fee_receipt_items_receipt     FOREIGN KEY (receipt_id)  REFERENCES fee_receipts(receipt_id),
    CONSTRAINT FK_fee_receipt_items_fee_type    FOREIGN KEY (fee_type_id) REFERENCES fee_types(fee_type_id),
    CONSTRAINT FK_fee_receipt_items_term        FOREIGN KEY (term_id)     REFERENCES terms(term_id)
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
    assigned_date   DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    due_date        DATE            NOT NULL,
    school_id       INT             NOT NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    created_by      VARCHAR(100)    NOT NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_by      VARCHAR(100)    NULL,
    updated_at      DATETIME        NULL,
    attachment_url  VARCHAR(4000)   NULL,       -- optional PDF/doc link (Google Drive, Cloudinary)
    CONSTRAINT PK_homework          PRIMARY KEY (homework_id),
    CONSTRAINT FK_homework_subject  FOREIGN KEY (subject_id) REFERENCES subjects(subject_id),
    CONSTRAINT FK_homework_class    FOREIGN KEY (class_id)   REFERENCES classes(class_id)
);
CREATE INDEX IX_homework_class_due ON homework (class_id, due_date, school_id);
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
    is_pinned       BIT             NOT NULL DEFAULT 0,
    school_id       INT             NOT NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    created_by      VARCHAR(100)    NOT NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    updated_by      VARCHAR(100)    NULL,
    updated_at      DATETIME        NULL,
    attachment_url  VARCHAR(4000)   NULL,       -- optional PDF/doc link (Google Drive, Cloudinary)
    CONSTRAINT PK_announcements         PRIMARY KEY (announcement_id),
    CONSTRAINT FK_announcements_class   FOREIGN KEY (class_id) REFERENCES classes(class_id)
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
    media_url       VARCHAR(4000)   NOT NULL,                   -- Cloudinary URL or YouTube URL
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
ALTER TABLE homework                                                                                  
        ADD section_id INT NULL,                                                                              
            CONSTRAINT FK_homework_section FOREIGN KEY (section_id) REFERENCES sections(section_id)
            