using AscentSchools.Core.DTOs.School.Staff;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    public class StaffRepository
    {
        private readonly IConnectionFactory _db;
        public StaffRepository(IConnectionFactory db) { _db = db; }

        // ── Staff master ──────────────────────────────────────────────────────

        public IEnumerable<StaffDto> GetAll(string tenantDbName, int schoolId, string search, string status)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<StaffDto>(
                    @"SELECT staff_id      StaffId,
                             staff_name    StaffName,
                             ISNULL(employee_code, '')  EmployeeCode,
                             ISNULL(designation, '')    Designation,
                             ISNULL(department, '')     Department,
                             ISNULL(mobile, '')         Mobile,
                             ISNULL(email, '')          Email,
                             CONVERT(VARCHAR(10), join_date, 105) JoinDate,
                             status        Status
                      FROM staff
                      WHERE school_id = @schoolId
                        AND (@status IS NULL OR status = @status)
                        AND (@search IS NULL OR staff_name   LIKE '%' + @search + '%'
                                             OR employee_code LIKE '%' + @search + '%')
                      ORDER BY staff_name",
                    new { schoolId, status = string.IsNullOrWhiteSpace(status) ? null : status,
                          search = string.IsNullOrWhiteSpace(search) ? null : search });
        }

        public int Create(string tenantDbName, int schoolId, SaveStaffRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO staff (staff_name, employee_code, designation, department,
                                        mobile, email, join_date, school_id)
                      VALUES (@staffName, @employeeCode, @designation, @department,
                              @mobile, @email, @joinDate, @schoolId);
                      SELECT SCOPE_IDENTITY();",
                    new
                    {
                        req.StaffName, req.EmployeeCode, req.Designation, req.Department,
                        req.Mobile, req.Email,
                        joinDate  = string.IsNullOrWhiteSpace(req.JoinDate) ? (DateTime?)null
                                    : DateTime.Parse(req.JoinDate),
                        schoolId,
                    });
        }

        public void Update(string tenantDbName, int schoolId, int staffId, SaveStaffRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE staff
                      SET staff_name    = @staffName,
                          employee_code = @employeeCode,
                          designation   = @designation,
                          department    = @department,
                          mobile        = @mobile,
                          email         = @email,
                          join_date     = @joinDate,
                          updated_at    = GETDATE()
                      WHERE staff_id = @staffId AND school_id = @schoolId",
                    new
                    {
                        req.StaffName, req.EmployeeCode, req.Designation, req.Department,
                        req.Mobile, req.Email,
                        joinDate  = string.IsNullOrWhiteSpace(req.JoinDate) ? (DateTime?)null
                                    : DateTime.Parse(req.JoinDate),
                        staffId, schoolId,
                    });
        }

        public void SetStatus(string tenantDbName, int schoolId, int staffId, string status)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE staff SET status = @status, updated_at = GETDATE()
                      WHERE staff_id = @staffId AND school_id = @schoolId",
                    new { status, staffId, schoolId });
        }

        // ── Attendance grid ───────────────────────────────────────────────────

        public StaffAttendanceGridDto GetAttendanceGrid(
            string tenantDbName, int schoolId, string date)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var allStaff = conn.Query<StaffRow>(
                    @"SELECT staff_id StaffId, staff_name StaffName,
                             ISNULL(employee_code,'') EmployeeCode,
                             ISNULL(designation,'')   Designation
                      FROM staff
                      WHERE school_id = @schoolId AND status = 'Active'
                      ORDER BY staff_name",
                    new { schoolId }).ToList();

                var existing = conn.Query<AttendanceRow>(
                    @"SELECT staff_id StaffId, status Status, remarks Remarks
                      FROM staff_attendance
                      WHERE school_id = @schoolId AND attendance_date = @date",
                    new { schoolId, date })
                    .ToDictionary(r => r.StaffId);

                var rows = allStaff.Select(s =>
                {
                    existing.TryGetValue(s.StaffId, out var att);
                    return new StaffAttendanceRowDto
                    {
                        StaffId      = s.StaffId,
                        StaffName    = s.StaffName,
                        EmployeeCode = s.EmployeeCode,
                        Designation  = s.Designation,
                        Status       = att?.Status,
                        Remarks      = att?.Remarks,
                    };
                });

                return new StaffAttendanceGridDto
                {
                    Date     = date,
                    IsMarked = existing.Count > 0,
                    Staff    = rows,
                };
            }
        }

        // ── Save attendance (upsert) ──────────────────────────────────────────

        public void SaveAttendance(
            string tenantDbName, int schoolId, string markedBy, SaveStaffAttendanceRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                foreach (var entry in req.Entries)
                {
                    conn.Execute(
                        @"MERGE staff_attendance AS target
                          USING (VALUES (@staffId, @date, @schoolId))
                                AS source (staff_id, attendance_date, school_id)
                          ON  target.staff_id        = source.staff_id
                          AND target.attendance_date = source.attendance_date
                          AND target.school_id       = source.school_id
                          WHEN MATCHED THEN
                              UPDATE SET status = @status, remarks = @remarks,
                                         marked_by = @markedBy, marked_at = GETDATE()
                          WHEN NOT MATCHED THEN
                              INSERT (staff_id, attendance_date, status, remarks,
                                      school_id, marked_by)
                              VALUES (@staffId, @date, @status, @remarks,
                                      @schoolId, @markedBy);",
                        new
                        {
                            entry.StaffId,
                            date      = req.Date,
                            schoolId,
                            status    = entry.Status ?? "Present",
                            remarks   = entry.Remarks,
                            markedBy,
                        });
                }
            }
        }

        // ── Monthly summary ───────────────────────────────────────────────────

        public IEnumerable<StaffAttendanceSummaryDto> GetMonthlySummary(
            string tenantDbName, int schoolId, int month, int year)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<StaffAttendanceSummaryDto>(
                    @"SELECT s.staff_id      StaffId,
                             ISNULL(s.employee_code,'') EmployeeCode,
                             s.staff_name   StaffName,
                             ISNULL(s.designation,'')   Designation,
                             SUM(CASE WHEN a.status = 'Present'  THEN 1 ELSE 0 END) Present,
                             SUM(CASE WHEN a.status = 'Absent'   THEN 1 ELSE 0 END) Absent,
                             SUM(CASE WHEN a.status = 'Late'     THEN 1 ELSE 0 END) Late,
                             SUM(CASE WHEN a.status = 'HalfDay'  THEN 1 ELSE 0 END) HalfDay,
                             SUM(CASE WHEN a.status = 'OnLeave'  THEN 1 ELSE 0 END) OnLeave,
                             COUNT(a.attendance_id) TotalMarked
                      FROM staff s
                      LEFT JOIN staff_attendance a
                             ON a.staff_id  = s.staff_id
                            AND a.school_id = s.school_id
                            AND MONTH(a.attendance_date) = @month
                            AND YEAR(a.attendance_date)  = @year
                      WHERE s.school_id = @schoolId
                        AND s.status    = 'Active'
                      GROUP BY s.staff_id, s.employee_code, s.staff_name, s.designation
                      ORDER BY s.staff_name",
                    new { schoolId, month, year });
        }

        // ── Staff advances ────────────────────────────────────────────────────

        public IEnumerable<StaffAdvanceDto> GetAdvances(
            string tenantDbName, int schoolId,
            int? staffId, DateTime? dateFrom, DateTime? dateTo, string status)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<StaffAdvanceDto>(
                    @"SELECT a.advance_id   AdvanceId,
                             a.staff_id     StaffId,
                             s.staff_name   StaffName,
                             ISNULL(s.employee_code,'') EmployeeCode,
                             ISNULL(s.designation,'')   Designation,
                             CONVERT(VARCHAR(10), a.advance_date, 105) AdvanceDate,
                             a.amount       Amount,
                             ISNULL(a.purpose,'')  Purpose,
                             ISNULL(a.remarks,'')  Remarks,
                             ISNULL(r.TotalRepaid, 0)              TotalRepaid,
                             a.amount - ISNULL(r.TotalRepaid, 0)   Outstanding,
                             a.status       Status,
                             a.created_by   CreatedBy
                      FROM staff_advances a
                      JOIN staff s ON s.staff_id = a.staff_id
                      LEFT JOIN (
                          SELECT advance_id, SUM(amount) TotalRepaid
                          FROM staff_advance_repayments
                          WHERE school_id = @schoolId
                          GROUP BY advance_id
                      ) r ON r.advance_id = a.advance_id
                      WHERE a.school_id = @schoolId
                        AND (@staffId  IS NULL OR a.staff_id    = @staffId)
                        AND (@status   IS NULL OR a.status      = @status)
                        AND (@dateFrom IS NULL OR a.advance_date >= @dateFrom)
                        AND (@dateTo   IS NULL OR a.advance_date <= @dateTo)
                      ORDER BY a.advance_date DESC, s.staff_name",
                    new
                    {
                        schoolId,
                        staffId,
                        status    = string.IsNullOrWhiteSpace(status)  ? null : status,
                        dateFrom  = (object)dateFrom ?? DBNull.Value,
                        dateTo    = (object)dateTo   ?? DBNull.Value,
                    });
        }

        public int CreateAdvance(string tenantDbName, int schoolId, string createdBy, CreateAdvanceRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO staff_advances (staff_id, advance_date, amount, purpose, remarks, school_id, created_by)
                      VALUES (@staffId, @advanceDate, @amount, @purpose, @remarks, @schoolId, @createdBy);
                      SELECT SCOPE_IDENTITY();",
                    new
                    {
                        req.StaffId,
                        advanceDate = DateTime.Parse(req.AdvanceDate),
                        req.Amount,
                        purpose  = string.IsNullOrWhiteSpace(req.Purpose)  ? null : req.Purpose,
                        remarks  = string.IsNullOrWhiteSpace(req.Remarks)  ? null : req.Remarks,
                        schoolId,
                        createdBy,
                    });
        }

        public void CancelAdvance(string tenantDbName, int schoolId, int advanceId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE staff_advances SET status = 'Cancelled'
                      WHERE advance_id = @advanceId AND school_id = @schoolId",
                    new { advanceId, schoolId });
        }

        public IEnumerable<StaffAdvanceRepaymentDto> GetRepayments(
            string tenantDbName, int schoolId, int advanceId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<StaffAdvanceRepaymentDto>(
                    @"SELECT repayment_id   RepaymentId,
                             advance_id     AdvanceId,
                             CONVERT(VARCHAR(10), repayment_date, 105) RepaymentDate,
                             amount         Amount,
                             ISNULL(remarks,'') Remarks,
                             created_by     CreatedBy
                      FROM staff_advance_repayments
                      WHERE advance_id = @advanceId AND school_id = @schoolId
                      ORDER BY repayment_date",
                    new { advanceId, schoolId });
        }

        // Returns error message or null on success
        public string AddRepayment(
            string tenantDbName, int schoolId, string createdBy,
            int advanceId, AddRepaymentRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var advance = conn.QueryFirstOrDefault<AdvanceRow>(
                    "SELECT amount Amount, status Status FROM staff_advances WHERE advance_id = @advanceId AND school_id = @schoolId",
                    new { advanceId, schoolId });

                if (advance == null)             return "Advance not found.";
                if (advance.Status != "Active")  return "Cannot add repayment to a cancelled advance.";

                var repaid = conn.ExecuteScalar<decimal>(
                    "SELECT ISNULL(SUM(amount),0) FROM staff_advance_repayments WHERE advance_id = @advanceId AND school_id = @schoolId",
                    new { advanceId, schoolId });

                decimal outstanding = advance.Amount - repaid;
                if (req.Amount > outstanding)
                    return $"Repayment amount ({req.Amount:N2}) exceeds outstanding balance ({outstanding:N2}).";

                var staffId = conn.ExecuteScalar<int>(
                    "SELECT staff_id FROM staff_advances WHERE advance_id = @advanceId",
                    new { advanceId });

                conn.Execute(
                    @"INSERT INTO staff_advance_repayments (advance_id, staff_id, repayment_date, amount, remarks, school_id, created_by)
                      VALUES (@advanceId, @staffId, @repaymentDate, @amount, @remarks, @schoolId, @createdBy);",
                    new
                    {
                        advanceId, staffId,
                        repaymentDate = DateTime.Parse(req.RepaymentDate),
                        req.Amount,
                        remarks   = string.IsNullOrWhiteSpace(req.Remarks) ? null : req.Remarks,
                        schoolId, createdBy,
                    });

                return null;
            }
        }

        public IEnumerable<StaffAdvanceSummaryDto> GetAdvanceSummary(
            string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<StaffAdvanceSummaryDto>(
                    @"SELECT s.staff_id      StaffId,
                             ISNULL(s.employee_code,'') EmployeeCode,
                             s.staff_name   StaffName,
                             ISNULL(s.designation,'')   Designation,
                             ISNULL(SUM(a.amount), 0)                                  TotalAdvanced,
                             ISNULL(SUM(ISNULL(r.TotalRepaid, 0)), 0)                  TotalRepaid,
                             ISNULL(SUM(a.amount - ISNULL(r.TotalRepaid, 0)), 0)       Outstanding
                      FROM staff s
                      LEFT JOIN staff_advances a ON a.staff_id = s.staff_id
                                                 AND a.school_id = s.school_id
                                                 AND a.status = 'Active'
                      LEFT JOIN (
                          SELECT advance_id, SUM(amount) TotalRepaid
                          FROM staff_advance_repayments
                          WHERE school_id = @schoolId
                          GROUP BY advance_id
                      ) r ON r.advance_id = a.advance_id
                      WHERE s.school_id = @schoolId AND s.status = 'Active'
                      GROUP BY s.staff_id, s.employee_code, s.staff_name, s.designation
                      HAVING ISNULL(SUM(a.amount - ISNULL(r.TotalRepaid,0)), 0) > 0
                      ORDER BY Outstanding DESC",
                    new { schoolId });
        }

        // ── Salary components ─────────────────────────────────────────────────

        public IEnumerable<SalaryComponentDto> GetSalaryComponents(
            string tenantDbName, int schoolId, int staffId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<SalaryComponentDto>(
                    @"SELECT component_id   ComponentId,
                             staff_id       StaffId,
                             component_name ComponentName,
                             component_type ComponentType,
                             amount         Amount,
                             display_order  DisplayOrder
                      FROM staff_salary_components
                      WHERE staff_id = @staffId
                        AND school_id = @schoolId
                        AND is_active = 1
                      ORDER BY component_type DESC,   -- 'Earning' before 'Deduction' alphabetically
                               display_order,
                               component_name",
                    new { staffId, schoolId });
        }

        public void SaveSalaryComponents(
            string tenantDbName, int schoolId, SaveSalaryComponentsRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                conn.Execute(
                    "DELETE FROM staff_salary_components WHERE staff_id = @staffId AND school_id = @schoolId",
                    new { req.StaffId, schoolId });

                if (req.Components != null && req.Components.Count > 0)
                {
                    foreach (var item in req.Components)
                    {
                        conn.Execute(
                            @"INSERT INTO staff_salary_components
                                (staff_id, component_name, component_type, amount, display_order, school_id)
                              VALUES
                                (@staffId, @componentName, @componentType, @amount, @displayOrder, @schoolId)",
                            new
                            {
                                staffId       = req.StaffId,
                                item.ComponentName,
                                item.ComponentType,
                                item.Amount,
                                item.DisplayOrder,
                                schoolId,
                            });
                    }
                }
            }
        }

        // ── Monthly salaries ──────────────────────────────────────────────────

        public IEnumerable<StaffSalaryDto> GetSalaries(
            string tenantDbName, int schoolId,
            int? month, int? year, int? staffId, string status)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<StaffSalaryDto>(
                    @"SELECT ss.salary_id       SalaryId,
                             ss.staff_id        StaffId,
                             s.staff_name       StaffName,
                             ISNULL(s.employee_code,'') EmployeeCode,
                             ISNULL(s.designation,'')   Designation,
                             ss.month           Month,
                             ss.year            Year,
                             ss.gross_earnings  GrossEarnings,
                             ss.total_deductions TotalDeductions,
                             ss.advance_deducted AdvanceDeducted,
                             ss.net_salary      NetSalary,
                             ss.status          Status,
                             ISNULL(ss.remarks,'') Remarks,
                             ss.processed_by    ProcessedBy,
                             CONVERT(VARCHAR(19), ss.processed_at, 120) ProcessedAt,
                             CONVERT(VARCHAR(10), ss.paid_date, 105)    PaidDate
                      FROM staff_salaries ss
                      JOIN staff s ON s.staff_id = ss.staff_id
                      WHERE ss.school_id = @schoolId
                        AND (@month   IS NULL OR ss.month   = @month)
                        AND (@year    IS NULL OR ss.year    = @year)
                        AND (@staffId IS NULL OR ss.staff_id = @staffId)
                        AND (@status  IS NULL OR ss.status  = @status)
                      ORDER BY ss.year DESC, ss.month DESC, s.staff_name",
                    new
                    {
                        schoolId,
                        month,
                        year,
                        staffId,
                        status = string.IsNullOrWhiteSpace(status) ? null : status,
                    });
        }

        public int ProcessSalaries(
            string tenantDbName, int schoolId, string processedBy, ProcessSalariesRequest req)
        {
            int created = 0;
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                // Get all active staff that have at least one active salary component
                var staffWithComponents = conn.Query<StaffComponentSummaryRow>(
                    @"SELECT sc.staff_id StaffId,
                             SUM(CASE WHEN sc.component_type = 'Earning'   THEN sc.amount ELSE 0 END) GrossEarnings,
                             SUM(CASE WHEN sc.component_type = 'Deduction' THEN sc.amount ELSE 0 END) TotalDeductions
                      FROM staff_salary_components sc
                      JOIN staff st ON st.staff_id = sc.staff_id AND st.school_id = sc.school_id
                      WHERE sc.school_id = @schoolId
                        AND sc.is_active = 1
                        AND st.status = 'Active'
                      GROUP BY sc.staff_id",
                    new { schoolId }).ToList();

                foreach (var row in staffWithComponents)
                {
                    // Skip if salary already exists for this month/year
                    var existing = conn.ExecuteScalar<int>(
                        @"SELECT COUNT(1) FROM staff_salaries
                          WHERE staff_id = @staffId AND month = @month AND year = @year AND school_id = @schoolId",
                        new { row.StaffId, req.Month, req.Year, schoolId });

                    if (existing > 0) continue;

                    var net = row.GrossEarnings - row.TotalDeductions;

                    var salaryId = conn.QuerySingle<int>(
                        @"INSERT INTO staff_salaries
                            (staff_id, month, year, gross_earnings, total_deductions,
                             advance_deducted, net_salary, school_id, processed_by)
                          VALUES
                            (@staffId, @month, @year, @grossEarnings, @totalDeductions,
                             0, @netSalary, @schoolId, @processedBy);
                          SELECT SCOPE_IDENTITY();",
                        new
                        {
                            staffId       = row.StaffId,
                            req.Month,
                            req.Year,
                            grossEarnings    = row.GrossEarnings,
                            totalDeductions  = row.TotalDeductions,
                            netSalary        = net,
                            schoolId,
                            processedBy,
                        });

                    // Insert salary item snapshots
                    var components = conn.Query<SalaryComponentDto>(
                        @"SELECT component_name ComponentName, component_type ComponentType, amount Amount
                          FROM staff_salary_components
                          WHERE staff_id = @staffId AND school_id = @schoolId AND is_active = 1",
                        new { staffId = row.StaffId, schoolId });

                    foreach (var comp in components)
                    {
                        conn.Execute(
                            @"INSERT INTO staff_salary_items (salary_id, component_name, component_type, amount)
                              VALUES (@salaryId, @componentName, @componentType, @amount)",
                            new { salaryId, comp.ComponentName, comp.ComponentType, comp.Amount });
                    }

                    created++;
                }
            }
            return created;
        }

        public void UpdateSalary(
            string tenantDbName, int schoolId, int salaryId, UpdateSalaryRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE staff_salaries
                      SET advance_deducted = @advanceDeducted,
                          remarks          = @remarks,
                          net_salary       = gross_earnings - total_deductions - @advanceDeducted
                      WHERE salary_id = @salaryId AND school_id = @schoolId",
                    new
                    {
                        advanceDeducted = req.AdvanceDeducted,
                        remarks         = string.IsNullOrWhiteSpace(req.Remarks) ? null : req.Remarks,
                        salaryId,
                        schoolId,
                    });
        }

        public string MarkSalaryPaid(
            string tenantDbName, int schoolId, int salaryId, DateTime paidDate)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var current = conn.ExecuteScalar<string>(
                    "SELECT status FROM staff_salaries WHERE salary_id = @salaryId AND school_id = @schoolId",
                    new { salaryId, schoolId });

                if (current == null) return "Salary record not found.";
                if (current != "Draft") return "Only Draft salaries can be marked as Paid.";

                conn.Execute(
                    "UPDATE staff_salaries SET status = 'Paid', paid_date = @paidDate WHERE salary_id = @salaryId AND school_id = @schoolId",
                    new { paidDate, salaryId, schoolId });
                return null;
            }
        }

        public string CancelSalary(string tenantDbName, int schoolId, int salaryId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var current = conn.ExecuteScalar<string>(
                    "SELECT status FROM staff_salaries WHERE salary_id = @salaryId AND school_id = @schoolId",
                    new { salaryId, schoolId });

                if (current == null) return "Salary record not found.";
                if (current == "Paid") return "Cannot cancel a Paid salary.";

                conn.Execute(
                    "UPDATE staff_salaries SET status = 'Cancelled' WHERE salary_id = @salaryId AND school_id = @schoolId",
                    new { salaryId, schoolId });
                return null;
            }
        }

        public StaffSalarySlipDto GetSalarySlip(string tenantDbName, int schoolId, int salaryId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var slip = conn.QueryFirstOrDefault<StaffSalarySlipDto>(
                    @"SELECT ss.salary_id       SalaryId,
                             ss.staff_id        StaffId,
                             s.staff_name       StaffName,
                             ISNULL(s.employee_code,'') EmployeeCode,
                             ISNULL(s.designation,'')   Designation,
                             ss.month           Month,
                             ss.year            Year,
                             ss.gross_earnings  GrossEarnings,
                             ss.total_deductions TotalDeductions,
                             ss.advance_deducted AdvanceDeducted,
                             ss.net_salary      NetSalary,
                             ss.status          Status,
                             ISNULL(ss.remarks,'') Remarks,
                             ss.processed_by    ProcessedBy,
                             CONVERT(VARCHAR(19), ss.processed_at, 120) ProcessedAt,
                             CONVERT(VARCHAR(10), ss.paid_date, 105)    PaidDate
                      FROM staff_salaries ss
                      JOIN staff s ON s.staff_id = ss.staff_id
                      WHERE ss.salary_id = @salaryId AND ss.school_id = @schoolId",
                    new { salaryId, schoolId });

                if (slip == null) return null;

                var items = conn.Query<SalaryItemDto>(
                    @"SELECT component_name ComponentName, component_type ComponentType, amount Amount
                      FROM staff_salary_items
                      WHERE salary_id = @salaryId
                      ORDER BY component_type DESC, component_name",
                    new { salaryId });

                slip.Items = items.ToList();
                return slip;
            }
        }

        private class StaffRow                { public int StaffId { get; set; } public string StaffName { get; set; } public string EmployeeCode { get; set; } public string Designation { get; set; } }
        private class AttendanceRow           { public int StaffId { get; set; } public string Status    { get; set; } public string Remarks      { get; set; } }
        private class AdvanceRow              { public decimal Amount { get; set; } public string Status { get; set; } }
        private class StaffComponentSummaryRow { public int StaffId { get; set; } public decimal GrossEarnings { get; set; } public decimal TotalDeductions { get; set; } }
    }
}
