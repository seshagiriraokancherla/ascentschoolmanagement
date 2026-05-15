-- Transport concession migration
-- Adds bus_route_id to fee_concessions so transport concessions link to a
-- specific bus route rather than a fee_type.
-- Also makes fee_type_id nullable to accommodate these transport rows.
-- Run on all existing tenant DBs.  New DBs get it via the updated tenant_tables.sql.

USE master;
GO

-- Replace N'ascent_group_1' with the actual tenant DB name before running.
USE ascent_group_1;
GO

-- 1. Make fee_type_id nullable (transport concessions have no fee_type)
ALTER TABLE fee_concessions
    ALTER COLUMN fee_type_id INT NULL;
GO

-- 2. Add bus_route_id column
ALTER TABLE fee_concessions
    ADD bus_route_id INT NULL;
GO

ALTER TABLE fee_concessions
    ADD CONSTRAINT FK_fee_concessions_bus_route
        FOREIGN KEY (bus_route_id) REFERENCES bus_routes(route_id);
GO

-- 3. Recreate existing unique indexes to restrict them to school-fee rows (fee_type_id IS NOT NULL)
DROP INDEX UQ_fee_concessions_term   ON fee_concessions;
DROP INDEX UQ_fee_concessions_period ON fee_concessions;
GO

CREATE UNIQUE INDEX UQ_fee_concessions_term
    ON fee_concessions (student_id, fee_type_id, school_id, term_id)
    WHERE status = 'Active' AND term_id IS NOT NULL AND fee_type_id IS NOT NULL;
GO

CREATE UNIQUE INDEX UQ_fee_concessions_period
    ON fee_concessions (student_id, fee_type_id, school_id, fee_period_id)
    WHERE status = 'Active' AND fee_period_id IS NOT NULL AND fee_type_id IS NOT NULL;
GO

-- 4. New index for transport concessions (one active concession per student+route+term)
CREATE UNIQUE INDEX UQ_fee_concessions_transport
    ON fee_concessions (student_id, bus_route_id, school_id, term_id)
    WHERE status = 'Active' AND bus_route_id IS NOT NULL AND term_id IS NOT NULL;
GO
