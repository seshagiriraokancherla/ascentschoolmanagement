using System;
using AscentSyncTool.Models;

namespace AscentSyncTool.Mappers
{
    public static class Mappers
    {
        // Mobile number fields in legacy may hold multiple numbers ("99../98..,..").
        // Take the first token, capped at 10 digits.
        private static string FirstMobile(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var first = raw.Split(new[] { '/', ',' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return first.Length > 10 ? first.Substring(0, 10) : first;
        }

        private static string Trim(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // ── Export Receipts: legacy → bulk receipt row ────────────────────────
        public static BulkReceiptRow ToBulkReceipt(LegacyFeeReceipt r)
        {
            return new BulkReceiptRow
            {
                LegacyReceiptNo = Trim(r.FeeReciptID),
                AdmissionNo     = Trim(r.StuAdmnNo),
                AcademicYear    = Trim(r.AcdYear),
                FeeCategory     = "School",
                FeeTypeName     = Trim(r.FeeTyp),
                TermName        = Trim(r.PaymentTyp),
                PaidAmount      = (r.FeeAmt ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
                PaymentDate     = r.FeeDat?.ToString("yyyy-MM-dd"),
                PaymentMode     = Trim(r.PymntTyp),
                StudentUniqueId = Trim(r.RefNo),   // legacy RefNo holds SAS_StudentMaster.StuUnqID → student_unique_id
                ChequeNo        = null,            // RefNo is the unique id, not a cheque no
                Remarks         = Trim(r.RemarksDet),
                Status          = Trim(r.FeeReceiptStat),
                CancelReason    = Trim(r.DeletResn),
            };
        }

        // ── Export Students: legacy → bulk student row ────────────────────────
        public static StudentBulkRow ToBulkStudent(LegacyStudent s)
        {
            return new StudentBulkRow
            {
                AdmissionNo  = Trim(s.StuAmnNo),
                StudentName  = Trim(s.StuName),
                Gender       = Trim(s.StuGender),
                DateOfBirth  = s.StuDOB?.ToString("dd/MM/yyyy"),
                AcademicYear = Trim(s.StuAcdYear),
                ClassName    = Trim(s.StuClassNam),
                SectionName  = Trim(s.StuSect),
                RollNo       = Trim(s.StuClassRolNo),
                FeeCategory  = null,                       // legacy student has no fee category
                FatherName   = Trim(s.StuFatherName),
                MotherName   = Trim(s.StuMotherName),
                FatherMobile = FirstMobile(s.StuMobile1),
                MotherMobile = FirstMobile(s.StuMobile2),
                AadharNo     = Trim(s.StuAdarNo),
                Caste        = Trim(s.StuCaste),
                CasteCode    = Trim(s.StuCasteCode),
                Religion     = Trim(s.StuReligion),
                JoiningClass = Trim(s.StuStartClass),
                MotherTongue = Trim(s.MothrLang),
                Status       = MapStudentStatus(s.StuStatus),
            };
        }

        private static string MapStudentStatus(string legacy)
        {
            switch (legacy?.Trim())
            {
                case "A": return "Active";
                case "D": return "Inactive";
                default:  return string.IsNullOrWhiteSpace(legacy) ? "Active" : legacy.Trim();
            }
        }

        // ── Download Receipts: API receipt detail + item → legacy row ─────────
        public static LegacyFeeReceipt ToLegacyReceipt(ReceiptDetail d, ReceiptItem item)
        {
            // Fee type label: route/hostel name for transport/hostel items, else fee type.
            var feeTyp = !string.IsNullOrWhiteSpace(item.FeeTypeName) ? item.FeeTypeName
                       : !string.IsNullOrWhiteSpace(item.RouteName)   ? item.RouteName
                       : !string.IsNullOrWhiteSpace(item.HostelName)  ? item.HostelName
                       : null;

            return new LegacyFeeReceipt
            {
                FeeReciptID    = d.ReceiptNo,
                FeeDat         = d.PaymentDate,
                StuAdmnNo      = d.AdmissionNo,
                AcdYear        = d.AcademicYear,
                ClassNam       = d.ClassName,
                FeeTyp         = feeTyp,
                PaymentTyp     = item.TermName,
                FeeAmt         = item.NetAmount,
                PymntTyp       = d.PaymentModeName,
                RefNo          = d.StudentUniqueId?.ToString(),  // legacy RefNo = student_unique_id
                RemarksDet     = d.Remarks,
                FeeReceiptStat = string.Equals(d.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) ? "D" : "A",
            };
        }
    }
}
