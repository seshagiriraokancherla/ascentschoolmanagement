-- ============================================================
-- Ascent Schools Application — SQL Server Database Schema
-- Converted from VB6 SAS_* tables
-- Target: SQL Server (compatible with .NET 4.8 / ADO.NET / EF)
-- Naming: snake_case columns, PascalCase not used
-- All IDs: INT IDENTITY(1,1) PRIMARY KEY
-- ============================================================

-- ============================================================
-- 1. school_groups
--    New table — supports chain schools under one group
-- ============================================================
CREATE TABLE school_groups (
    group_id        INT             NOT NULL IDENTITY(1,1),
    group_name      VARCHAR(100)    NOT NULL,
    description     VARCHAR(200)    NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_school_groups PRIMARY KEY (group_id)
);
GO


-- ============================================================
-- 2. schools  (was SAS_LicenceDet)
-- ============================================================
CREATE TABLE schools (
    school_id                       INT             NOT NULL IDENTITY(1,1),
    group_id                        INT             NULL,
    school_name                     VARCHAR(150)    NOT NULL,
    school_caption                  VARCHAR(250)    NULL,
    address                         VARCHAR(205)    NULL,
    city                            VARCHAR(55)     NULL,
    district                        VARCHAR(55)     NULL,
    state                           VARCHAR(55)     NULL,
    pin_code                        VARCHAR(15)     NULL,
    landline                        VARCHAR(25)     NULL,
    mobile                          VARCHAR(35)     NULL,
    day_end_notification_mobiles    VARCHAR(66)     NULL,  -- Day-end SMS to admin persons
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
    CONSTRAINT PK_schools PRIMARY KEY (school_id),
    CONSTRAINT FK_schools_group FOREIGN KEY (group_id) REFERENCES school_groups(group_id)
);
GO


-- ============================================================
-- 3. audit_logs  (was SAS_TransLogData)
--    NOTE: user_id FK added after users table is created
-- ============================================================
CREATE TABLE audit_logs (
    log_id              INT             NOT NULL IDENTITY(1,1),
    reference_id        VARCHAR(10)     NULL,   -- ID of the record that was changed
    reference_type      VARCHAR(15)     NULL,   -- Table/entity name
    transaction_date    DATE            NULL,
    transaction_type    VARCHAR(15)     NULL,   -- Insert / Update / Delete
    transaction_time    DATETIME        NULL,
    user_id             INT             NULL,   -- FK → users.user_id (added below)
    machine_id          VARCHAR(10)     NULL,
    system_ip           VARCHAR(15)     NULL,
    form_name           VARCHAR(15)     NULL,   -- Screen / module name
    school_id           INT             NULL,
    CONSTRAINT PK_audit_logs PRIMARY KEY (log_id),
    CONSTRAINT FK_audit_logs_school FOREIGN KEY (school_id) REFERENCES schools(school_id)
);
GO


-- ============================================================
-- 4. users  (was SAS_UserMaster)
-- ============================================================
CREATE TABLE users (
    user_id         INT             NOT NULL IDENTITY(1,1),
    employee_id     INT             NULL,       -- FK → employee table (TBD)
    username        VARCHAR(50)     NOT NULL,
    password_hash   VARCHAR(255)    NOT NULL,   -- Store hashed password only
    access_type     VARCHAR(20)     NULL,
    status          VARCHAR(10)     NOT NULL DEFAULT 'Active',
    school_id       INT             NULL,
    audit_log_id    INT             NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    CONSTRAINT PK_users PRIMARY KEY (user_id),
    CONSTRAINT FK_users_school    FOREIGN KEY (school_id)    REFERENCES schools(school_id),
    CONSTRAINT FK_users_audit_log FOREIGN KEY (audit_log_id) REFERENCES audit_logs(log_id)
);
GO

-- Back-fill FK: audit_logs → users (circular ref resolved after both tables exist)
ALTER TABLE audit_logs
    ADD CONSTRAINT FK_audit_logs_user FOREIGN KEY (user_id) REFERENCES users(user_id);
GO


