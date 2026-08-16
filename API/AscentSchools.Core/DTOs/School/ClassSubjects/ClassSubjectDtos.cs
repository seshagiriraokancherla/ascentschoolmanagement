using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.ClassSubjects
{
    /// <summary>One subject mapped to a class for an academic year.</summary>
    public class ClassSubjectDto
    {
        public int      ClassSubjectId { get; set; }
        public int      AcademicYearId { get; set; }
        public int      ClassId        { get; set; }
        public string   ClassName      { get; set; }
        public int      SubjectId      { get; set; }
        public string   SubjectName    { get; set; }
        public string   ShortName      { get; set; }
        public string   SubjectType    { get; set; }
        public int?     DisplayOrder   { get; set; }
        public bool     IsOptional     { get; set; }
        public string   Status         { get; set; }
    }

    /// <summary>A subject available to add to a class (dropdown source).</summary>
    public class AvailableSubjectDto
    {
        public int    SubjectId   { get; set; }
        public string SubjectName { get; set; }
        public string ShortName   { get; set; }
        public string SubjectType { get; set; }
    }

    /// <summary>
    /// Full replacement of a class's subject set for one academic year
    /// (delete + insert in a transaction, like fee-structure save).
    /// </summary>
    public class SaveClassSubjectsRequest
    {
        public int AcademicYearId { get; set; }
        public int ClassId        { get; set; }
        public List<ClassSubjectItem> Subjects { get; set; }
    }

    public class ClassSubjectItem
    {
        public int  SubjectId    { get; set; }
        public int? DisplayOrder { get; set; }
        public bool IsOptional   { get; set; }
    }
}
