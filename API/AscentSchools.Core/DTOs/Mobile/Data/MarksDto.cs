using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.Mobile.Data
{
    public class MarksResultDto
    {
        public int    AcademicYearId   { get; set; }
        public string AcademicYearName { get; set; }
        public IEnumerable<ExamResultDto> Exams { get; set; }
    }

    public class ExamResultDto
    {
        public int    ExamTypeId   { get; set; }
        public string ExamTypeName { get; set; }
        public IEnumerable<SubjectMarkDto> Marks { get; set; }
        public decimal TotalObtained { get; set; }
        public decimal TotalMax      { get; set; }
        public decimal Percentage    { get; set; }
    }

    public class SubjectMarkDto
    {
        public string  SubjectName    { get; set; }
        public decimal MarksObtained  { get; set; }
        public decimal MaxMarks       { get; set; }
        public bool    IsAbsent       { get; set; }
    }
}
