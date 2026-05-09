using AscentSchools.Core.DTOs.School.Students;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;
using System.Text;

namespace AscentSchools.Data.Repositories.School
{
    public class StudentRepository
    {
        private readonly IConnectionFactory _db;
        public StudentRepository(IConnectionFactory db) { _db = db; }

        // ── List ──────────────────────────────────────────────────────────

        public IEnumerable<StudentListDto> GetAll(
            string tenantDbName, int schoolId,
            string search, int? classId, int? sectionId, int? academicYearId, string status,
            string bloodGroup = null)
        {
            var where = new StringBuilder("s.school_id = @schoolId");
            if (!string.IsNullOrWhiteSpace(search))
                where.Append(" AND (s.student_name LIKE @search OR s.admission_no LIKE @search)");
            if (classId.HasValue)
                where.Append(" AND s.class_id = @classId");
            if (sectionId.HasValue)
                where.Append(" AND s.section_id = @sectionId");
            if (academicYearId.HasValue)
                where.Append(" AND s.academic_year_id = @academicYearId");
            if (!string.IsNullOrWhiteSpace(status))
                where.Append(" AND s.status = @status");
            if (!string.IsNullOrWhiteSpace(bloodGroup))
                where.Append(" AND s.blood_group = @bloodGroup");

            var sql = $@"
                SELECT s.student_id StudentId, s.admission_no AdmissionNo,
                       s.student_name StudentName, c.class_name ClassName,
                       sec.section_name SectionName,
                       s.gender Gender, s.status Status,
                       s.photo_path PhotoPath, s.school_id SchoolId,
                       s.date_of_birth DateOfBirth,
                       s.father_name FatherName,
                       s.father_mobile FatherMobile,
                       s.blood_group BloodGroup,
                       s.blocked_reason BlockedReason,
                       ISNULL(s.is_detained, 0) IsDetained,
                       s.detained_reason DetainedReason
                FROM students s
                LEFT JOIN classes  c   ON c.class_id   = s.class_id
                LEFT JOIN sections sec ON sec.section_id = s.section_id
                WHERE {where}
                ORDER BY s.student_name";

            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<StudentListDto>(sql, new
                {
                    schoolId,
                    search        = $"%{search}%",
                    classId,
                    sectionId,
                    academicYearId,
                    status,
                    bloodGroup
                });
        }

        // ── Detail ────────────────────────────────────────────────────────

        public StudentDto GetById(string tenantDbName, int schoolId, long studentId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QueryFirstOrDefault<StudentDto>(
                    @"SELECT
                        s.student_id StudentId, s.admission_no AdmissionNo,
                        s.school_unique_id SchoolUniqueId, s.student_name StudentName,
                        s.short_name ShortName, s.join_type JoinType,
                        s.gender Gender, s.date_of_birth DateOfBirth,
                        s.blood_group BloodGroup, s.status Status, s.photo_path PhotoPath,
                        s.academic_year_id AcademicYearId, ay.academic_year AcademicYear,
                        s.class_id ClassId, c.class_name ClassName,
                        s.branch_name BranchName, s.section_id SectionId,
                        sec.section_name SectionName, s.roll_no RollNo,
                        s.date_of_joining DateOfJoining, s.admission_date AdmissionDate,
                        s.fee_category_id FeeCategoryId, s.joining_class JoiningClass,
                        s.join_term JoinTerm,
                        s.guardian_type GuardianType,
                        s.father_name FatherName, s.father_qualification FatherQualification,
                        s.father_occupation FatherOccupation,
                        s.father_employment_type FatherEmploymentType,
                        s.father_mobile FatherMobile,
                        s.mother_name MotherName, s.mother_qualification MotherQualification,
                        s.mother_occupation MotherOccupation, s.mother_mobile MotherMobile,
                        s.annual_income AnnualIncome, s.family_children_count FamilyChildrenCount,
                        s.door_no DoorNo, s.address_area AddressArea,
                        s.address_city AddressCity, s.address_state AddressState,
                        s.permanent_address PermanentAddress, s.email Email,
                        s.caste Caste, s.caste_code CasteCode,
                        s.religion Religion, s.nationality Nationality,
                        s.aadhar_no AadharNo, s.udise_no UdiseNo,
                        s.dob_proof_submitted DobProofSubmitted,
                        s.caste_cert_submitted CasteCertSubmitted,
                        s.other_certificates OtherCertificates,
                        s.disability_status DisabilityStatus,
                        s.disability_type DisabilityType,
                        s.biometric_id BiometricId,
                        s.transport_type TransportType,
                        s.bus_route_id BusRouteId, s.bus_id BusId, s.bus_trip BusTrip,
                        s.student_type StudentType, s.hostel_name HostelName,
                        s.scholarship_status ScholarshipStatus,
                        s.scholarship_description ScholarshipDescription,
                        s.mother_tongue MotherTongue,
                        s.first_language FirstLanguage, s.second_language SecondLanguage,
                        s.third_language ThirdLanguage,
                        s.reference_name ReferenceName, s.remarks Remarks,
                        s.spare_field_1 SpareField1, s.spare_field_2 SpareField2,
                        s.school_id SchoolId
                    FROM students s
                    LEFT JOIN classes        c   ON c.class_id         = s.class_id
                    LEFT JOIN sections      sec ON sec.section_id      = s.section_id
                    LEFT JOIN academic_years ay  ON ay.academic_year_id = s.academic_year_id
                    WHERE s.student_id = @studentId AND s.school_id = @schoolId",
                    new { studentId, schoolId });
        }

