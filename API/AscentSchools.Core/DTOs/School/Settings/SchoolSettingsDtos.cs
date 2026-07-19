namespace AscentSchools.Core.DTOs.School.Settings
{
    // ── Cloudflare R2 storage config (per school) ──────────────────────────────
    public class R2ConfigDto
    {
        public string AccountId     { get; set; }
        public string AccessKeyId   { get; set; }
        public string BucketName    { get; set; }
        public string PublicBaseUrl { get; set; }
        public bool   IsEnabled     { get; set; }
        public bool   HasSecretKey  { get; set; }   // true if a secret is stored (never returns the value)
    }

    public class UpdateR2ConfigRequest
    {
        public string AccountId       { get; set; }
        public string AccessKeyId     { get; set; }
        public string SecretAccessKey { get; set; }   // blank = keep existing
        public string BucketName      { get; set; }
        public string PublicBaseUrl   { get; set; }
        public bool   IsEnabled       { get; set; }
    }

    // Internal — full config incl. secret, for server-side presigning.
    public class R2ConfigInternal
    {
        public string AccountId       { get; set; }
        public string AccessKeyId     { get; set; }
        public string SecretAccessKey { get; set; }
        public string BucketName      { get; set; }
        public string PublicBaseUrl   { get; set; }
        public bool   IsEnabled       { get; set; }
    }

    // Presigned upload request/response.
    public class PresignRequest
    {
        public string Purpose     { get; set; }   // student-photo | homework | announcement | event
        public long   EntityId    { get; set; }   // studentId / homeworkId / announcementId / eventId
        public string FileName    { get; set; }   // original name (for extension)
        public string ContentType { get; set; }   // MIME type the browser will PUT with
    }

    public class PresignResponse
    {
        public string UploadUrl { get; set; }   // presigned PUT URL (browser uploads directly to R2)
        public string PublicUrl { get; set; }   // permanent public URL to store in DB / show in app
        public string Key       { get; set; }   // object key
    }

    public class SchoolSettingsDto
    {
        // Admission
        public string AdmissionNoType            { get; set; }
        public string CategoryWiseAdmissions     { get; set; }  // Y / N
        public string NewStudentEntryMode        { get; set; }  // Fast Entry / Normal Entry
        public string PrePrimaryAdmissionPrefix  { get; set; }
        public string PrimaryAdmissionPrefix     { get; set; }
        public string HighSchoolAdmissionPrefix  { get; set; }
        public string GeneralAdmissionPrefix     { get; set; }

        // Fee & Billing
        public int?   FeeReceiptLock             { get; set; }
        public string FeeReceiptPrint            { get; set; }
        public int?   ReceiptPrintCopies         { get; set; }
        public string TransportFeeIncluded       { get; set; }  // Y / N
        public string FineEnabled                { get; set; }  // Y / N
        public string ReceiptFeeTypeSeparator    { get; set; }
        public string BillNoSeriesType           { get; set; }
        public string StudentConcessionEnabled   { get; set; }  // Enable / Disable
        public string FeeMessageToTeacher        { get; set; }  // Y / N
        public string BillingStatus              { get; set; }

        // Reports & Institution
        public string ProgressReportType        { get; set; }
        public string InstitutionHeadName       { get; set; }
        public string InstitutionHeadSignature  { get; set; }
        public string OtherSubjectsType         { get; set; }
    }

    public class UpdateSchoolSettingsRequest
    {
        // Admission
        public string AdmissionNoType            { get; set; }
        public string CategoryWiseAdmissions     { get; set; }
        public string NewStudentEntryMode        { get; set; }
        public string PrePrimaryAdmissionPrefix  { get; set; }
        public string PrimaryAdmissionPrefix     { get; set; }
        public string HighSchoolAdmissionPrefix  { get; set; }
        public string GeneralAdmissionPrefix     { get; set; }

        // Fee & Billing
        public int?   FeeReceiptLock             { get; set; }
        public string FeeReceiptPrint            { get; set; }
        public int?   ReceiptPrintCopies         { get; set; }
        public string TransportFeeIncluded       { get; set; }
        public string FineEnabled                { get; set; }
        public string ReceiptFeeTypeSeparator    { get; set; }
        public string BillNoSeriesType           { get; set; }
        public string StudentConcessionEnabled   { get; set; }
        public string FeeMessageToTeacher        { get; set; }
        public string BillingStatus              { get; set; }

        // Reports & Institution
        public string ProgressReportType        { get; set; }
        public string InstitutionHeadName       { get; set; }
        public string InstitutionHeadSignature  { get; set; }
        public string OtherSubjectsType         { get; set; }
    }
}
