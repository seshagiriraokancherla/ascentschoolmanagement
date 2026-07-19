using System;

namespace AscentSchools.Core.DTOs.School.ClassTeachers
{
    public class ClassTeacherDto
    {
        public int      AssignmentId   { get; set; }
        public int      AcademicYearId { get; set; }
        public int      ClassId        { get; set; }
        public string   ClassName      { get; set; }
        public int?     SectionId      { get; set; }   // NULL = whole class
        public string   SectionName    { get; set; }
        public int      UserId         { get; set; }
        public string   FullName       { get; set; }
        public string   Username       { get; set; }
        public string   Designation    { get; set; }
        public string   CreatedBy      { get; set; }
        public DateTime CreatedAt      { get; set; }
        public int      SchoolId       { get; set; }
    }

    public class SaveClassTeacherRequest
    {
        public int  AcademicYearId { get; set; }
        public int  ClassId        { get; set; }
        public int? SectionId      { get; set; }   // NULL / 0 = whole class
        public int  UserId         { get; set; }
    }

    /// <summary>Active logins available to assign as a class teacher (dropdown source).</summary>
    public class AssignableTeacherDto
    {
        public int    UserId      { get; set; }
        public string FullName    { get; set; }
        public string Username    { get; set; }
        public string Designation { get; set; }
    }
}
