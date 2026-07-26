using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Reports
{
    public class DetainedStudentRowDto
    {
        public long   StudentId      { get; set; }
        public string AdmissionNo    { get; set; }
        public string StudentName    { get; set; }
        public string ClassName      { get; set; }
        public string SectionName    { get; set; }
        public string AcademicYear   { get; set; }
        public string FatherName     { get; set; }
        public string FatherMobile   { get; set; }
        public string DetainedReason { get; set; }
    }

    public class RegularAbsenteeRowDto
    {
        public long   StudentId      { get; set; }
        public string AdmissionNo    { get; set; }
        public string StudentName    { get; set; }
        public string ClassName      { get; set; }
        public string SectionName    { get; set; }
        public string FatherName     { get; set; }
        public string FatherMobile   { get; set; }
        public int    AbsentDays     { get; set; }
        public string LastAbsentDate { get; set; }
        public string Status         { get; set; }
    }

    public class StrengthRowDto
    {
        public string ClassName   { get; set; }
        public string SectionName { get; set; }
        public int    Boys        { get; set; }
        public int    Girls       { get; set; }
        public int    Others      { get; set; }
        public int    Total       { get; set; }
    }

    public class AbsentRowDto
    {
        public string AttendanceDate { get; set; }
        public string AdmissionNo    { get; set; }
        public string StudentName    { get; set; }
        public string ClassName      { get; set; }
        public string SectionName    { get; set; }
        public string FatherName     { get; set; }
        public string FatherMobile   { get; set; }
    }

    public class TransportStudentRowDto
    {
        public string AdmissionNo   { get; set; }
        public string StudentName   { get; set; }
        public string ClassName     { get; set; }
        public string SectionName   { get; set; }
        public string RouteName     { get; set; }
        public string BusName       { get; set; }
        public string BusTrip       { get; set; }
        public string FatherName    { get; set; }
        public string FatherMobile  { get; set; }
    }

    public class AttendanceSheetDto
    {
        public int    Month       { get; set; }
        public int    Year        { get; set; }
        public int    DaysInMonth { get; set; }
        public string ClassName   { get; set; }
        public string SectionName { get; set; }
        public IEnumerable<AttendanceSheetRowDto> Rows { get; set; }
    }

    public class AttendanceSheetRowDto
    {
        public long   StudentId   { get; set; }
        public string AdmissionNo { get; set; }
        public string StudentName { get; set; }
        public int    Present     { get; set; }
        public int    Absent      { get; set; }
        public int    Late        { get; set; }
        public int    HalfDay     { get; set; }
        // Key = day-of-month (1–31), Value = "P" | "A" | "L" | "H" | ""
        public System.Collections.Generic.Dictionary<int, string> Days { get; set; }
    }

    public class DailyAttendanceSummaryRowDto
    {
        public string AttendanceDate { get; set; }
        public string ClassName      { get; set; }
        public string SectionName    { get; set; }
        public int    TotalStudents  { get; set; }
        public int    Present        { get; set; }
        public int    Absent         { get; set; }
        public int    Late           { get; set; }
        public int    HalfDay        { get; set; }
    }

    public class MonthlyAttendanceSheetDto
    {
        public string ClassName   { get; set; }
        public string SectionName { get; set; }
        public string AcademicYear { get; set; }
        // Ordered month keys "YYYY-MM" present in the data
        public System.Collections.Generic.List<string> Months { get; set; }
        public IEnumerable<MonthlyAttendanceRowDto> Rows { get; set; }
    }

    public class MonthlyAttendanceRowDto
    {
        public string AdmissionNo  { get; set; }
        public string StudentName  { get; set; }
        public int    TotalPresent { get; set; }
        public int    TotalAbsent  { get; set; }
        public int    TotalLate    { get; set; }
        public int    TotalHalfDay { get; set; }
        // Key = "YYYY-MM"
        public System.Collections.Generic.Dictionary<string, MonthCounts> MonthData { get; set; }
    }

    public class MonthCounts
    {
        public int Present { get; set; }
        public int Absent  { get; set; }
        public int Late    { get; set; }
        public int HalfDay { get; set; }
    }

    public class FailedStudentsDto
    {
        public string ExamName     { get; set; }
        public string AcademicYear { get; set; }
        public int    PassMarkPct  { get; set; }
        public System.Collections.Generic.List<string> Subjects { get; set; }
        public IEnumerable<FailedStudentRowDto> Rows { get; set; }
    }

    public class FailedStudentRowDto
    {
        public string  AdmissionNo   { get; set; }
        public string  StudentName   { get; set; }
        public string  ClassName     { get; set; }
        public string  SectionName   { get; set; }
        public int     FailedCount   { get; set; }
        public decimal TotalObtained { get; set; }
        public decimal TotalMax      { get; set; }
        public decimal Percentage    { get; set; }
        // Key = subject name, Value = "28/100" | "AB" | "—"
        public System.Collections.Generic.Dictionary<string, string> SubjectMarks { get; set; }
        // Subject names where student failed (for PDF summary column)
        public System.Collections.Generic.List<string> FailedSubjects { get; set; }
    }

    public class ClassToppersDto
    {
        public string ClassName    { get; set; }
        public string SectionName  { get; set; }
        public string AcademicYear { get; set; }
        public System.Collections.Generic.List<string> Subjects { get; set; }
        public IEnumerable<ClassTopperExamDto> Exams { get; set; }
    }

    public class ClassTopperExamDto
    {
        public string ExamName                    { get; set; }
        public IEnumerable<ExamTopperRowDto> Rows { get; set; }
    }

    public class ExamToppersDto
    {
        public string ExamName     { get; set; }
        public string AcademicYear { get; set; }
        public System.Collections.Generic.List<string> Subjects { get; set; }
        public IEnumerable<ExamTopperClassDto> Classes { get; set; }
    }

    public class ExamTopperClassDto
    {
        public string ClassName   { get; set; }
        public string SectionName { get; set; }
        public IEnumerable<ExamTopperRowDto> Rows { get; set; }
    }

    public class ExamTopperRowDto
    {
        public int     Rank          { get; set; }
        public string  AdmissionNo   { get; set; }
        public string  StudentName   { get; set; }
        public decimal TotalObtained { get; set; }
        public decimal TotalMax      { get; set; }
        public decimal Percentage    { get; set; }
        // Key = subject name, Value = "45/100" | "AB" | "—"
        public System.Collections.Generic.Dictionary<string, string> SubjectMarks { get; set; }
    }

    public class HomeworkStatementRowDto
    {
        public string AssignedDate { get; set; }
        public string ClassName    { get; set; }
        public string SubjectName  { get; set; }
        public string Title        { get; set; }
        public string Description  { get; set; }
        public string AssignedBy   { get; set; }
    }

    public class AcademicYearToppersDto
    {
        public string AcademicYear { get; set; }
        // Ordered exam names used as column headers
        public System.Collections.Generic.List<string> ExamNames { get; set; }
        public IEnumerable<AcademicYearTopperClassDto> Classes { get; set; }
    }

    public class AcademicYearTopperClassDto
    {
        public string ClassName   { get; set; }
        public string SectionName { get; set; }
        public IEnumerable<AcademicYearTopperRowDto> Rows { get; set; }
    }

    public class AcademicYearTopperRowDto
    {
        public int     Rank          { get; set; }
        public string  AdmissionNo   { get; set; }
        public string  StudentName   { get; set; }
        public decimal TotalObtained { get; set; }
        public decimal TotalMax      { get; set; }
        public decimal Percentage    { get; set; }
        // Key = exam name, Value = "obtained/max" or "—"
        public System.Collections.Generic.Dictionary<string, string> ExamTotals { get; set; }
    }

    public class AttendanceRegisterDto
    {
        public string ClassName   { get; set; }
        public string SectionName { get; set; }
        // Ordered working-day dates that have attendance records, format "YYYY-MM-DD"
        public System.Collections.Generic.List<string> Dates { get; set; }
        public IEnumerable<AttendanceRegisterRowDto> Rows { get; set; }
    }

    public class AttendanceRegisterRowDto
    {
        public int    SerialNo    { get; set; }
        public string AdmissionNo { get; set; }
        public string StudentName { get; set; }
        public int    Present     { get; set; }
        public int    Absent      { get; set; }
        public int    Late        { get; set; }
        public int    HalfDay     { get; set; }
        // Key = "YYYY-MM-DD", Value = "P" | "A" | "L" | "H"
        public System.Collections.Generic.Dictionary<string, string> DateData { get; set; }
    }
}
