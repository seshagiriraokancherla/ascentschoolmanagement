using System;
using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.Mobile.Data
{
    public class AttendanceSummaryDto
    {
        public int   TotalDays   { get; set; }
        public int   PresentDays { get; set; }
        public int   AbsentDays  { get; set; }
        public int   LateDays    { get; set; }
        public IEnumerable<AttendanceRecordDto> Records { get; set; }
    }

    public class AttendanceRecordDto
    {
        public DateTime Date    { get; set; }
        public string   Status  { get; set; }   // Present / Absent / Late / Holiday
        public string   Remarks { get; set; }
    }
}
