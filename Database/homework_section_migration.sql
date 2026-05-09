-- Add section_id to homework table
ALTER TABLE homework
ADD section_id INT NULL,
    CONSTRAINT FK_homework_section FOREIGN KEY (section_id) REFERENCES sections(section_id);
GO
