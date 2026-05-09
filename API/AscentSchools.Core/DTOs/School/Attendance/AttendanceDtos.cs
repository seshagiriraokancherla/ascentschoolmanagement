using System;
using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Attendance
{
    public class AttendanceStudentDto
    {
        public long   StudentId   { get; set; }
        public string StudentName { get; set; }
        public string AdmissionNo { get; set; }
        public string Status      { get; set; }  // Present / Absent / Late / Holiday — null if not yet marked
        public string Remarks     { get; set; }
    }

    public class AttendanceGridDto
    {
        public int    ClassId    { get; set; }
        public string ClassName  { get; set; }
        public string Date       { get; set; }  // yyyy-MM-dd
        public bool   IsMarked   { get; set; }  // true if at least one record exists for this date
        public IEnumerable<AttendanceStudentDto> Students { get; set; }
    }

    public class SaveAttendanceRequest
    {
        public int    ClassId { get; set; }
        public string Date    { get; set; }  // yyyy-MM-dd
        public IEnumerable<AttendanceEntryDto> Entries { get; set; }
    }

    public class AttendanceEntryDto
    {
        public long   StudentId { get; set; }
        public string Status    { get; set; }
        public string Remarks   { get; set; }
    }

    // Monthly summary — used by reports / dashboard
    public class AttendanceMonthlySummaryDto
    {
        public string StudentName  { get; set; }
        public string AdmissionNo  { get; set; }
        public int    PresentDays  { get; set; }
        public int    AbsentDays   { get; set; }
        public int    LateDays     { get; set; }
        public int    TotalMarked  { get; set; }
    }
}
