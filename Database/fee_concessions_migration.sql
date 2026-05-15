-- ============================================================
-- Fee Concessions Migration
-- Run on each existing tenant DB (ascent_group_N)
-- Adds: fee_concessions table
-- ============================================================

USE ascent_group_1;   -- change to target DB before running
GO

CREATE TABLE fee_concessions (
    concession_id     INT           NOT NULL IDENTITY(1,1),
    school_id         INT           NOT NULL,
    academic_year_id  INT           NOT NULL,
    student_id        BIGINT        NOT NULL,
    student_unique_id INT           NULL,
    fee_type_id       INT           NOT NULL,
    term_id           INT           NULL,        -- FK → terms; set for Term-based fee structures
    fee_period_id     INT           NULL,        -- FK → fee_periods; set for Monthly fee structures
    concession_type   VARCHAR(30)   NOT NULL,   -- Poor / Not Applicable / Staff Child
    amount            DECIMAL(12,2) NOT NULL,
    remarks           VARCHAR(500)  NULL,
    receipt_no        VARCHAR(25)   NOT NULL,
    status            VARCHAR(10)   NOT NULL DEFAULT 'Active',
    created_by        VARCHAR(25)   NULL,
    created_at        DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_fee_concessions          PRIMARY KEY (concession_id),
    CONSTRAINT FK_fee_concessions_students FOREIGN KEY (student_id)       REFERENCES students(student_id),
    CONSTRAINT FK_fee_concessions_types    FOREIGN KEY (fee_type_id)      REFERENCES fee_types(fee_type_id),
    CONSTRAINT FK_fee_concessions_ay       FOREIGN KEY (academic_year_id) REFERENCES academic_years(academic_year_id),
    CONSTRAINT FK_fee_concessions_term     FOREIGN KEY (term_id)          REFERENCES terms(term_id),
    CONSTRAINT FK_fee_concessions_period   FOREIGN KEY (fee_period_id)    REFERENCES fee_periods(fee_period_id)
);
GO

-- One active concession per student+fee_type+term (for Term-based fee structures)
CREATE UNIQUE INDEX UQ_fee_concessions_term
    ON fee_concessions (student_id, fee_type_id, school_id, term_id)
    WHERE status = 'Active' AND term_id IS NOT NULL;
GO

-- One active concession per student+fee_type+period (for Monthly fee structures)
CREATE UNIQUE INDEX UQ_fee_concessions_period
    ON fee_concessions (student_id, fee_type_id, school_id, fee_period_id)
    WHERE status = 'Active' AND fee_period_id IS NOT NULL;
GO
