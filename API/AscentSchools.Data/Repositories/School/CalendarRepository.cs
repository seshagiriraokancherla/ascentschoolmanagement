using AscentSchools.Core.DTOs.School.Calendar;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.School
{
    public class CalendarRepository
    {
        private readonly IConnectionFactory _db;
        public CalendarRepository(IConnectionFactory db) { _db = db; }

        // Events overlapping a given month (month/year both > 0), else all active for the school.
        // Overlap = start_date <= last-day-of-month AND end_date >= first-day-of-month.
        public IEnumerable<CalendarEventDto> GetEvents(
            string tenantDbName, int schoolId, int month, int year, int? academicYearId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<CalendarEventDto>(
                    @"SELECT calendar_event_id CalendarEventId, title Title, description Description,
                             category Category, start_date StartDate, end_date EndDate,
                             academic_year_id AcademicYearId, status Status,
                             created_by CreatedBy, created_at CreatedAt
                      FROM calendar_events
                      WHERE school_id = @schoolId
                        AND status    = 'Active'
                        AND (@academicYearId IS NULL OR academic_year_id = @academicYearId)
                        AND (@month = 0 OR @year = 0 OR
                             (start_date <= EOMONTH(DATEFROMPARTS(@year, @month, 1))
                              AND end_date >= DATEFROMPARTS(@year, @month, 1)))
                      ORDER BY start_date, calendar_event_id",
                    new { schoolId, academicYearId, month, year });
        }

        public int CreateEvent(string tenantDbName, int schoolId, string createdBy, SaveCalendarEventRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    // created_at is stamped by the column's IST default (server runs US Eastern).
                    @"INSERT INTO calendar_events
                        (title, description, category, start_date, end_date, academic_year_id, school_id, created_by)
                      VALUES
                        (@Title, @Description, @Category, @StartDate, @EndDate, @AcademicYearId, @schoolId, @createdBy);
                      SELECT SCOPE_IDENTITY();",
                    new { req.Title, req.Description, req.Category, req.StartDate, req.EndDate, req.AcademicYearId, schoolId, createdBy });
        }

        public void UpdateEvent(string tenantDbName, int schoolId, int id, string updatedBy, SaveCalendarEventRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE calendar_events
                      SET title = @Title, description = @Description, category = @Category,
                          start_date = @StartDate, end_date = @EndDate, academic_year_id = @AcademicYearId,
                          updated_by = @updatedBy, updated_at = GETDATE()
                      WHERE calendar_event_id = @id AND school_id = @schoolId",
                    new { req.Title, req.Description, req.Category, req.StartDate, req.EndDate, req.AcademicYearId, updatedBy, id, schoolId });
        }

        public void DeleteEvent(string tenantDbName, int schoolId, int id, string updatedBy)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE calendar_events SET status = 'Inactive', updated_by = @updatedBy, updated_at = GETDATE()
                      WHERE calendar_event_id = @id AND school_id = @schoolId",
                    new { updatedBy, id, schoolId });
        }
    }
}
