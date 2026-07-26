using AscentSchools.Core.DTOs.School.ClassTeachers;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.School
{
    /// <summary>
    /// class_teacher_assignments — maps a class (+ optional section) to its teacher(s).
    /// A NULL section_id assignment covers every section of the class.
    /// </summary>
    public class ClassTeacherRepository
    {
        private readonly IConnectionFactory _db;
        public ClassTeacherRepository(IConnectionFactory db) { _db = db; }

        // ── Management (school web app) ────────────────────────────────────

        public IEnumerable<ClassTeacherDto> GetAssignments(string tenantDbName, int schoolId,
            int? academicYearId, int? classId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<ClassTeacherDto>(
                    @"SELECT a.assignment_id AssignmentId, a.academic_year_id AcademicYearId,
                             a.class_id ClassId, c.class_name ClassName,
                             a.section_id SectionId, s.section_name SectionName,
                             a.user_id UserId, u.full_name FullName, u.username Username,
                             st.designation Designation,
                             a.created_by CreatedBy, a.created_at CreatedAt, a.school_id SchoolId
                      FROM class_teacher_assignments a
                      JOIN classes c        ON c.class_id   = a.class_id
                      LEFT JOIN sections s  ON s.section_id = a.section_id
                      JOIN users u          ON u.user_id    = a.user_id
                      LEFT JOIN staff st    ON st.staff_id  = u.employee_id
                      WHERE a.school_id = @schoolId
                        AND (@academicYearId IS NULL OR a.academic_year_id = @academicYearId)
                        AND (@classId IS NULL OR a.class_id = @classId)
                      ORDER BY c.class_name, ISNULL(s.sequence_no, 9999), s.section_name, u.full_name",
                    new { schoolId, academicYearId, classId });
        }

        /// <summary>Active logins of the branch, for the assign-teacher dropdown.</summary>
        public IEnumerable<AssignableTeacherDto> GetAssignableTeachers(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<AssignableTeacherDto>(
                    @"SELECT DISTINCT u.user_id UserId, u.full_name FullName, u.username Username,
                             st.designation Designation
                      FROM users u
                      LEFT JOIN staff st ON st.staff_id = u.employee_id
                      LEFT JOIN user_roles ur ON ur.user_id = u.user_id
                      WHERE u.status = 'Active'
                        AND (u.school_id = @schoolId OR u.school_id IS NULL OR ur.school_id = @schoolId)
                      ORDER BY u.full_name",
                    new { schoolId });
        }

        /// <summary>True when this exact teacher is already assigned to this class+section.</summary>
        public bool AssignmentExists(string tenantDbName, int schoolId, SaveClassTeacherRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.ExecuteScalar<int>(
                    @"SELECT COUNT(1) FROM class_teacher_assignments
                      WHERE school_id = @schoolId AND academic_year_id = @AcademicYearId
                        AND class_id = @ClassId AND user_id = @UserId
                        AND ISNULL(section_id, 0) = ISNULL(@SectionId, 0)",
                    new { schoolId, r.AcademicYearId, r.ClassId, r.UserId, r.SectionId }) > 0;
        }

        public int CreateAssignment(string tenantDbName, int schoolId, string createdBy, SaveClassTeacherRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO class_teacher_assignments
                        (academic_year_id, class_id, section_id, user_id, school_id, created_by)
                      VALUES (@AcademicYearId, @ClassId, @SectionId, @UserId, @schoolId, @createdBy);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { r.AcademicYearId, r.ClassId, r.SectionId, r.UserId, schoolId, createdBy });
        }

        public int DeleteAssignment(string tenantDbName, int schoolId, int assignmentId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Execute(
                    @"DELETE FROM class_teacher_assignments
                      WHERE assignment_id = @assignmentId AND school_id = @schoolId",
                    new { assignmentId, schoolId });
        }

        // ── Routing (messaging) ───────────────────────────────────────────

        /// <summary>
        /// Teachers who should receive a parent's message about a child in this
        /// class+section. Matches section-specific assignments AND class-wide
        /// (section_id IS NULL) ones — same convention as homework/announcements.
        /// Empty result = messaging is not set up for that class yet.
        /// </summary>
        public IEnumerable<AssignableTeacherDto> GetTeachersForClass(string tenantDbName, int schoolId,
            int academicYearId, int classId, int? sectionId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<AssignableTeacherDto>(
                    @"SELECT DISTINCT u.user_id UserId, u.full_name FullName, u.username Username,
                             st.designation Designation
                      FROM class_teacher_assignments a
                      JOIN users u       ON u.user_id   = a.user_id
                      LEFT JOIN staff st ON st.staff_id = u.employee_id
                      WHERE a.school_id = @schoolId
                        AND a.academic_year_id = @academicYearId
                        AND a.class_id = @classId
                        AND (a.section_id IS NULL OR a.section_id = @sectionId)
                        AND u.status = 'Active'
                      ORDER BY u.full_name",
                    new { schoolId, academicYearId, classId, sectionId });
        }

        /// <summary>
        /// The class+section pairs a teacher is assigned to (drives their thread list).
        /// A class-wide assignment yields one row with SectionId = NULL.
        /// </summary>
        public IEnumerable<ClassTeacherDto> GetAssignmentsForTeacher(string tenantDbName, int schoolId,
            int academicYearId, int userId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<ClassTeacherDto>(
                    @"SELECT a.assignment_id AssignmentId, a.academic_year_id AcademicYearId,
                             a.class_id ClassId, c.class_name ClassName,
                             a.section_id SectionId, s.section_name SectionName,
                             a.user_id UserId, a.school_id SchoolId
                      FROM class_teacher_assignments a
                      JOIN classes c       ON c.class_id   = a.class_id
                      LEFT JOIN sections s ON s.section_id = a.section_id
                      WHERE a.school_id = @schoolId
                        AND a.academic_year_id = @academicYearId
                        AND a.user_id = @userId
                      ORDER BY c.class_name, s.section_name",
                    new { schoolId, academicYearId, userId });
        }
    }
}
