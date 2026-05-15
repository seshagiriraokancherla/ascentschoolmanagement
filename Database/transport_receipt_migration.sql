-- Transport fee receipt migration
-- Adds bus_route_id to fee_receipt_items so transport receipts link to a
-- specific bus route rather than a fee_type.
-- Run on all existing tenant DBs. New DBs get it via the updated tenant_tables.sql.

USE master;
GO

-- Replace N'ascent_group_1' with the actual tenant DB name before running.
USE ascent_group_1;
GO

ALTER TABLE fee_receipt_items
    ADD bus_route_id INT NULL;
GO

ALTER TABLE fee_receipt_items
    ADD CONSTRAINT FK_fee_receipt_items_bus_route
        FOREIGN KEY (bus_route_id) REFERENCES bus_routes(route_id);
GO
