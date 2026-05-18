namespace AscentSchools.Core.DTOs.School.Settings
{
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
