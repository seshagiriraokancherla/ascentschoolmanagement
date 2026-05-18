using AscentSchools.Core.DTOs.School.Settings;
using AscentSchools.Data.ConnectionFactory;
using Dapper;

namespace AscentSchools.Data.Repositories.School
{
    public class SchoolSettingsRepository
    {
        private readonly IConnectionFactory _db;
        public SchoolSettingsRepository(IConnectionFactory db) { _db = db; }

        public SchoolSettingsDto Get(int schoolId)
        {
            using (var conn = _db.GetMasterConnection())
            {
                var row = conn.QueryFirstOrDefault<SchoolSettingsDto>(
                    @"SELECT
                        admission_no_type            AdmissionNoType,
                        category_wise_admissions     CategoryWiseAdmissions,
                        new_student_entry_mode       NewStudentEntryMode,
                        pre_primary_admission_prefix PrePrimaryAdmissionPrefix,
                        primary_admission_prefix     PrimaryAdmissionPrefix,
                        high_school_admission_prefix HighSchoolAdmissionPrefix,
                        general_admission_prefix     GeneralAdmissionPrefix,
                        fee_receipt_lock             FeeReceiptLock,
                        fee_receipt_print            FeeReceiptPrint,
                        receipt_print_copies         ReceiptPrintCopies,
                        transport_fee_included       TransportFeeIncluded,
                        fine_enabled                 FineEnabled,
                        receipt_fee_type_separator   ReceiptFeeTypeSeparator,
                        bill_no_series_type          BillNoSeriesType,
                        student_concession_enabled   StudentConcessionEnabled,
                        fee_message_to_teacher       FeeMessageToTeacher,
                        billing_status               BillingStatus,
                        progress_report_type         ProgressReportType,
                        institution_head_name        InstitutionHeadName,
                        institution_head_signature   InstitutionHeadSignature,
                        other_subjects_type          OtherSubjectsType
                      FROM school_settings
                      WHERE school_id = @schoolId",
                    new { schoolId });

                return row ?? new SchoolSettingsDto();
            }
        }

        public void Upsert(int schoolId, UpdateSchoolSettingsRequest r, string updatedBy)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    @"IF EXISTS (SELECT 1 FROM school_settings WHERE school_id = @schoolId)
                          UPDATE school_settings SET
                              admission_no_type            = @AdmissionNoType,
                              category_wise_admissions     = @CategoryWiseAdmissions,
                              new_student_entry_mode       = @NewStudentEntryMode,
                              pre_primary_admission_prefix = @PrePrimaryAdmissionPrefix,
                              primary_admission_prefix     = @PrimaryAdmissionPrefix,
                              high_school_admission_prefix = @HighSchoolAdmissionPrefix,
                              general_admission_prefix     = @GeneralAdmissionPrefix,
                              fee_receipt_lock             = @FeeReceiptLock,
                              fee_receipt_print            = @FeeReceiptPrint,
                              receipt_print_copies         = @ReceiptPrintCopies,
                              transport_fee_included       = @TransportFeeIncluded,
                              fine_enabled                 = @FineEnabled,
                              receipt_fee_type_separator   = @ReceiptFeeTypeSeparator,
                              bill_no_series_type          = @BillNoSeriesType,
                              student_concession_enabled   = @StudentConcessionEnabled,
                              fee_message_to_teacher       = @FeeMessageToTeacher,
                              billing_status               = @BillingStatus,
                              progress_report_type         = @ProgressReportType,
                              institution_head_name        = @InstitutionHeadName,
                              institution_head_signature   = @InstitutionHeadSignature,
                              other_subjects_type          = @OtherSubjectsType,
                              created_by                   = @updatedBy
                          WHERE school_id = @schoolId
                      ELSE
                          INSERT INTO school_settings (
                              school_id, admission_no_type, category_wise_admissions,
                              new_student_entry_mode, pre_primary_admission_prefix,
                              primary_admission_prefix, high_school_admission_prefix,
                              general_admission_prefix, fee_receipt_lock, fee_receipt_print,
                              receipt_print_copies, transport_fee_included, fine_enabled,
                              receipt_fee_type_separator, bill_no_series_type,
                              student_concession_enabled, fee_message_to_teacher,
                              billing_status, progress_report_type, institution_head_name,
                              institution_head_signature, other_subjects_type, created_by
                          ) VALUES (
                              @schoolId, @AdmissionNoType, @CategoryWiseAdmissions,
                              @NewStudentEntryMode, @PrePrimaryAdmissionPrefix,
                              @PrimaryAdmissionPrefix, @HighSchoolAdmissionPrefix,
                              @GeneralAdmissionPrefix, @FeeReceiptLock, @FeeReceiptPrint,
                              @ReceiptPrintCopies, @TransportFeeIncluded, @FineEnabled,
                              @ReceiptFeeTypeSeparator, @BillNoSeriesType,
                              @StudentConcessionEnabled, @FeeMessageToTeacher,
                              @BillingStatus, @ProgressReportType, @InstitutionHeadName,
                              @InstitutionHeadSignature, @OtherSubjectsType, @updatedBy
                          )",
                    new
                    {
                        schoolId,
                        r.AdmissionNoType,           r.CategoryWiseAdmissions,
                        r.NewStudentEntryMode,        r.PrePrimaryAdmissionPrefix,
                        r.PrimaryAdmissionPrefix,     r.HighSchoolAdmissionPrefix,
                        r.GeneralAdmissionPrefix,     r.FeeReceiptLock,
                        r.FeeReceiptPrint,            r.ReceiptPrintCopies,
                        r.TransportFeeIncluded,       r.FineEnabled,
                        r.ReceiptFeeTypeSeparator,    r.BillNoSeriesType,
                        r.StudentConcessionEnabled,   r.FeeMessageToTeacher,
                        r.BillingStatus,              r.ProgressReportType,
                        r.InstitutionHeadName,        r.InstitutionHeadSignature,
                        r.OtherSubjectsType,          updatedBy
                    });
        }
    }
}
