using System;
using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Homework
{
    public class HomeworkDto
    {
        public int      HomeworkId    { get; set; }
        public string   Title         { get; set; }
        public string   Description   { get; set; }
        public int?     SubjectId     { get; set; }
        public string   SubjectName   { get; set; }
        public int?     ClassId       { get; set; }
        public string   ClassName     { get; set; }
        public int?     SectionId     { get; set; }
        public string   SectionName   { get; set; }
        public DateTime  AssignedDate  { get; set; }
        public DateTime? DueDate       { get; set; }   // retired — null for new homework
        public string   Status        { get; set; }
        public string   AttachmentUrl { get; set; }
        public string   CreatedBy     { get; set; }
        public DateTime CreatedAt     { get; set; }
    }

    public class SaveHomeworkRequest
    {
        public string   Title         { get; set; }
        public string   Description   { get; set; }
        public int?     SubjectId     { get; set; }
        public int?     ClassId       { get; set; }
        public int?     SectionId     { get; set; }
        public DateTime  AssignedDate  { get; set; }
        public DateTime? DueDate       { get; set; }   // retired — clients no longer send it
        public string   AttachmentUrl { get; set; }
    }

    /// <summary>Multi-subject daily homework entry (one row saved per filled subject
    /// per target section). Re-saving the same Class+Date replaces that day's rows for
    /// the sections being written.</summary>
    public class BatchHomeworkRequest
    {
        public int?     ClassId      { get; set; }
        /// <summary>Single-section form, kept for older clients. Only read when
        /// SectionIds is empty; null = class-wide.</summary>
        public int?     SectionId    { get; set; }
        /// <summary>Sections to post to. Empty = class-wide. Listing every active
        /// section of the class collapses to one class-wide row set.</summary>
        public List<int> SectionIds  { get; set; }
        public DateTime AssignedDate { get; set; }
        public List<BatchHomeworkItem> Items { get; set; }
    }

    public class BatchHomeworkItem
    {
        public int?    SubjectId   { get; set; }
        public string  Description { get; set; }
    }

    /// <summary>What a batch save actually wrote. Sections holds a single null entry
    /// when the save collapsed to one class-wide (section_id NULL) row set.</summary>
    public class BatchHomeworkResult
    {
        public int        SubjectCount { get; set; }
        public int        RowCount     { get; set; }
        public List<int?> Sections     { get; set; }
    }
}
