using AscentSchools.Core.DTOs.School.Dashboard;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    public class DashboardRepository
    {
        private readonly IConnectionFactory _db;
        public DashboardRepository(IConnectionFactory db) { _db = db; }

        public DashboardDto GetDashboard(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                // ── 1. Total active students — current academic year only ─────
                var totalStudents = conn.QuerySingle<int>(
                    @"SELECT COUNT(*) FROM students
                      WHERE school_id = @schoolId AND status = 'Active'
                        AND academic_year_id = (
                            SELECT TOP 1 academic_year_id
                            FROM academic_years
                            WHERE school_id = @schoolId AND status = 'Active'
                            ORDER BY academic_year_id DESC)",
                    new { schoolId });

                // ── 2. Today's attendance ─────────────────────────────────────
                // Scope to current-year students so stray marks on prior-year student rows
                // (e.g. from the old all-years attendance grid) don't inflate the counts.
                var todayAtt = conn.QueryFirstOrDefault<AttendanceRow>(
                    @"SELECT
                          SUM(CASE WHEN sa.status = 'Present'  THEN 1 ELSE 0 END) TodayPresent,
                          SUM(CASE WHEN sa.status = 'Absent'   THEN 1 ELSE 0 END) TodayAbsent,
                          SUM(CASE WHEN sa.status = 'Late'     THEN 1 ELSE 0 END) TodayLate,
                          SUM(CASE WHEN sa.status = 'HalfDay'  THEN 1 ELSE 0 END) TodayHalfDay,
                          COUNT(*) TotalMarked
                      FROM student_attendance sa
                      JOIN students s ON s.student_id = sa.student_id AND s.school_id = sa.school_id
                      WHERE sa.school_id = @schoolId
                        AND sa.attendance_date = CAST(GETDATE() AS DATE)
                        AND s.academic_year_id = (
                            SELECT TOP 1 academic_year_id FROM academic_years
                            WHERE school_id = @schoolId AND status = 'Active'
                            ORDER BY academic_year_id DESC)",
                    new { schoolId }) ?? new AttendanceRow();

                var attMarked = todayAtt.TotalMarked > 0;
                var attPct    = attMarked
                    ? Math.Round((decimal)(todayAtt.TodayPresent + todayAtt.TodayLate + 0.5m * todayAtt.TodayHalfDay) / todayAtt.TotalMarked * 100, 1)
                    : 0m;

                // ── 3. Fee collection — today + this month ────────────────────
                var feeToday = conn.QuerySingle<decimal>(
                    @"SELECT ISNULL(SUM(total_amount), 0)
                      FROM fee_receipts
                      WHERE school_id = @schoolId AND status = 'Active'
                        AND payment_date = CAST(GETDATE() AS DATE)",
                    new { schoolId });

                var feeMonth = conn.QueryFirstOrDefault<FeeMonthRow>(
                    @"SELECT ISNULL(SUM(total_amount), 0) Amount, COUNT(*) ReceiptCount
                      FROM fee_receipts
                      WHERE school_id = @schoolId AND status = 'Active'
                        AND MONTH(payment_date) = MONTH(GETDATE())
                        AND YEAR(payment_date)  = YEAR(GETDATE())",
                    new { schoolId }) ?? new FeeMonthRow();

                // ── 4. Last 6 months fee trend ────────────────────────────────
                var last6 = conn.Query<MonthlyFeeDto>(
                    @"SELECT FORMAT(payment_date, 'MMM yyyy') MonthLabel,
                             MONTH(payment_date) Month,
                             YEAR(payment_date)  Year,
                             ISNULL(SUM(total_amount), 0) Amount
                      FROM fee_receipts
                      WHERE school_id = @schoolId AND status = 'Active'
                        AND payment_date >= DATEADD(MONTH, -5,
                              DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
                      GROUP BY YEAR(payment_date), MONTH(payment_date),
                               FORMAT(payment_date, 'MMM yyyy')
                      ORDER BY YEAR(payment_date), MONTH(payment_date)",
                    new { schoolId });

                // ── 5. Recent 5 receipts ──────────────────────────────────────
                var recentReceipts = conn.Query<RecentReceiptDto>(
                    @"SELECT TOP 5
                          fr.receipt_no ReceiptNo,
                          s.student_name StudentName,
                          fr.total_amount TotalAmount,
                          fr.payment_date PaymentDate,
                          fr.status Status
                      FROM fee_receipts fr
                      JOIN students s ON s.student_id = fr.student_id
                      WHERE fr.school_id = @schoolId
                      ORDER BY fr.created_at DESC",
                    new { schoolId });

                // ── 6. Upcoming homework (next 7 days) ────────────────────────
                var upcomingHw = conn.Query<UpcomingHomeworkDto>(
                    @"SELECT TOP 7
                          h.title Title,
                          sub.subject_name SubjectName,
                          c.class_name ClassName,
                          h.due_date DueDate
                      FROM homework h
                      LEFT JOIN subjects sub ON sub.subject_id = h.subject_id
                      LEFT JOIN classes  c   ON c.class_id     = h.class_id
                      WHERE h.school_id = @schoolId AND h.status = 'Active'
                        AND h.due_date >= CAST(GETDATE() AS DATE)
                      ORDER BY h.due_date",
                    new { schoolId });

                // ── 7. Active announcements count ─────────────────────────────
                var announcementsCount = conn.QuerySingle<int>(
                    "SELECT COUNT(*) FROM announcements WHERE school_id = @schoolId AND status = 'Active'",
                    new { schoolId });

                return new DashboardDto
                {
                    TotalActiveStudents      = totalStudents,
                    AttendanceMarkedToday    = attMarked,
                    TodayPresent             = todayAtt.TodayPresent,
                    TodayAbsent              = todayAtt.TodayAbsent,
                    TodayLate                = todayAtt.TodayLate,
                    TodayHalfDay             = todayAtt.TodayHalfDay,
                    TodayTotalMarked         = todayAtt.TotalMarked,
                    AttendancePct            = attPct,
                    TodayCollection          = feeToday,
                    MonthCollection          = feeMonth.Amount,
                    MonthReceiptCount        = feeMonth.ReceiptCount,
                    Last6MonthsCollection    = last6,
                    RecentReceipts           = recentReceipts,
                    UpcomingHomework         = upcomingHw,
                    ActiveAnnouncementsCount = announcementsCount,
                };
            }
        }

        private class AttendanceRow
        {
            public int TodayPresent { get; set; }
            public int TodayAbsent  { get; set; }
            public int TodayLate    { get; set; }
            public int TodayHalfDay { get; set; }
            public int TotalMarked  { get; set; }
        }

        private class FeeMonthRow
        {
            public decimal Amount       { get; set; }
            public int     ReceiptCount { get; set; }
        }
    }
}