-- ============================================================
-- 5. academic_years  (was SAS_AcdYear)
--    Surrogate PK added; academic_year string retained as display value
-- ============================================================
CREATE TABLE academic_years (
    academic_year_id            INT             NOT NULL IDENTITY(1,1),
    academic_year               VARCHAR(55)     NOT NULL,  -- e.g. "2025-2026" or "25-29 B-Pharm"
    start_month                 VARCHAR(15)     NULL,
    end_month                   VARCHAR(15)     NULL,
    status                      VARCHAR(10)      NULL,
    registration_fee_frequency  VARCHAR(10)     NULL,  -- Yearly / Monthly / Termwise
    transport_fee_frequency     VARCHAR(10)     NULL,  -- Yearly / Monthly / Termwise
    hostel_fee_frequency        VARCHAR(10)     NULL,  -- Yearly / Monthly / Termwise
    boarding_type               VARCHAR(10)     NULL,  -- Day School / Hostel
    new_admissions_enabled      VARCHAR(10)     NULL,  -- Enable / Disable
    school_id                   INT             NULL,
    audit_log_id                INT             NULL,
    created_by                  VARCHAR(25)     NULL,
    created_at                  DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id                  VARCHAR(20)     NULL,
    CONSTRAINT PK_academic_years PRIMARY KEY (academic_year_id),
    CONSTRAINT FK_academic_years_school     FOREIGN KEY (school_id)    REFERENCES schools(school_id),
    CONSTRAINT FK_academic_years_audit_log  FOREIGN KEY (audit_log_id) REFERENCES audit_logs(log_id)
);
GO


-- ============================================================
-- 6. school_settings  (was SAS_SoftwareSettings)
--    One row per school — school_id is the PK
-- ============================================================
CREATE TABLE school_settings (
    school_id                       INT             NOT NULL,
    fee_receipt_lock                INT             NULL,
    fee_receipt_print               VARCHAR(10)     NULL,
    print_mode                      VARCHAR(25)     NULL,
    admission_no_type               VARCHAR(15)     NULL,
    receipt_printer                 VARCHAR(30)     NULL,
    reports_printer                 VARCHAR(30)     NULL,
    category_wise_admissions        VARCHAR(5)      NULL,  -- Y / N
    transport_fee_included          VARCHAR(5)      NULL,  -- Y / N
    fine_enabled                    VARCHAR(5)      NULL,  -- Y / N
    receipt_fee_type_separator      VARCHAR(5)      NULL,
    new_student_entry_mode          VARCHAR(10)     NULL,  -- Fast Entry / Normal Entry
    misc_print_mode                 VARCHAR(25)     NULL,
    backup_drive                    VARCHAR(25)     NULL,
    receipt_print_copies            INT             NULL,
    progress_report_type            VARCHAR(25)     NULL,
    billing_status                  VARCHAR(10)     NULL,
    other_subjects_type             VARCHAR(10)     NULL,
    pre_primary_admission_prefix    VARCHAR(15)     NULL,  -- Prefix for auto-gen admission no
    primary_admission_prefix        VARCHAR(15)     NULL,
    high_school_admission_prefix    VARCHAR(15)     NULL,
    general_admission_prefix        VARCHAR(15)     NULL,
    bill_no_series_type             VARCHAR(25)     NULL,  -- New Year/Academic Year/Financial Year/Continues/Cash-Bank/Category-wise
    webcam_name                     VARCHAR(25)     NULL,
    institution_head_signature      VARCHAR(255)    NULL,  -- File path / URL (not binary blob)
    institution_head_name           VARCHAR(20)     NULL,
    fee_message_to_teacher          VARCHAR(5)      NULL,  -- Y / N
    student_concession_enabled      VARCHAR(25)     NULL,  -- Enable / Disable discount for clerk
    integration_api_key             VARCHAR(50)     NULL,  -- Static API key for VB6 legacy integration
    audit_log_id                    INT             NULL,
    created_by                      VARCHAR(25)     NULL,
    created_at                      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id                      VARCHAR(20)     NULL,
    CONSTRAINT PK_school_settings PRIMARY KEY (school_id),
    CONSTRAINT FK_school_settings_school    FOREIGN KEY (school_id)    REFERENCES schools(school_id),
    CONSTRAINT FK_school_settings_audit_log FOREIGN KEY (audit_log_id) REFERENCES audit_logs(log_id)
);
GO


-- ============================================================
-- 7. class_groups  (was SAS_ClassGrps)
--    e.g. Pre-Primary, Primary, High-School, General
-- ============================================================
CREATE TABLE class_groups (
    class_group_id  INT             NOT NULL IDENTITY(1,1),
    group_name      VARCHAR(30)     NOT NULL,  -- Pre-Primary / Primary / High-School / General
    description     VARCHAR(50)     NULL,
    prefix          VARCHAR(5)      NULL,
    status          VARCHAR(10)      NULL,
    school_id       INT             NULL,
    audit_log_id    INT             NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    CONSTRAINT PK_class_groups PRIMARY KEY (class_group_id),
    CONSTRAINT FK_class_groups_school     FOREIGN KEY (school_id)    REFERENCES schools(school_id),
    CONSTRAINT FK_class_groups_audit_log  FOREIGN KEY (audit_log_id) REFERENCES audit_logs(log_id)
);
GO


