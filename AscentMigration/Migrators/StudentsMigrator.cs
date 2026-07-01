using AscentMigration.Config;
using AscentMigration.Models.Legacy;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace AscentMigration.Migrators
{
    public class StudentsMigrator : BaseMigrator
    {
        public override string Name => "students";

        public StudentsMigrator(MigrationConfig config) : base(config) { }

        public override async Task<MigrationResult> RunAsync()
        {
            var result = new MigrationResult { Entity = Name };

            using (var src  = OpenSource())
            using (var dest = OpenDest())
            {
                // 1. Load all legacy rows
                var rows = (await src.QueryAsync<LegacyStudent>("SELECT * FROM SAS_StudentMaster where stuacdyear='2026-2027'")).AsList();
                result.Total = rows.Count;
                Log($"Found {result.Total} rows in SAS_StudentMaster");

                // 2. Pre-load all lookup dictionaries
                var acadYearMap        = await LoadAcadYearMap(dest);         // academic_year       → academic_year_id
                var classMap           = await LoadClassMap(dest);            // class_name          → class_id
                var sectionMap         = await LoadSectionMap(dest);          // "section_name|class_id" → section_id
                var legacyRouteNameMap = await LoadLegacyRouteNameMap(src);   // legacy RouteID      → RouteNam
                var destRouteIdMap     = await LoadDestRouteIdMap(dest);      // route_name          → route_id (Active wins on dup)

                Log($"Lookups loaded — years:{acadYearMap.Count} classes:{classMap.Count} sections:{sectionMap.Count} legacyRoutes:{legacyRouteNameMap.Count} destRoutes:{destRouteIdMap.Count}");

                // 3. Handle Truncate vs Skip
                var mode = Config.GetTableMode(Name);
                Log($"Mode: {mode}");

                if (mode == MigrationMode.Truncate && !Config.DryRun)
                {
                    var deleted = await dest.ExecuteAsync(
                        "DELETE FROM students WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    Log($"Deleted {deleted} existing rows for school_id={Config.SchoolId}");
                }

                // For Skip mode: composite key admission_no|academic_year_id
                var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (mode == MigrationMode.Skip)
                {
                    var existing = await dest.QueryAsync<StudentKey>(
                        @"SELECT ISNULL(admission_no,'')         AS admission_no,
                                 ISNULL(academic_year_id, 0)     AS academic_year_id
                          FROM students WHERE school_id = @SchoolId",
                        new { Config.SchoolId });
                    foreach (var e in existing)
                        existingKeys.Add(MakeKey(e.admission_no, e.academic_year_id));
                }

                // 4. Migrate row by row
                int processed = 0;
                foreach (var row in rows)
                {
                    processed++;
                    LogProgress(processed, result.Total);

                    // student_name is NOT NULL in dest — skip blank rows
                    var studentName = row.StuName?.Trim();
                    if (string.IsNullOrWhiteSpace(studentName))
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"StuID={row.StuID}",
                            Reason        = "StuName is empty — skipped"
                        });
                        continue;
                    }

                    // --- academic_year_id ---
                    int? academicYearId = null;
                    var acdYear = row.StuAcdYear?.Trim();
                    if (!string.IsNullOrWhiteSpace(acdYear))
                    {
                        if (acadYearMap.TryGetValue(acdYear, out var ayId))
                            academicYearId = ayId;
                        else
                            Log($"Warning: StuID={row.StuID} — academic_year '{acdYear}' not found — academic_year_id set to NULL");
                    }

                    // --- class_id ---
                    int? classId = null;
                    var className = row.StuClassNam?.Trim();
                    if (!string.IsNullOrWhiteSpace(className))
                    {
                        if (classMap.TryGetValue(className, out var cId))
                            classId = cId;
                        else
                            Log($"Warning: StuID={row.StuID} — class '{className}' not found — class_id set to NULL");
                    }

                    // --- section_id: needs class_id resolved first ---
                    int? sectionId = null;
                    var sectionName = row.StuSect?.Trim();
                    if (!string.IsNullOrWhiteSpace(sectionName) && classId.HasValue)
                    {
                        var sectionKey = $"{sectionName}|{classId}";
                        if (sectionMap.TryGetValue(sectionKey, out var sId))
                            sectionId = sId;
                        else
                            Log($"Warning: StuID={row.StuID} — section '{sectionName}' for class_id={classId} not found — section_id set to NULL");
                    }

                    // --- bus_route_id: two-step lookup ---
                    int? busRouteId = null;
                    var legacyRouteId = row.StuBusRoute?.Trim();
                    if (!string.IsNullOrWhiteSpace(legacyRouteId))
                    {
                        if (legacyRouteNameMap.TryGetValue(legacyRouteId, out var routeName))
                        {
                            if (destRouteIdMap.TryGetValue(routeName, out var rId))
                                busRouteId = rId;
                            else
                                Log($"Warning: StuID={row.StuID} — route_name '{routeName}' not found in dest bus_routes — bus_route_id set to NULL");
                        }
                        else
                            Log($"Warning: StuID={row.StuID} — StuBusRoute='{legacyRouteId}' not found in SAS_BusRoutes — bus_route_id set to NULL");
                    }

                    // --- family_children_count: varchar → int ---
                    int? familyChildrenCount = null;
                    var childrenRaw = row.StuFamilyChildres?.Trim();
                    if (!string.IsNullOrWhiteSpace(childrenRaw))
                    {
                        if (int.TryParse(childrenRaw, out var childCount))
                            familyChildrenCount = childCount;
                        else
                            Log($"Warning: StuID={row.StuID} — StuFamilyChildres='{childrenRaw}' is not a number — family_children_count set to NULL");
                    }

                    // --- student_unique_id: float → int ---
                    int? studentUniqueId = row.StuUnqID.HasValue ? (int?)((int)row.StuUnqID.Value) : null;

                    // --- Skip check ---
                    if (mode == MigrationMode.Skip)
                    {
                        var key = MakeKey(row.StuAmnNo?.Trim() ?? "", academicYearId);
                        if (existingKeys.Contains(key))
                        {
                            result.Skipped++;
                            continue;
                        }
                    }

                    try
                    {
                        if (!Config.DryRun)
                        {
                            await dest.ExecuteAsync(@"
                                INSERT INTO students (
                                    student_unique_id, admission_no, student_name, short_name,
                                    join_type,
                                    father_name, father_qualification, father_occupation, father_employment_type, father_mobile,
                                    mother_name, mother_qualification, mother_occupation, mother_mobile,
                                    date_of_birth, date_of_joining, academic_year_id, gender,
                                    second_language, class_id, section_id,
                                    roll_no, caste, religion, nationality,
                                    door_no, address_area, address_city, address_state, permanent_address,
                                    email, annual_income, family_children_count,
                                    dob_proof_submitted, aadhar_no, caste_cert_submitted, other_certificates,
                                    transport_type, bus_route_id, joining_class, remarks,
                                    status, admission_date,
                                    disability_status, disability_type,
                                    reference_name, student_type,
                                    scholarship_status, scholarship_description,
                                    blood_group, join_term, biometric_id,
                                    mother_tongue, first_language, third_language, udise_no,
                                    spare_field_1, spare_field_2,
                                    is_detained, school_id, created_by, created_at
                                ) VALUES (
                                    @StudentUniqueId, @AdmissionNo, @StudentName, @ShortName,
                                    @JoinType,
                                    @FatherName, @FatherQualification, @FatherOccupation, @FatherEmploymentType, @FatherMobile,
                                    @MotherName, @MotherQualification, @MotherOccupation, @MotherMobile,
                                    @DateOfBirth, @DateOfJoining, @AcademicYearId, @Gender,
                                    @SecondLanguage, @ClassId, @SectionId,
                                    @RollNo, @Caste, @Religion, @Nationality,
                                    @DoorNo, @AddressArea, @AddressCity, @AddressState, @PermanentAddress,
                                    @Email, @AnnualIncome, @FamilyChildrenCount,
                                    @DobProofSubmitted, @AadharNo, @CasteCertSubmitted, @OtherCertificates,
                                    @TransportType, @BusRouteId, @JoiningClass, @Remarks,
                                    @Status, @AdmissionDate,
                                    @DisabilityStatus, @DisabilityType,
                                    @ReferenceName, @StudentType,
                                    @ScholarshipStatus, @ScholarshipDescription,
                                    @BloodGroup, @JoinTerm, @BiometricId,
                                    @MotherTongue, @FirstLanguage, @ThirdLanguage, @UdiseNo,
                                    @SpareField1, @SpareField2,
                                    0, @SchoolId, @CreatedBy, @CreatedAt
                                )",
                                new
                                {
                                    StudentUniqueId       = studentUniqueId,
                                    AdmissionNo           = row.StuAmnNo?.Trim(),
                                    StudentName           = studentName,
                                    ShortName             = row.StuSurNam?.Trim(),
                                    JoinType              = row.StuJoinTyp?.Trim(),
                                    FatherName            = row.StuFatherName?.Trim(),
                                    FatherQualification   = row.StuFatherQualf?.Trim(),
                                    FatherOccupation      = row.StuFatherOccup?.Trim(),
                                    FatherEmploymentType  = row.StuFatherEmpTyp?.Trim(),
                                    FatherMobile          = TakeFirstMobile(row.StuMobile1),
                                    MotherName            = row.StuMotherName?.Trim(),
                                    MotherQualification   = row.StuMotherQualf?.Trim(),
                                    MotherOccupation      = row.StuMotherOccup?.Trim(),
                                    MotherMobile          = TakeFirstMobile(row.StuMobile2),
                                    DateOfBirth           = row.StuDOB,
                                    DateOfJoining         = row.StuDOJ,
                                    AcademicYearId        = academicYearId,
                                    Gender                = row.StuGender?.Trim(),
                                    SecondLanguage        = row.StuSecndLang?.Trim(),
                                    ClassId               = classId,
                                    SectionId             = sectionId,
                                    RollNo                = row.StuClassRolNo?.Trim(),
                                    Caste                 = row.StuCaste?.Trim(),
                                    Religion              = row.StuReligion?.Trim(),
                                    Nationality           = row.StuNationlity?.Trim(),
                                    DoorNo                = row.StuDoorNo?.Trim(),
                                    AddressArea           = row.StuAddrArea?.Trim(),
                                    AddressCity           = row.StuAddrCity?.Trim(),
                                    AddressState          = row.StuAddrState?.Trim(),
                                    PermanentAddress      = row.StuAddrPerminent?.Trim(),
                                    Email                 = row.StuMailID?.Trim(),
                                    AnnualIncome          = row.StuAnnualIncome,
                                    FamilyChildrenCount   = familyChildrenCount,
                                    DobProofSubmitted     = row.StuDOBStat?.Trim(),
                                    AadharNo              = row.StuAdarNo?.Trim(),
                                    CasteCertSubmitted    = row.StuCasteStat?.Trim(),
                                    OtherCertificates     = row.StuOtherCertificates?.Trim(),
                                    TransportType         = row.StuTransportTyp?.Trim(),
                                    BusRouteId            = busRouteId,
                                    JoiningClass          = row.StuStartClass?.Trim(),
                                    Remarks               = row.StuRemarks?.Trim(),
                                    Status                = MapStatus(row.StuStatus),
                                    AdmissionDate         = row.AdminDate,
                                    DisabilityStatus      = row.DisabilityStatus?.Trim(),
                                    DisabilityType        = row.DisabilityTyp?.Trim(),
                                    ReferenceName         = row.StuRefNam?.Trim(),
                                    StudentType           = row.StudentType?.Trim(),
                                    ScholarshipStatus     = row.StuScholarshipStatus?.Trim(),
                                    ScholarshipDescription= row.StuScholarshipDescript?.Trim(),
                                    BloodGroup            = row.BloodGrp?.Trim(),
                                    JoinTerm              = row.JoinTerm?.Trim(),
                                    BiometricId           = row.BioMatrcID?.Trim(),
                                    MotherTongue          = row.MothrLang?.Trim(),
                                    FirstLanguage         = row.StuFirstLang?.Trim(),
                                    ThirdLanguage         = row.StuThirdLang?.Trim(),
                                    UdiseNo               = row.StuUdiseNo?.Trim(),
                                    SpareField1           = row.StuMobile1?.Trim(),
                                    SpareField2           = row.StuMobile2?.Trim(),
                                    SchoolId              = Config.SchoolId,
                                    CreatedBy             = "migration",
                                    CreatedAt             = DateTime.Now
                                });
                        }

                        result.Migrated++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new MigrationError
                        {
                            RowIdentifier = $"StuID={row.StuID} AdmNo={row.StuAmnNo?.Trim()}",
                            Reason        = ex.Message
                        });
                    }
                }

                LogProgressDone();
            }

            return result;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        // Splits on '/' or ',' and returns the first token, capped at 10 digits
        private static string TakeFirstMobile(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var first = raw.Split(new[] { '/', ',' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return first.Length > 10 ? first.Substring(0, 10) : first;
        }

        // ---------------------------------------------------------------
        // Lookup loaders
        // ---------------------------------------------------------------

        private async Task<Dictionary<string, int>> LoadAcadYearMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<AcadYearRow>(
                "SELECT academic_year_id, academic_year FROM academic_years WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.academic_year?.Trim() ?? "";
                if (!map.ContainsKey(key)) map[key] = r.academic_year_id;
            }
            return map;
        }

        private async Task<Dictionary<string, int>> LoadClassMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<ClassRow>(
                "SELECT class_id, class_name FROM classes WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.class_name?.Trim() ?? "";
                if (!map.ContainsKey(key)) map[key] = r.class_id;
                else Log($"Warning: duplicate class_name '{key}' in dest — keeping first (id={map[key]})");
            }
            return map;
        }

        // Composite key: "section_name|class_id"
        private async Task<Dictionary<string, int>> LoadSectionMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<SectionRow>(
                "SELECT section_id, section_name, class_id FROM sections WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = $"{r.section_name?.Trim()}|{r.class_id}";
                if (!map.ContainsKey(key)) map[key] = r.section_id;
            }
            return map;
        }

        // Loads legacy RouteID → RouteNam from source SAS_BusRoutes
        private async Task<Dictionary<string, string>> LoadLegacyRouteNameMap(SqlConnection src)
        {
            var rows = await src.QueryAsync<LegacyRouteRow>("SELECT RouteID, RouteNam FROM SAS_BusRoutes");
            var map  = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key = r.RouteID?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                    map[key] = r.RouteNam?.Trim() ?? "";
            }
            return map;
        }

        // Loads route_name → route_id from dest bus_routes; Active row wins on duplicate name
        private async Task<Dictionary<string, int>> LoadDestRouteIdMap(SqlConnection dest)
        {
            var rows = await dest.QueryAsync<DestRouteRow>(
                "SELECT route_id, route_name, status FROM bus_routes WHERE school_id = @SchoolId",
                new { Config.SchoolId });
            var map       = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var mapStatus = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in rows)
            {
                var key    = r.route_name?.Trim() ?? "";
                var status = r.status?.Trim() ?? "";
                if (!map.ContainsKey(key))
                {
                    map[key]       = r.route_id;
                    mapStatus[key] = status;
                }
                else if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(mapStatus[key], "Active", StringComparison.OrdinalIgnoreCase))
                {
                    map[key]       = r.route_id;
                    mapStatus[key] = status;
                }
            }
            return map;
        }

        private static string MakeKey(string admissionNo, int? academicYearId)
            => $"{admissionNo ?? ""}|{academicYearId ?? 0}";

        private static string MapStatus(string legacy)
        {
            switch (legacy?.Trim())
            {
                case "A": return "Active";
                case "D": return "Inactive";
                case "TC": return "TC";
                case "R": return "R";
                default:  return null;
            }
        }

        // ---------------------------------------------------------------
        // Private POCOs for typed Dapper queries
        // ---------------------------------------------------------------

        private class AcadYearRow  { public int    academic_year_id { get; set; } public string academic_year { get; set; } }
        private class ClassRow     { public int    class_id         { get; set; } public string class_name    { get; set; } }
        private class SectionRow   { public int    section_id       { get; set; } public string section_name  { get; set; } public int class_id { get; set; } }
        private class LegacyRouteRow { public string RouteID        { get; set; } public string RouteNam      { get; set; } }
        private class DestRouteRow { public int    route_id         { get; set; } public string route_name    { get; set; } public string status { get; set; } }
        private class StudentKey   { public string admission_no     { get; set; } public int    academic_year_id { get; set; } }
    }
}
