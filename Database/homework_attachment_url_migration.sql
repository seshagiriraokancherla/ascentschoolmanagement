-- homework_attachment_url_migration.sql
-- Adds attachment_url column to the homework table on existing tenant DBs.

USE [ascent_group_1]  -- ← change to target tenant DB before running
GO

ALTER TABLE homework
    ADD attachment_url VARCHAR(4000) NULL   -- optional PDF/doc link (Google Drive, Cloudinary)
GO