-- ============================================================
-- 8. fee_categories  (was SAS_FeeCategory)
--    e.g. General Students Fee, Staff Child Fee, VTPS Staff Fee
--    Defined before classes because classes.fee_category_id references it
-- ============================================================
CREATE TABLE fee_categories (
    fee_category_id INT             NOT NULL IDENTITY(1,1),
    category_name   VARCHAR(25)     NOT NULL,
    academic_year_id INT            NULL,
    description     VARCHAR(30)     NULL,
    status          VARCHAR(10)      NULL,
    school_id       INT             NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    deleted_by      VARCHAR(60)     NULL,
    CONSTRAINT PK_fee_categories PRIMARY KEY (fee_category_id),
    CONSTRAINT FK_fee_categories_school FOREIGN KEY (school_id) REFERENCES schools(school_id)
    -- FK to academic_years added after academic_years table exists; see below
);
GO

ALTER TABLE fee_categories
    ADD CONSTRAINT FK_fee_categories_academic_year
    FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id);
GO


-- ============================================================
-- 9. classes  (was SAS_ClassNames)
-- ============================================================
CREATE TABLE classes (
    class_id        INT             NOT NULL IDENTITY(1,1),
    class_name      VARCHAR(55)     NOT NULL,  -- e.g. "1 Class", "XI th Grade Science"
    branch_name     VARCHAR(55)     NULL,       -- Stream: Science / Arts / Commerce
    description     VARCHAR(50)     NULL,
    status          VARCHAR(10)      NULL,
    class_group_id  INT             NULL,
    fee_category_id INT             NULL,       -- Default fee category for this class
    prefix_enabled  VARCHAR(5)      NULL,
    prefix_code     VARCHAR(5)      NULL,
    sequence_no     INT             NULL,
    school_id       INT             NULL,
    audit_log_id    INT             NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    CONSTRAINT PK_classes PRIMARY KEY (class_id),
    CONSTRAINT FK_classes_school        FOREIGN KEY (school_id)      REFERENCES schools(school_id),
    CONSTRAINT FK_classes_class_group   FOREIGN KEY (class_group_id) REFERENCES class_groups(class_group_id),
    CONSTRAINT FK_classes_fee_category  FOREIGN KEY (fee_category_id) REFERENCES fee_categories(fee_category_id),
    CONSTRAINT FK_classes_audit_log     FOREIGN KEY (audit_log_id)   REFERENCES audit_logs(log_id)
);
GO


-- ============================================================
-- 10. fee_types  (was SAS_FeeTypes)
-- ============================================================
CREATE TABLE fee_types (
    fee_type_id     INT             NOT NULL IDENTITY(1,1),
    fee_type_name   VARCHAR(25)     NOT NULL,
    academic_year_id INT            NULL,
    term_name       VARCHAR(25)     NULL,
    sequence_no     INT             NULL,
    description     VARCHAR(50)     NULL,
    status          VARCHAR(10)      NULL,
    school_id       INT             NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    deleted_by      VARCHAR(60)     NULL,
    CONSTRAINT PK_fee_types PRIMARY KEY (fee_type_id),
    CONSTRAINT FK_fee_types_school         FOREIGN KEY (school_id)       REFERENCES schools(school_id),
    CONSTRAINT FK_fee_types_academic_year  FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
);
GO


-- ============================================================
-- 11. terms  (was SAS_TermMonthData)
--    e.g. "1st Term", "2nd Term", "January-2025"
-- ============================================================
CREATE TABLE terms (
    term_id         INT             NOT NULL IDENTITY(1,1),
    term_name       VARCHAR(20)     NOT NULL,  -- "1st Term" / "January-2025"
    year_name       VARCHAR(10)     NULL,
    order_no        INT             NULL,
    description     VARCHAR(30)     NULL,
    academic_year_id INT            NULL,
    status          VARCHAR(10)      NULL,
    school_id       INT             NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    deleted_by      VARCHAR(60)     NULL,
    CONSTRAINT PK_terms PRIMARY KEY (term_id),
    CONSTRAINT FK_terms_school         FOREIGN KEY (school_id)        REFERENCES schools(school_id),
    CONSTRAINT FK_terms_academic_year  FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
);
GO


