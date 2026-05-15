-- Hostel management migration
-- Adds hostels master, hostel_fee_structures, and hostel_id to students,
-- fee_receipt_items, and fee_concessions.
-- Run on all existing tenant DBs.  New DBs get all tables via tenant_tables.sql.

USE master;
GO

-- Replace N'ascent_group_1' with the actual tenant DB name before running.
USE ascent_group_1;
GO

-- ============================================================
-- 1. hostels master table
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
    CONSTRAINT PK_hostels       PRIMARY KEY (hostel_id),
    CONSTRAINT UQ_hostels_name  UNIQUE (hostel_name, school_id)
);
GO


-- ============================================================
-- 2. hostel_fee_structures
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
-- 3. Add hostel_id to students (alongside existing hostel_name)
-- ============================================================
ALTER TABLE students ADD hostel_id INT NULL;
GO
ALTER TABLE students ADD CONSTRAINT FK_students_hostel
    FOREIGN KEY (hostel_id) REFERENCES hostels(hostel_id);
GO


-- ============================================================
-- 4. Add hostel_id to fee_receipt_items
-- ============================================================
ALTER TABLE fee_receipt_items ADD hostel_id INT NULL;
GO
ALTER TABLE fee_receipt_items ADD CONSTRAINT FK_fee_receipt_items_hostel
    FOREIGN KEY (hostel_id) REFERENCES hostels(hostel_id);
GO


-- ============================================================
-- 5. Add hostel_id to fee_concessions + new unique indexes
-- ============================================================
ALTER TABLE fee_concessions ADD hostel_id INT NULL;
GO
ALTER TABLE fee_concessions ADD CONSTRAINT FK_fee_concessions_hostel
    FOREIGN KEY (hostel_id) REFERENCES hostels(hostel_id);
GO

-- One active hostel concession per student+hostel+term
CREATE UNIQUE INDEX UQ_fee_concessions_hostel
    ON fee_concessions (student_id, hostel_id, school_id, term_id)
    WHERE status = 'Active' AND hostel_id IS NOT NULL AND term_id IS NOT NULL;
GO

-- One active hostel concession per student+hostel+period (monthly)
CREATE UNIQUE INDEX UQ_fee_concessions_hostel_period
    ON fee_concessions (student_id, hostel_id, school_id, fee_period_id)
    WHERE status = 'Active' AND hostel_id IS NOT NULL AND fee_period_id IS NOT NULL;
GO