        // ── Create ────────────────────────────────────────────────────────

        public long Create(string tenantDbName, int schoolId, SaveStudentRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<long>(
                    @"INSERT INTO students (
                        admission_no, school_unique_id, student_name, short_name, join_type,
                        gender, date_of_birth, blood_group, status,
                        academic_year_id, class_id, branch_name, section_id, roll_no,
                        date_of_joining, admission_date, fee_category_id,
                        joining_class, join_term,
                        guardian_type,
                        father_name, father_qualification, father_occupation,
                        father_employment_type, father_mobile,
                        mother_name, mother_qualification, mother_occupation, mother_mobile,
                        annual_income, family_children_count,
                        door_no, address_area, address_city, address_state,
                        permanent_address, email,
                        caste, caste_code, religion, nationality,
                        aadhar_no, udise_no,
                        dob_proof_submitted, caste_cert_submitted, other_certificates,
                        disability_status, disability_type, biometric_id,
                        transport_type, bus_route_id, bus_id, bus_trip,
                        student_type, hostel_name,
                        scholarship_status, scholarship_description,
                        mother_tongue, first_language, second_language, third_language,
                        reference_name, remarks, spare_field_1, spare_field_2,
                        school_id
                      ) VALUES (
                        @AdmissionNo, @SchoolUniqueId, @StudentName, @ShortName, @JoinType,
                        @Gender, @DateOfBirth, @BloodGroup, @Status,
                        @AcademicYearId, @ClassId, @BranchName, @SectionId, @RollNo,
                        @DateOfJoining, @AdmissionDate, @FeeCategoryId,
                        @JoiningClass, @JoinTerm,
                        @GuardianType,
                        @FatherName, @FatherQualification, @FatherOccupation,
                        @FatherEmploymentType, @FatherMobile,
                        @MotherName, @MotherQualification, @MotherOccupation, @MotherMobile,
                        @AnnualIncome, @FamilyChildrenCount,
                        @DoorNo, @AddressArea, @AddressCity, @AddressState,
                        @PermanentAddress, @Email,
                        @Caste, @CasteCode, @Religion, @Nationality,
                        @AadharNo, @UdiseNo,
                        @DobProofSubmitted, @CasteCertSubmitted, @OtherCertificates,
                        @DisabilityStatus, @DisabilityType, @BiometricId,
                        @TransportType, @BusRouteId, @BusId, @BusTrip,
                        @StudentType, @HostelName,
                        @ScholarshipStatus, @ScholarshipDescription,
                        @MotherTongue, @FirstLanguage, @SecondLanguage, @ThirdLanguage,
                        @ReferenceName, @Remarks, @SpareField1, @SpareField2,
                        @schoolId
                      );
                      SELECT CAST(SCOPE_IDENTITY() AS BIGINT)",
                    new
                    {
                        r.AdmissionNo, r.SchoolUniqueId, r.StudentName, r.ShortName, r.JoinType,
                        r.Gender, r.DateOfBirth, r.BloodGroup, r.Status,
                        r.AcademicYearId, r.ClassId, r.BranchName, r.SectionId, r.RollNo,
                        r.DateOfJoining, r.AdmissionDate, r.FeeCategoryId,
                        r.JoiningClass, r.JoinTerm,
                        r.GuardianType,
                        r.FatherName, r.FatherQualification, r.FatherOccupation,
                        r.FatherEmploymentType, r.FatherMobile,
                        r.MotherName, r.MotherQualification, r.MotherOccupation, r.MotherMobile,
                        r.AnnualIncome, r.FamilyChildrenCount,
                        r.DoorNo, r.AddressArea, r.AddressCity, r.AddressState,
                        r.PermanentAddress, r.Email,
                        r.Caste, r.CasteCode, r.Religion, r.Nationality,
                        r.AadharNo, r.UdiseNo,
                        r.DobProofSubmitted, r.CasteCertSubmitted, r.OtherCertificates,
                        r.DisabilityStatus, r.DisabilityType, r.BiometricId,
                        r.TransportType, r.BusRouteId, r.BusId, r.BusTrip,
                        r.StudentType, r.HostelName,
                        r.ScholarshipStatus, r.ScholarshipDescription,
                        r.MotherTongue, r.FirstLanguage, r.SecondLanguage, r.ThirdLanguage,
                        r.ReferenceName, r.Remarks, r.SpareField1, r.SpareField2,
                        schoolId
                    });
        }