-- ============================================================
-- 12. subjects  (was SAS_Subjects)
-- ============================================================
CREATE TABLE subjects (
    subject_id      INT             NOT NULL IDENTITY(1,1),
    subject_name    VARCHAR(30)     NOT NULL,
    short_name      VARCHAR(15)     NULL,
    subject_type    VARCHAR(20)     NULL,  -- Theory / Language / General / Practical / Others
    description     VARCHAR(50)     NULL,
    remarks         VARCHAR(50)     NULL,
    academic_year_id INT            NULL,
    status          VARCHAR(10)      NULL,
    school_id       INT             NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    deleted_by      VARCHAR(60)     NULL,
    CONSTRAINT PK_subjects PRIMARY KEY (subject_id),
    CONSTRAINT FK_subjects_school         FOREIGN KEY (school_id)        REFERENCES schools(school_id),
    CONSTRAINT FK_subjects_academic_year  FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
);
GO


-- ============================================================
-- 13. payment_modes  (was SAS_PaymentMods)
--    Configurable per school: Cash / Cheque / Online / etc.
-- ============================================================
CREATE TABLE payment_modes (
    payment_mode_id INT             NOT NULL IDENTITY(1,1),
    mode_name       VARCHAR(15)     NOT NULL,  -- Cash / Cheque / Online
    description     VARCHAR(25)     NULL,
    status          VARCHAR(10)      NULL,
    school_id       INT             NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    deleted_by      VARCHAR(60)     NULL,
    CONSTRAINT PK_payment_modes PRIMARY KEY (payment_mode_id),
    CONSTRAINT FK_payment_modes_school FOREIGN KEY (school_id) REFERENCES schools(school_id)
);
GO


-- ============================================================
-- 14. fee_structures  (was SAS_FeeMaster)
--    Fee amount per class + fee category + fee type + term
-- ============================================================
CREATE TABLE fee_structures (
    fee_structure_id    INT             NOT NULL IDENTITY(1,1),
    fee_category_id     INT             NULL,
    class_id            INT             NULL,
    fee_type_id         INT             NULL,
    fee_type_name       VARCHAR(25)     NULL,   -- Nullable; populated from UI if needed
    term_id             INT             NULL,
    amount              DECIMAL(12,2)   NULL,
    description         VARCHAR(50)     NULL,
    status              VARCHAR(10)      NULL,
    academic_year_id    INT             NULL,
    payment_mode_id     INT             NULL,
    admission_type      VARCHAR(5)      NULL,   -- New / Old student
    school_id           INT             NULL,
    created_by          VARCHAR(25)     NULL,
    created_at          DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id          VARCHAR(20)     NULL,
    deleted_by          VARCHAR(60)     NULL,
    CONSTRAINT PK_fee_structures PRIMARY KEY (fee_structure_id),
    CONSTRAINT FK_fee_structures_school        FOREIGN KEY (school_id)       REFERENCES schools(school_id),
    CONSTRAINT FK_fee_structures_fee_category  FOREIGN KEY (fee_category_id) REFERENCES fee_categories(fee_category_id),
    CONSTRAINT FK_fee_structures_class         FOREIGN KEY (class_id)        REFERENCES classes(class_id),
    CONSTRAINT FK_fee_structures_fee_type      FOREIGN KEY (fee_type_id)     REFERENCES fee_types(fee_type_id),
    CONSTRAINT FK_fee_structures_term          FOREIGN KEY (term_id)         REFERENCES terms(term_id),
    CONSTRAINT FK_fee_structures_payment_mode  FOREIGN KEY (payment_mode_id) REFERENCES payment_modes(payment_mode_id),
    CONSTRAINT FK_fee_structures_academic_year FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
);
GO


-- ============================================================
-- 15. buses  (was SAS_BussData)
-- ============================================================
CREATE TABLE buses (
    bus_id          INT             NOT NULL IDENTITY(1,1),
    bus_name        VARCHAR(20)     NULL,
    model           VARCHAR(25)     NULL,
    driver_id       INT             NULL,   -- FK → employee table (TBD)
    capacity        INT             NULL,
    description     VARCHAR(50)     NULL,
    status          VARCHAR(10)      NULL,
    purchase_date   DATE            NULL,
    registration_no VARCHAR(25)     NULL,
    trip_data       VARCHAR(10)     NULL,   -- Nullable; UI sends null
    cleaner_name    VARCHAR(10)     NULL,   -- Nullable; UI sends null
    owner_data      VARCHAR(60)     NULL,   -- Nullable; UI sends null
    route_name      VARCHAR(25)     NULL,   -- Nullable; UI sends null
    school_id       INT             NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    deleted_by      VARCHAR(60)     NULL,
    CONSTRAINT PK_buses PRIMARY KEY (bus_id),
    CONSTRAINT FK_buses_school FOREIGN KEY (school_id) REFERENCES schools(school_id)
);
GO


