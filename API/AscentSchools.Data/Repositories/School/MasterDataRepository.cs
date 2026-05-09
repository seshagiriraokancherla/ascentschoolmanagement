using AscentSchools.Core.DTOs.School.Master;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.School
{
    public class MasterDataRepository
    {
        private readonly IConnectionFactory _db;
        public MasterDataRepository(IConnectionFactory db) { _db = db; }

        // ── Academic Years ────────────────────────────────────────────────

        public IEnumerable<AcademicYearDto> GetAcademicYears(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<AcademicYearDto>(
                    @"SELECT academic_year_id AcademicYearId, academic_year AcademicYear,
                             start_month StartMonth, end_month EndMonth, status Status,
                             registration_fee_frequency RegistrationFeeFrequency,
                             transport_fee_frequency TransportFeeFrequency,
                             hostel_fee_frequency HostelFeeFrequency,
                             boarding_type BoardingType,
                             new_admissions_enabled NewAdmissionsEnabled,
                             school_id SchoolId
                      FROM academic_years
                      WHERE school_id = @schoolId
                      ORDER BY academic_year_id DESC",
                    new { schoolId });
        }

        public int CreateAcademicYear(string tenantDbName, int schoolId, SaveAcademicYearRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO academic_years
                        (academic_year, start_month, end_month, status,
                         registration_fee_frequency, transport_fee_frequency,
                         hostel_fee_frequency, boarding_type, new_admissions_enabled, school_id)
                      VALUES
                        (@AcademicYear, @StartMonth, @EndMonth, @Status,
                         @RegistrationFeeFrequency, @TransportFeeFrequency,
                         @HostelFeeFrequency, @BoardingType, @NewAdmissionsEnabled, @schoolId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new
                    {
                        r.AcademicYear, r.StartMonth, r.EndMonth, r.Status,
                        r.RegistrationFeeFrequency, r.TransportFeeFrequency,
                        r.HostelFeeFrequency, r.BoardingType, r.NewAdmissionsEnabled, schoolId
                    });
        }

        public void UpdateAcademicYear(string tenantDbName, int schoolId, int id, SaveAcademicYearRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE academic_years SET
                        academic_year = @AcademicYear, start_month = @StartMonth, end_month = @EndMonth,
                        status = @Status, registration_fee_frequency = @RegistrationFeeFrequency,
                        transport_fee_frequency = @TransportFeeFrequency,
                        hostel_fee_frequency = @HostelFeeFrequency, boarding_type = @BoardingType,
                        new_admissions_enabled = @NewAdmissionsEnabled
                      WHERE academic_year_id = @id AND school_id = @schoolId",
                    new
                    {
                        r.AcademicYear, r.StartMonth, r.EndMonth, r.Status,
                        r.RegistrationFeeFrequency, r.TransportFeeFrequency,
                        r.HostelFeeFrequency, r.BoardingType, r.NewAdmissionsEnabled, id, schoolId
                    });
        }

        // ── Class Groups ──────────────────────────────────────────────────

        public IEnumerable<ClassGroupDto> GetClassGroups(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<ClassGroupDto>(
                    @"SELECT class_group_id ClassGroupId, group_name GroupName,
                             description Description, prefix Prefix,
                             status Status, school_id SchoolId
                      FROM class_groups
                      WHERE school_id = @schoolId
                      ORDER BY group_name",
                    new { schoolId });
        }

        public int CreateClassGroup(string tenantDbName, int schoolId, SaveClassGroupRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO class_groups (group_name, description, prefix, status, school_id)
                      VALUES (@GroupName, @Description, @Prefix, @Status, @schoolId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { r.GroupName, r.Description, r.Prefix, r.Status, schoolId });
        }

        public void UpdateClassGroup(string tenantDbName, int schoolId, int id, SaveClassGroupRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE class_groups SET group_name = @GroupName, description = @Description,
                        prefix = @Prefix, status = @Status
                      WHERE class_group_id = @id AND school_id = @schoolId",
                    new { r.GroupName, r.Description, r.Prefix, r.Status, id, schoolId });
        }

        // ── Fee Categories ────────────────────────────────────────────────

        public IEnumerable<FeeCategoryDto> GetFeeCategories(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<FeeCategoryDto>(
                    @"SELECT fee_category_id FeeCategoryId, category_name CategoryName,
                             academic_year_id AcademicYearId, description Description,
                             status Status, school_id SchoolId
                      FROM fee_categories
                      WHERE school_id = @schoolId
                      ORDER BY category_name",
                    new { schoolId });
        }

        public int CreateFeeCategory(string tenantDbName, int schoolId, SaveFeeCategoryRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO fee_categories (category_name, academic_year_id, description, status, school_id)
                      VALUES (@CategoryName, @AcademicYearId, @Description, @Status, @schoolId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { r.CategoryName, r.AcademicYearId, r.Description, r.Status, schoolId });
        }

        public void UpdateFeeCategory(string tenantDbName, int schoolId, int id, SaveFeeCategoryRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE fee_categories SET category_name = @CategoryName,
                        academic_year_id = @AcademicYearId, description = @Description, status = @Status
                      WHERE fee_category_id = @id AND school_id = @schoolId",
                    new { r.CategoryName, r.AcademicYearId, r.Description, r.Status, id, schoolId });
        }

        // ── Classes ───────────────────────────────────────────────────────

        public IEnumerable<ClassDto> GetClasses(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<ClassDto>(
                    @"SELECT c.class_id ClassId, c.class_name ClassName, c.branch_name BranchName,
                             c.description Description, c.status Status,
                             c.class_group_id ClassGroupId, cg.group_name ClassGroupName,
                             c.fee_category_id FeeCategoryId, c.sequence_no SequenceNo,
                             c.school_id SchoolId
                      FROM classes c
                      LEFT JOIN class_groups cg ON cg.class_group_id = c.class_group_id
                      WHERE c.school_id = @schoolId
                      ORDER BY ISNULL(c.sequence_no, 9999), c.class_name",
                    new { schoolId });
        }

        public int CreateClass(string tenantDbName, int schoolId, SaveClassRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO classes
                        (class_name, branch_name, description, status,
                         class_group_id, fee_category_id, sequence_no, school_id)
                      VALUES
                        (@ClassName, @BranchName, @Description, @Status,
                         @ClassGroupId, @FeeCategoryId, @SequenceNo, @schoolId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { r.ClassName, r.BranchName, r.Description, r.Status, r.ClassGroupId, r.FeeCategoryId, r.SequenceNo, schoolId });
        }

        public void UpdateClass(string tenantDbName, int schoolId, int id, SaveClassRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE classes SET class_name = @ClassName, branch_name = @BranchName,
                        description = @Description, status = @Status,
                        class_group_id = @ClassGroupId, fee_category_id = @FeeCategoryId,
                        sequence_no = @SequenceNo
                      WHERE class_id = @id AND school_id = @schoolId",
                    new { r.ClassName, r.BranchName, r.Description, r.Status, r.ClassGroupId, r.FeeCategoryId, r.SequenceNo, id, schoolId });
        }

        // ── Sections ──────────────────────────────────────────────────────

        public IEnumerable<SectionDto> GetSections(string tenantDbName, int schoolId, int classId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<SectionDto>(
                    @"SELECT section_id SectionId, class_id ClassId, section_name SectionName,
                             description Description, sequence_no SequenceNo, status Status, school_id SchoolId
                      FROM sections
                      WHERE class_id = @classId AND school_id = @schoolId
                      ORDER BY ISNULL(sequence_no, 9999), section_name",
                    new { classId, schoolId });
        }

        public int CreateSection(string tenantDbName, int schoolId, SaveSectionRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO sections (class_id, section_name, description, sequence_no, status, school_id)
                      VALUES (@ClassId, @SectionName, @Description, @SequenceNo, @Status, @schoolId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { r.ClassId, r.SectionName, r.Description, r.SequenceNo, r.Status, schoolId });
        }

        public void UpdateSection(string tenantDbName, int schoolId, int id, SaveSectionRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE sections SET section_name = @SectionName, description = @Description,
                        sequence_no = @SequenceNo, status = @Status
                      WHERE section_id = @id AND school_id = @schoolId",
                    new { r.SectionName, r.Description, r.SequenceNo, r.Status, id, schoolId });
        }

        // ── Fee Types ─────────────────────────────────────────────────────

        public IEnumerable<FeeTypeDto> GetFeeTypes(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<FeeTypeDto>(
                    @"SELECT fee_type_id FeeTypeId, fee_type_name FeeTypeName,
                             academic_year_id AcademicYearId, sequence_no SequenceNo,
                             description Description, status Status, school_id SchoolId
                      FROM fee_types
                      WHERE school_id = @schoolId
                      ORDER BY ISNULL(sequence_no, 9999), fee_type_name",
                    new { schoolId });
        }

        public int CreateFeeType(string tenantDbName, int schoolId, SaveFeeTypeRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO fee_types (fee_type_name, academic_year_id, sequence_no, description, status, school_id)
                      VALUES (@FeeTypeName, @AcademicYearId, @SequenceNo, @Description, @Status, @schoolId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { r.FeeTypeName, r.AcademicYearId, r.SequenceNo, r.Description, r.Status, schoolId });
        }

        public void UpdateFeeType(string tenantDbName, int schoolId, int id, SaveFeeTypeRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE fee_types SET fee_type_name = @FeeTypeName, academic_year_id = @AcademicYearId,
                        sequence_no = @SequenceNo, description = @Description, status = @Status
                      WHERE fee_type_id = @id AND school_id = @schoolId",
                    new { r.FeeTypeName, r.AcademicYearId, r.SequenceNo, r.Description, r.Status, id, schoolId });
        }

        // ── Terms ─────────────────────────────────────────────────────────

        public IEnumerable<TermDto> GetTerms(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<TermDto>(
                    @"SELECT term_id TermId, term_name TermName, year_name YearName,
                             order_no OrderNo, description Description,
                             academic_year_id AcademicYearId, status Status, school_id SchoolId
                      FROM terms
                      WHERE school_id = @schoolId
                      ORDER BY academic_year_id, ISNULL(order_no, 9999)",
                    new { schoolId });
        }

        public int CreateTerm(string tenantDbName, int schoolId, SaveTermRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO terms (term_name, year_name, order_no, description, academic_year_id, status, school_id)
                      VALUES (@TermName, @YearName, @OrderNo, @Description, @AcademicYearId, @Status, @schoolId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { r.TermName, r.YearName, r.OrderNo, r.Description, r.AcademicYearId, r.Status, schoolId });
        }

        public void UpdateTerm(string tenantDbName, int schoolId, int id, SaveTermRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE terms SET term_name = @TermName, year_name = @YearName, order_no = @OrderNo,
                        description = @Description, academic_year_id = @AcademicYearId, status = @Status
                      WHERE term_id = @id AND school_id = @schoolId",
                    new { r.TermName, r.YearName, r.OrderNo, r.Description, r.AcademicYearId, r.Status, id, schoolId });
        }

        // ── Subjects ──────────────────────────────────────────────────────

        public IEnumerable<SubjectDto> GetSubjects(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<SubjectDto>(
                    @"SELECT subject_id SubjectId, subject_name SubjectName, short_name ShortName,
                             subject_type SubjectType, description Description, remarks Remarks,
                             academic_year_id AcademicYearId, status Status, school_id SchoolId
                      FROM subjects
                      WHERE school_id = @schoolId
                      ORDER BY subject_name",
                    new { schoolId });
        }

        public int CreateSubject(string tenantDbName, int schoolId, SaveSubjectRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO subjects
                        (subject_name, short_name, subject_type, description, remarks, academic_year_id, status, school_id)
                      VALUES
                        (@SubjectName, @ShortName, @SubjectType, @Description, @Remarks, @AcademicYearId, @Status, @schoolId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { r.SubjectName, r.ShortName, r.SubjectType, r.Description, r.Remarks, r.AcademicYearId, r.Status, schoolId });
        }

        public void UpdateSubject(string tenantDbName, int schoolId, int id, SaveSubjectRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE subjects SET subject_name = @SubjectName, short_name = @ShortName,
                        subject_type = @SubjectType, description = @Description, remarks = @Remarks,
                        academic_year_id = @AcademicYearId, status = @Status
                      WHERE subject_id = @id AND school_id = @schoolId",
                    new { r.SubjectName, r.ShortName, r.SubjectType, r.Description, r.Remarks, r.AcademicYearId, r.Status, id, schoolId });
        }

        // ── Payment Modes ─────────────────────────────────────────────────

        public IEnumerable<PaymentModeDto> GetPaymentModes(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<PaymentModeDto>(
                    @"SELECT payment_mode_id PaymentModeId, mode_name ModeName,
                             description Description, status Status,
                             ISNULL(is_online, 0) IsOnline,
                             school_id SchoolId
                      FROM payment_modes
                      WHERE school_id = @schoolId
                      ORDER BY mode_name",
                    new { schoolId });
        }

        public int CreatePaymentMode(string tenantDbName, int schoolId, SavePaymentModeRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    @"INSERT INTO payment_modes (mode_name, description, status, is_online, school_id)
                      VALUES (@ModeName, @Description, @Status, @IsOnline, @schoolId);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { r.ModeName, r.Description, r.Status, r.IsOnline, schoolId });
        }

        public void UpdatePaymentMode(string tenantDbName, int schoolId, int id, SavePaymentModeRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE payment_modes
                      SET mode_name = @ModeName, description = @Description,
                          status = @Status, is_online = @IsOnline
                      WHERE payment_mode_id = @id AND school_id = @schoolId",
                    new { r.ModeName, r.Description, r.Status, r.IsOnline, id, schoolId });
        }
    }
}