        // ── Update ────────────────────────────────────────────────────────

        public void Update(string tenantDbName, int schoolId, long studentId, SaveStudentRequest r)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE students SET
                        admission_no = @AdmissionNo, school_unique_id = @SchoolUniqueId,
                        student_name = @StudentName, short_name = @ShortName,
                        join_type = @JoinType, gender = @Gender,
                        date_of_birth = @DateOfBirth, blood_group = @BloodGroup, status = @Status,
                        academic_year_id = @AcademicYearId, class_id = @ClassId,
                        branch_name = @BranchName, section_id = @SectionId, roll_no = @RollNo,
                        date_of_joining = @DateOfJoining, admission_date = @AdmissionDate,
                        fee_category_id = @FeeCategoryId,
                        joining_class = @JoiningClass, join_term = @JoinTerm,
                        guardian_type = @GuardianType,
                        father_name = @FatherName, father_qualification = @FatherQualification,
                        father_occupation = @FatherOccupation,
                        father_employment_type = @FatherEmploymentType,
                        father_mobile = @FatherMobile,
                        mother_name = @MotherName, mother_qualification = @MotherQualification,
                        mother_occupation = @MotherOccupation, mother_mobile = @MotherMobile,
                        annual_income = @AnnualIncome, family_children_count = @FamilyChildrenCount,
                        door_no = @DoorNo, address_area = @AddressArea,
                        address_city = @AddressCity, address_state = @AddressState,
                        permanent_address = @PermanentAddress, email = @Email,
                        caste = @Caste, caste_code = @CasteCode,
                        religion = @Religion, nationality = @Nationality,
                        aadhar_no = @AadharNo, udise_no = @UdiseNo,
                        dob_proof_submitted = @DobProofSubmitted,
                        caste_cert_submitted = @CasteCertSubmitted,
                        other_certificates = @OtherCertificates,
                        disability_status = @DisabilityStatus, disability_type = @DisabilityType,
                        biometric_id = @BiometricId,
                        transport_type = @TransportType,
                        bus_route_id = @BusRouteId, bus_id = @BusId, bus_trip = @BusTrip,
                        student_type = @StudentType, hostel_name = @HostelName,
                        scholarship_status = @ScholarshipStatus,
                        scholarship_description = @ScholarshipDescription,
                        mother_tongue = @MotherTongue,
                        first_language = @FirstLanguage, second_language = @SecondLanguage,
                        third_language = @ThirdLanguage,
                        reference_name = @ReferenceName, remarks = @Remarks,
                        spare_field_1 = @SpareField1, spare_field_2 = @SpareField2
                      WHERE student_id = @studentId AND school_id = @schoolId",
                    new
                    {
                        r.AdmissionNo, r.SchoolUniqueId, r.StudentName, r.ShortName, r.JoinType,
                        r.Gender, r.DateOfBirth, r.BloodGroup, r.Status,
                        r.AcademicYearId, r.ClassId, r.BranchName, r.SectionId, r.RollNo,
                        r.DateOfJoining, r.AdmissionDate, r.FeeCategoryId,
                        r.JoiningClass, r.JoinTerm,
                        r.GuardianType,
                        r.FatherName, r.FatherQualification, r.FatherOccupation,
                        r.FatherEmploymentType, r.FatherMobile,
                        r.MotherName, r.MotherQualification, r.MotherOccupation, r.MotherMobile,
                        r.AnnualIncome, r.FamilyChildrenCount,
                        r.DoorNo, r.AddressArea, r.AddressCity, r.AddressState,
                        r.PermanentAddress, r.Email,
                        r.Caste, r.CasteCode, r.Religion, r.Nationality,
                        r.AadharNo, r.UdiseNo,
                        r.DobProofSubmitted, r.CasteCertSubmitted, r.OtherCertificates,
                        r.DisabilityStatus, r.DisabilityType, r.BiometricId,
                        r.TransportType, r.BusRouteId, r.BusId, r.BusTrip,
                        r.StudentType, r.HostelName,
                        r.ScholarshipStatus, r.ScholarshipDescription,
                        r.MotherTongue, r.FirstLanguage, r.SecondLanguage, r.ThirdLanguage,
                        r.ReferenceName, r.Remarks, r.SpareField1, r.SpareField2,
                        studentId, schoolId
                    });
        }

        // ── Change Section ────────────────────────────────────────────────

        public void ChangeSection(string tenantDbName, int schoolId, long studentId, int sectionId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "UPDATE students SET section_id = @sectionId WHERE student_id = @studentId AND school_id = @schoolId",
                    new { sectionId, studentId, schoolId });
        }

        // ── Photo ─────────────────────────────────────────────────────────

        public void UpdatePhotoPath(string tenantDbName, int schoolId, long studentId, string photoPath)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "UPDATE students SET photo_path = @photoPath WHERE student_id = @studentId AND school_id = @schoolId",
                    new { photoPath, studentId, schoolId });
        }

        // ── Promotion ─────────────────────────────────────────────────────────

        public IEnumerable<PromotePreviewDto> GetForPromotion(
            string tenantDbName, int schoolId, int fromYearId, int fromClassId, int? fromSectionId)
        {
            var where = new StringBuilder(
                "s.school_id = @schoolId AND s.academic_year_id = @fromYearId AND s.class_id = @fromClassId AND s.status IN ('Active','Y') AND ISNULL(s.is_detained,0) = 0");
            if (fromSectionId.HasValue)
                where.Append(" AND s.section_id = @fromSectionId");

            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<PromotePreviewDto>($@"
                    SELECT s.student_id StudentId, s.admission_no AdmissionNo,
                           s.student_name StudentName, c.class_name ClassName,
                           sec.section_name SectionName
                    FROM students s
                    LEFT JOIN classes  c   ON c.class_id    = s.class_id
                    LEFT JOIN sections sec ON sec.section_id = s.section_id
                    WHERE {where}
                    ORDER BY s.student_name",
                    new { schoolId, fromYearId, fromClassId, fromSectionId });
        }

        public int CountDetained(
            string tenantDbName, int schoolId, int fromYearId, int fromClassId, int? fromSectionId)
        {
            var where = new StringBuilder(
                "school_id = @schoolId AND academic_year_id = @fromYearId AND class_id = @fromClassId AND status IN ('Active','Y') AND ISNULL(is_detained,0) = 1");
            if (fromSectionId.HasValue)
                where.Append(" AND section_id = @fromSectionId");

            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.QuerySingle<int>(
                    $"SELECT COUNT(*) FROM students WHERE {where}",
                    new { schoolId, fromYearId, fromClassId, fromSectionId });
        }

        public PromoteStudentsResult Promote(
            string tenantDbName, int schoolId, PromoteStudentsRequest req)
        {
            var result = new PromoteStudentsResult();

            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var students = GetForPromotion(
                    tenantDbName, schoolId, req.FromAcademicYearId, req.FromClassId, req.FromSectionId);


                foreach (var s in students)
                {
                    result.Total++;

                    // Skip if already promoted (row for this admission_no + target year exists)
                    var exists = conn.ExecuteScalar<int>(
                        @"SELECT COUNT(1) FROM students
                          WHERE admission_no = @admNo AND school_id = @schoolId
                            AND academic_year_id = @toYearId",
                        new { admNo = s.AdmissionNo, schoolId, toYearId = req.ToAcademicYearId });

                    if (exists > 0) { result.Skipped++; continue; }

                    int? resolvedSectionId = req.ToSectionId;

                    // Copy all personal fields; set new academic placement
                    conn.Execute(@"
                        INSERT INTO students (
                            school_id, admission_no, school_unique_id, student_name, short_name,
                            join_type, guardian_type,
                            father_name, father_qualification, father_occupation,
                            father_employment_type, father_mobile,
                            mother_name, mother_qualification, mother_occupation, mother_mobile,
                            date_of_birth, date_of_joining, admission_date,
                            gender, blood_group, caste, caste_code, religion, nationality,
                            door_no, address_area, address_city, address_state, permanent_address,
                            email, annual_income, family_children_count,
                            aadhar_no, dob_proof_submitted, caste_cert_submitted, other_certificates,
                            transport_type, bus_route_id, bus_id, bus_trip,
                            joining_class, join_term, remarks, photo_path,
                            disability_status, disability_type, reference_name,
                            student_type, scholarship_status, scholarship_description,
                            hostel_name, biometric_id, mother_tongue,
                            first_language, second_language, third_language,
                            udise_no, spare_field_1, spare_field_2, branch_name,
                            fee_category_id,
                            academic_year_id, class_id, section_id, roll_no, status, created_by
                        )
                        SELECT
                            school_id, admission_no, school_unique_id, student_name, short_name,
                            join_type, guardian_type,
                            father_name, father_qualification, father_occupation,
                            father_employment_type, father_mobile,
                            mother_name, mother_qualification, mother_occupation, mother_mobile,
                            date_of_birth, date_of_joining, admission_date,
                            gender, blood_group, caste, caste_code, religion, nationality,
                            door_no, address_area, address_city, address_state, permanent_address,
                            email, annual_income, family_children_count,
                            aadhar_no, dob_proof_submitted, caste_cert_submitted, other_certificates,
                            transport_type, bus_route_id, bus_id, bus_trip,
                            joining_class, join_term, remarks, photo_path,
                            disability_status, disability_type, reference_name,
                            student_type, scholarship_status, scholarship_description,
                            hostel_name, biometric_id, mother_tongue,
                            first_language, second_language, third_language,
                            udise_no, spare_field_1, spare_field_2, branch_name,
                            fee_category_id,
                            @toYearId, @toClassId, @resolvedSectionId, NULL, 'Active', @promotedBy
                        FROM students
                        WHERE student_id = @studentId",
                        new {
                            studentId         = s.StudentId,
                            toYearId          = req.ToAcademicYearId,
                            toClassId         = req.ToClassId,
                            resolvedSectionId,
                            promotedBy        = "promotion"
                        });

                    result.Promoted++;
                }
            }

            return result;
        }

        // ── Bulk import ───────────────────────────────────────────────────────

        public BulkImportResult BulkCreate(string tenantDbName, int schoolId, BulkStudentImportRequest request)
        {
            var result = new BulkImportResult { Total = request.Rows.Count };

            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                // Build lookup maps once
                var years    = conn.Query<BulkLookup>("SELECT academic_year_id AS Id, academic_year AS Name FROM academic_years WHERE school_id = @schoolId", new { schoolId });
                var classes  = conn.Query<BulkLookup>("SELECT class_id AS Id, class_name AS Name FROM classes WHERE school_id = @schoolId", new { schoolId });
                var sections = conn.Query<BulkSectionLookup>("SELECT section_id AS SectionId, class_id AS ClassId, section_name AS SectionName FROM sections WHERE school_id = @schoolId", new { schoolId });
                var cats     = conn.Query<BulkLookup>("SELECT fee_category_id AS Id, category_name AS Name FROM fee_categories WHERE school_id = @schoolId", new { schoolId });

                var yearMap    = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
                var classMap   = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
                var sectionMap = new System.Collections.Generic.Dictionary<string, int>(); // key: "classId|sectionName"
                var catMap     = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

                foreach (var y in years)   yearMap[y.Name]  = y.Id;
                foreach (var c in classes)  classMap[c.Name] = c.Id;
                foreach (var s in sections) sectionMap[$"{s.ClassId}|{s.SectionName.ToLower()}"] = s.SectionId;
                foreach (var c in cats)     catMap[c.Name]   = c.Id;

                // Track admission numbers seen in this batch (duplicate detection within the file)
                var seenInBatch = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

                foreach (var row in request.Rows)
                {
                    // ── Validate required fields ──────────────────────────────
                    if (string.IsNullOrWhiteSpace(row.AdmissionNo))
                    { AddError(result, row, null, "AdmissionNo is required"); continue; }

                    if (string.IsNullOrWhiteSpace(row.StudentName))
                    { AddError(result, row, row.AdmissionNo, "StudentName is required"); continue; }

                    // ── Duplicate within batch ────────────────────────────────
                    if (!seenInBatch.Add(row.AdmissionNo.Trim()))
                    { AddError(result, row, row.AdmissionNo, "Duplicate AdmissionNo in file"); continue; }

                    // ── Duplicate in DB ───────────────────────────────────────
                    var exists = conn.ExecuteScalar<int>(
                        "SELECT COUNT(1) FROM students WHERE admission_no = @admNo AND school_id = @schoolId",
                        new { admNo = row.AdmissionNo.Trim(), schoolId });
                    if (exists > 0)
                    { AddError(result, row, row.AdmissionNo, "AdmissionNo already exists"); continue; }

                    // ── Resolve lookups ───────────────────────────────────────
                    int? academicYearId = null;
                    if (!string.IsNullOrWhiteSpace(row.AcademicYear))
                    {
                        if (!yearMap.TryGetValue(row.AcademicYear.Trim(), out var yid))
                        { AddError(result, row, row.AdmissionNo, $"Academic year '{row.AcademicYear}' not found"); continue; }
                        academicYearId = yid;
                    }

                    int? classId = null;
                    if (!string.IsNullOrWhiteSpace(row.ClassName))
                    {
                        if (!classMap.TryGetValue(row.ClassName.Trim(), out var cid))
                        { AddError(result, row, row.AdmissionNo, $"Class '{row.ClassName}' not found"); continue; }
                        classId = cid;
                    }

                    int? sectionId = null;
                    if (!string.IsNullOrWhiteSpace(row.SectionName) && classId.HasValue)
                    {
                        var skey = $"{classId.Value}|{row.SectionName.Trim().ToLower()}";
                        if (!sectionMap.TryGetValue(skey, out var sid))
                        { AddError(result, row, row.AdmissionNo, $"Section '{row.SectionName}' not found for class '{row.ClassName}'"); continue; }
                        sectionId = sid;
                    }

                    int? feeCategoryId = null;
                    if (!string.IsNullOrWhiteSpace(row.FeeCategory))
                    {
                        if (!catMap.TryGetValue(row.FeeCategory.Trim(), out var fcid))
                        { AddError(result, row, row.AdmissionNo, $"Fee category '{row.FeeCategory}' not found"); continue; }
                        feeCategoryId = fcid;
                    }

                    // ── Parse optional date ───────────────────────────────────
                    System.DateTime? dob = null;
                    if (!string.IsNullOrWhiteSpace(row.DateOfBirth))
                    {
                        System.DateTime parsed;
                        string[] formats = { "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "MM/dd/yyyy" };
                        if (!System.DateTime.TryParseExact(row.DateOfBirth.Trim(), formats,
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out parsed))
                        { AddError(result, row, row.AdmissionNo, $"Invalid DateOfBirth '{row.DateOfBirth}'. Use dd/MM/yyyy"); continue; }
                        dob = parsed;
                    }

                    var status = string.IsNullOrWhiteSpace(row.Status) ? "Active" : row.Status.Trim();

                    // ── Insert ────────────────────────────────────────────────
                    conn.Execute(@"
                        INSERT INTO students
                            (school_id, admission_no, student_name, gender, date_of_birth,
                             academic_year_id, class_id, section_id, roll_no,
                             fee_category_id, father_name, mother_name,
                             father_mobile, mother_mobile,
                             aadhar_no, caste, caste_code, religion,
                             joining_class, mother_tongue, status)
                        VALUES
                            (@schoolId, @admissionNo, @studentName, @gender, @dob,
                             @academicYearId, @classId, @sectionId, @rollNo,
                             @feeCategoryId, @fatherName, @motherName,
                             @fatherMobile, @motherMobile,
                             @aadharNo, @caste, @casteCode, @religion,
                             @joiningClass, @motherTongue, @status)",
                        new {
                            schoolId,
                            admissionNo   = row.AdmissionNo.Trim(),
                            studentName   = row.StudentName.Trim(),
                            gender        = row.Gender,
                            dob,
                            academicYearId,
                            classId,
                            sectionId,
                            rollNo        = row.RollNo,
                            feeCategoryId,
                            fatherName    = row.FatherName,
                            motherName    = row.MotherName,
                            fatherMobile  = row.FatherMobile,
                            motherMobile  = row.MotherMobile,
                            aadharNo      = row.AadharNo,
                            caste         = row.Caste,
                            casteCode     = row.CasteCode,
                            religion      = row.Religion,
                            joiningClass  = row.JoiningClass,
                            motherTongue  = row.MotherTongue,
                            status
                        });

                    result.Imported++;
                }
            }

            result.Failed = result.Total - result.Imported;
            return result;
        }

        // ── Block / Unblock ───────────────────────────────────────────────

        public void BlockStudent(string tenantDbName, int schoolId, long studentId, string reason)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE students
                      SET status = 'Blocked', blocked_reason = @reason
                      WHERE student_id = @studentId AND school_id = @schoolId",
                    new { studentId, schoolId, reason });
        }

        public void UnblockStudent(string tenantDbName, int schoolId, long studentId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE students
                      SET status = 'Active', blocked_reason = NULL
                      WHERE student_id = @studentId AND school_id = @schoolId",
                    new { studentId, schoolId });
        }

        // ── Detain / Release ──────────────────────────────────────────────

        public void DetainStudent(string tenantDbName, int schoolId, long studentId, string reason)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE students
                      SET is_detained = 1, detained_reason = @reason
                      WHERE student_id = @studentId AND school_id = @schoolId",
                    new { studentId, schoolId, reason });
        }

        public void ReleaseStudent(string tenantDbName, int schoolId, long studentId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    @"UPDATE students
                      SET is_detained = 0, detained_reason = NULL
                      WHERE student_id = @studentId AND school_id = @schoolId",
                    new { studentId, schoolId });
        }

        private static void AddError(BulkImportResult result, StudentBulkRow row, string admNo, string reason)
        {
            result.Errors.Add(new BulkRowError
            {
                Row         = row.RowNumber,
                AdmissionNo = admNo,
                Identifier  = admNo,
                Reason      = reason
            });
        }
    }

    internal class BulkLookup
    {
        public int    Id   { get; set; }
        public string Name { get; set; }
    }

    internal class BulkSectionLookup
    {
        public int    SectionId   { get; set; }
        public int    ClassId     { get; set; }
        public string SectionName { get; set; }
    }
}