-- ============================================================
-- 16. bus_routes  (was SAS_BusRoutes)
-- ============================================================
CREATE TABLE bus_routes (
    route_id        INT             NOT NULL IDENTITY(1,1),
    route_name      VARCHAR(55)     NOT NULL,
    route_code      VARCHAR(10)     NULL,
    route_category  VARCHAR(20)     NULL,
    description     VARCHAR(50)     NULL,
    status          VARCHAR(10)      NULL,
    bus_no_data     VARCHAR(15)     NULL,   -- Nullable; UI sends null
    school_id       INT             NULL,
    created_by      VARCHAR(25)     NULL,
    created_at      DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id      VARCHAR(20)     NULL,
    deleted_by      VARCHAR(60)     NULL,
    CONSTRAINT PK_bus_routes PRIMARY KEY (route_id),
    CONSTRAINT FK_bus_routes_school FOREIGN KEY (school_id) REFERENCES schools(school_id)
);
GO


-- ============================================================
-- 17. bus_fee_structures  (was SAS_BusMaster)
--    Bus fee per route per term per academic year
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
    school_id               INT             NULL,
    created_by              VARCHAR(25)     NULL,
    created_at              DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id              VARCHAR(20)     NULL,
    deleted_by              VARCHAR(60)     NULL,
    CONSTRAINT PK_bus_fee_structures PRIMARY KEY (bus_fee_structure_id),
    CONSTRAINT FK_bus_fee_structures_school        FOREIGN KEY (school_id)        REFERENCES schools(school_id),
    CONSTRAINT FK_bus_fee_structures_route         FOREIGN KEY (route_id)         REFERENCES bus_routes(route_id),
    CONSTRAINT FK_bus_fee_structures_term          FOREIGN KEY (term_id)          REFERENCES terms(term_id),
    CONSTRAINT FK_bus_fee_structures_academic_year FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id)
);
GO


-- ============================================================
-- 18. students  (was SAS_StudentMaster)
--    PK changed from FLOAT to BIGINT IDENTITY
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
    family_children_count       INT             NULL,   -- No. of kids in family
    dob_proof_submitted         VARCHAR(10)     NULL,   -- Submitted / Not Submitted
    aadhar_no                   VARCHAR(25)     NULL,
    caste_cert_submitted        VARCHAR(10)     NULL,   -- Submitted / Not Submitted
    other_certificates          VARCHAR(50)     NULL,
    transport_type              VARCHAR(10)     NULL,   -- Bus / Walking / etc.
    bus_route_id                INT             NULL,
    joining_class               VARCHAR(20)     NULL,   -- Class at first admission (history)
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
    hostel_name                 VARCHAR(10)     NULL,   -- NULL; hostel table to be designed later
    biometric_id                VARCHAR(5)      NULL,   -- Biometric device enrollment ID
    mother_tongue               VARCHAR(25)     NULL,
    first_language              VARCHAR(25)     NULL,
    second_language             VARCHAR(10)     NULL,
    third_language              VARCHAR(25)     NULL,
    udise_no                    VARCHAR(35)     NULL,   -- Govt UDISE number
    spare_field_1               VARCHAR(50)     NULL,   -- Reserved for future use
    spare_field_2               VARCHAR(50)     NULL,   -- Reserved for future use
    status                      VARCHAR(10)     NULL,
    school_id                   INT             NULL,
    created_by                  VARCHAR(25)     NULL,
    created_at                  DATETIME        NOT NULL DEFAULT GETDATE(),
    machine_id                  VARCHAR(25)     NULL,
    deleted_by                  VARCHAR(60)     NULL,
    CONSTRAINT PK_students PRIMARY KEY (student_id),
    CONSTRAINT FK_students_school          FOREIGN KEY (school_id)       REFERENCES schools(school_id),
    CONSTRAINT FK_students_academic_year   FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
    CONSTRAINT FK_students_fee_category    FOREIGN KEY (fee_category_id) REFERENCES fee_categories(fee_category_id),
    CONSTRAINT FK_students_class           FOREIGN KEY (class_id)        REFERENCES classes(class_id),
    CONSTRAINT FK_students_bus_route       FOREIGN KEY (bus_route_id)    REFERENCES bus_routes(route_id),
    CONSTRAINT FK_students_bus             FOREIGN KEY (bus_id)          REFERENCES buses(bus_id)
);
GO
