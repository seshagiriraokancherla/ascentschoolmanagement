using System;
using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Dashboard
{
    public class DashboardDto
    {
        // ── Students ──────────────────────────────────────────────────────────
        public int TotalActiveStudents { get; set; }

        // ── Attendance (today) ────────────────────────────────────────────────
        public bool    AttendanceMarkedToday { get; set; }
        public int     TodayPresent          { get; set; }
        public int     TodayAbsent           { get; set; }
        public int     TodayLate             { get; set; }
        public int     TodayHalfDay          { get; set; }
        public int     TodayTotalMarked      { get; set; }
        public decimal AttendancePct         { get; set; }  // (Present+Late+0.5×HalfDay) / TotalMarked × 100

        // ── Fee collection ────────────────────────────────────────────────────
        public decimal TodayCollection    { get; set; }
        public decimal MonthCollection    { get; set; }
        public int     MonthReceiptCount  { get; set; }

        // ── Last 6 months fee trend ───────────────────────────────────────────
        public IEnumerable<MonthlyFeeDto> Last6MonthsCollection { get; set; }

        // ── Recent receipts ───────────────────────────────────────────────────
        public IEnumerable<RecentReceiptDto> RecentReceipts { get; set; }

        // ── Recent homework ───────────────────────────────────────────────────
        public IEnumerable<RecentHomeworkDto> RecentHomework { get; set; }

        // ── Announcements ─────────────────────────────────────────────────────
        public int ActiveAnnouncementsCount { get; set; }
    }

    public class MonthlyFeeDto
    {
        public string  MonthLabel { get; set; }   // "Jan 2025"
        public int     Month      { get; set; }
        public int     Year       { get; set; }
        public decimal Amount     { get; set; }
    }

    public class RecentReceiptDto
    {
        public string   ReceiptNo    { get; set; }
        public string   StudentName  { get; set; }
        public decimal  TotalAmount  { get; set; }
        public DateTime PaymentDate  { get; set; }
        public string   Status       { get; set; }
        public string   PaymentMode  { get; set; }   // Cash / Cheque / Online etc.
    }

    public class RecentHomeworkDto
    {
        public string   Title        { get; set; }
        public string   SubjectName  { get; set; }
        public string   ClassName    { get; set; }
        public DateTime AssignedDate { get; set; }
    }
}
