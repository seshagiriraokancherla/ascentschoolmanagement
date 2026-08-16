using AscentSchools.Core.DTOs.School.ClassSubjects;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;
using System.Data;

namespace AscentSchools.Data.Repositories.School
{
    /// <summary>
    /// class_subjects — which subjects a class studies in a given academic year.
    /// Drives the exam / marks feature so a class only shows its own subjects.
    /// </summary>
    public class ClassSubjectRepository
    {
        private readonly IConnectionFactory _db;
        public ClassSubjectRepository(IConnectionFactory db) { _db = db; }

        /// <summary>Subjects currently mapped to a class for an academic year.</summary>
        public IEnumerable<ClassSubjectDto> GetMapping(string tenantDbName, int schoolId,
            int academicYearId, int classId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<ClassSubjectDto>(
                    @"SELECT cs.class_subject_id ClassSubjectId, cs.academic_year_id AcademicYearId,
                             cs.class_id ClassId, c.class_name ClassName,
                             cs.subject_id SubjectId, sub.subject_name SubjectName,
                             sub.short_name ShortName, sub.subject_type SubjectType,
                             cs.display_order DisplayOrder, cs.is_optional IsOptional, cs.status Status
                      FROM class_subjects cs
                      JOIN classes  c   ON c.class_id     = cs.class_id
                      JOIN subjects sub ON sub.subject_id = cs.subject_id
                      WHERE cs.school_id = @schoolId
                        AND cs.academic_year_id = @academicYearId
                        AND cs.class_id = @classId
                        AND cs.status = 'Active'
                      ORDER BY ISNULL(cs.display_order, 9999), sub.subject_name",
                    new { schoolId, academicYearId, classId });
        }

        /// <summary>
        /// Distinct subjects mapped to a class across ALL academic years — used by
        /// the exam setup, which picks subjects by class only (no year filter).
        /// </summary>
        public IEnumerable<AvailableSubjectDto> GetSubjectsForClass(string tenantDbName, int schoolId, int classId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<AvailableSubjectDto>(
                    @"SELECT DISTINCT sub.subject_id SubjectId, sub.subject_name SubjectName,
                             sub.short_name ShortName, sub.subject_type SubjectType
                      FROM class_subjects cs
                      JOIN subjects sub ON sub.subject_id = cs.subject_id
                      WHERE cs.school_id = @schoolId
                        AND cs.class_id = @classId
                        AND cs.status = 'Active'
                      ORDER BY sub.subject_name",
                    new { schoolId, classId });
        }

        /// <summary>
        /// All active subjects of the school, addable to any class's mapping.
        /// Deliberately NOT filtered by academic year — subjects are a school-level
        /// master; the year lives on the class_subjects mapping, not on the picker.
        /// </summary>
        public IEnumerable<AvailableSubjectDto> GetAvailableSubjects(string tenantDbName, int schoolId,
            int academicYearId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<AvailableSubjectDto>(
                    @"SELECT subject_id SubjectId, subject_name SubjectName,
                             short_name ShortName, subject_type SubjectType
                      FROM subjects
                      WHERE school_id = @schoolId
                        AND ISNULL(status, 'Active') = 'Active'
                      ORDER BY subject_name",
                    new { schoolId });
        }

        /// <summary>
        /// Replace a class's whole subject set for a year (delete + insert in one
        /// transaction), so the saved list is exactly what the form submitted.
        /// </summary>
        public void SaveMapping(string tenantDbName, int schoolId, string createdBy, SaveClassSubjectsRequest req)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    conn.Execute(
                        @"DELETE FROM class_subjects
                          WHERE school_id = @schoolId AND academic_year_id = @AcademicYearId
                            AND class_id = @ClassId",
                        new { schoolId, req.AcademicYearId, req.ClassId }, tx);

                    if (req.Subjects != null)
                    {
                        foreach (var s in req.Subjects)
                        {
                            conn.Execute(
                                @"INSERT INTO class_subjects
                                    (academic_year_id, class_id, subject_id, display_order,
                                     is_optional, school_id, created_by)
                                  VALUES (@AcademicYearId, @ClassId, @SubjectId, @DisplayOrder,
                                          @IsOptional, @schoolId, @createdBy)",
                                new
                                {
                                    req.AcademicYearId,
                                    req.ClassId,
                                    s.SubjectId,
                                    s.DisplayOrder,
                                    s.IsOptional,
                                    schoolId,
                                    createdBy,
                                }, tx);
                        }
                    }

                    tx.Commit();
                }
            }
        }
    }
}
