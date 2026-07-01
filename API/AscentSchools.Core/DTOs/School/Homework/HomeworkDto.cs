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
        public DateTime AssignedDate  { get; set; }
        public DateTime DueDate       { get; set; }
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
        public DateTime AssignedDate  { get; set; }
        public DateTime DueDate       { get; set; }
        public string   AttachmentUrl { get; set; }
    }

    /// <summary>Multi-subject daily homework entry (one row saved per filled subject).
    /// Re-saving the same Class+Section+Date replaces that day's existing rows.</summary>
    public class BatchHomeworkRequest
    {
        public int?     ClassId      { get; set; }
        public int?     SectionId    { get; set; }
        public DateTime AssignedDate { get; set; }
        public List<BatchHomeworkItem> Items { get; set; }
    }

    public class BatchHomeworkItem
    {
        public int?    SubjectId   { get; set; }
        public string  Description { get; set; }
    }
}
