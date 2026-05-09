using System;

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
}
