using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using AscentSyncTool.Config;
using AscentSyncTool.Models;
using Dapper;

namespace AscentSyncTool.Data
{
    /// <summary>Direct access to the legacy VB6 (SAS) SQL Server database.</summary>
    public class LegacyDb
    {
        private SqlConnection Open()
        {
            var conn = new SqlConnection(AppSettings.LegacyConnectionString);
            conn.Open();
            return conn;
        }

        // ── Distinct academic years (for the filter dropdown) ─────────────────
        public List<string> GetAcademicYears()
        {
            using (var conn = Open())
                return conn.Query<string>(
                    @"SELECT DISTINCT AcademicYear FROM SAS_AcdYear
                      WHERE AcademicYear IS NOT NULL AND LTRIM(RTRIM(AcademicYear)) <> ''
                      ORDER BY AcademicYear DESC").AsList();
        }

        // ── Export: receipts by FeeDat range (+ optional academic year) ───────
        public List<LegacyFeeReceipt> GetReceipts(DateTime from, DateTime toInclusive, string academicYear = null)
        {
            using (var conn = Open())
                return conn.Query<LegacyFeeReceipt>(
                    @"SELECT FeeReciptID, FeeDat, StuAdmnNo, AcdYear, FeeTyp, PaymentTyp,
                             FeeAmt, PymntTyp, RefNo, RemarksDet, FeeReceiptStat, DeletResn
                      FROM SAS_FeeReceipts
                      WHERE FeeDat >= @from AND FeeDat < @toExclusive
                        AND (@acdYear IS NULL OR AcdYear = @acdYear)
                      ORDER BY FeeReciptID, Id_new",
                    new
                    {
                        from = from.Date,
                        toExclusive = toInclusive.Date.AddDays(1),
                        acdYear = string.IsNullOrWhiteSpace(academicYear) ? null : academicYear
                    }).AsList();
        }

        // ── Export: students by CrtDat range (+ optional academic year) ───────
        public List<LegacyStudent> GetStudents(DateTime from, DateTime toInclusive, string academicYear = null)
        {
            using (var conn = Open())
                return conn.Query<LegacyStudent>(
                    @"SELECT s.StuAmnNo, s.StuName, s.StuGender, s.StuDOB, s.StuAcdYear, s.StuClassNam,
                             s.StuSect, s.StuClassRolNo, s.StuFatherName, s.StuMotherName,
                             s.StuMobile1, s.StuMobile2, s.StuAdarNo, s.StuCaste, s.StuCasteCode,
                             s.StuReligion, s.StuStartClass, s.MothrLang, s.StuStatus, s.StuUnqID,
                             s.StuJoinTyp, s.StuFatherOccup, s.StuFatherEmpTyp, s.StuMotherOccup, s.StuDOJ,
                             s.StuNationlity, s.StuDoorNo, s.StuAddrArea, s.StuAddrCity, s.StuAddrState,
                             s.StuAddrPerminent, s.StuMailID, s.StuAnnualIncome, s.StuFamilyChildres,
                             s.StuDOBStat, s.StuCasteStat, s.StuTransportTyp, s.AdminDate, s.StudentType,
                             s.BloodGrp, s.JoinTerm, s.StuFirstLang, s.StuThirdLang, s.StuUdiseNo, s.CrtDat,
                             br.RouteNam AS RouteName, bd.BusNam AS BusName
                      FROM SAS_StudentMaster s
                      LEFT JOIN SAS_BusRoutes br ON br.RouteID = s.StuBusRoute
                      LEFT JOIN SAS_BussData  bd ON bd.BusID   = s.BusID
                      WHERE s.CrtDat >= @from AND s.CrtDat < @toExclusive
                        AND (@acdYear IS NULL OR s.StuAcdYear = @acdYear)
                      ORDER BY s.CrtDat",
                    new
                    {
                        from = from.Date,
                        toExclusive = toInclusive.Date.AddDays(1),
                        acdYear = string.IsNullOrWhiteSpace(academicYear) ? null : academicYear
                    }).AsList();
        }

        // ── Update Date of Births: every legacy student for one academic year ─────
        // Lightweight — only the columns the DOB-update tab actually needs, with no
        // CrtDat range (this tab operates on the whole year, not a creation window).
        public List<LegacyStudent> GetStudentsForDobUpdate(string academicYear)
        {
            using (var conn = Open())
                return conn.Query<LegacyStudent>(
                    @"SELECT s.StuAmnNo, s.StuName, s.StuDOB, s.StuAcdYear, s.StuUnqID
                      FROM SAS_StudentMaster s
                      WHERE s.StuAcdYear = @academicYear
                      ORDER BY s.StuName",
                    new { academicYear }).AsList();
        }

        // ── Export: concessions for one academic year, one row per key ────────
        // SAS_ConcessionDet stores one row per fee head per term; a key can have several
        // rows (rare) and deleted ones ('D'). Amount sums only the active rows, so the
        // new app is set to the legacy total and a fully-deleted key (Amount 0) cancels it.
        // Bus concessions carry the bus route id in FeeMasterID; every name that route id
        // has in SAS_BusRoutes is sent ('|' separated — some ids have two names).
        public List<LegacyConcession> GetConcessions(string academicYear)
        {
            using (var conn = Open())
                return conn.Query<LegacyConcession>(
                    @"WITH c AS (
                          SELECT LTRIM(RTRIM(c0.AdmnNo)) AdmnNo, LTRIM(RTRIM(c0.AcdamicYer)) AcdamicYer,
                                 LTRIM(RTRIM(c0.FeeTypID)) FeeTypID, LTRIM(RTRIM(c0.TermNam)) TermNam,
                                 CASE WHEN EXISTS (SELECT 1 FROM SAS_BusRoutes r WHERE r.RouteID = c0.FeeMasterID)
                                      THEN LTRIM(RTRIM(c0.FeeMasterID)) END RouteID,
                                 c0.ConcessionAmt, LTRIM(RTRIM(c0.TraStatus)) TraStatus, LTRIM(RTRIM(c0.TraID)) TraID,
                                 LTRIM(RTRIM(c0.RecommandByNam)) RecommandByNam, LTRIM(RTRIM(c0.RemarksDet)) RemarksDet,
                                 c0.CrtDat
                          FROM SAS_ConcessionDet c0
                          WHERE c0.AcdamicYer = @academicYear
                      )
                      SELECT g.AdmissionNo, g.AcademicYear,
                             CASE WHEN g.RouteID IS NULL THEN ft.FeeTyp END AS FeeTypeName,
                             rn.RouteNames, g.TermName, g.Amount, g.ActiveRows, g.DeletedRows,
                             lr.TraID AS LegacyNo, lr.RecommandByNam AS ConcessionType, lr.RemarksDet AS Remarks
                      FROM (
                          SELECT AdmnNo AdmissionNo, AcdamicYer AcademicYear, FeeTypID, TermNam TermName, RouteID,
                                 SUM(CASE WHEN TraStatus = 'A' THEN ISNULL(ConcessionAmt, 0) ELSE 0 END) Amount,
                                 SUM(CASE WHEN TraStatus = 'A' THEN 1 ELSE 0 END) ActiveRows,
                                 SUM(CASE WHEN TraStatus = 'A' THEN 0 ELSE 1 END) DeletedRows
                          FROM c
                          GROUP BY AdmnNo, AcdamicYer, FeeTypID, TermNam, RouteID
                      ) g
                      OUTER APPLY (SELECT TOP 1 LTRIM(RTRIM(f.FeeTyp)) FeeTyp
                                   FROM SAS_FeeTypes f WHERE f.FeeTypID = g.FeeTypID) ft
                      OUTER APPLY (SELECT STUFF((SELECT DISTINCT '|' + LTRIM(RTRIM(r.RouteNam))
                                                 FROM SAS_BusRoutes r WHERE r.RouteID = g.RouteID
                                                 FOR XML PATH(''), TYPE).value('.', 'varchar(max)'), 1, 1, '') RouteNames) rn
                      OUTER APPLY (SELECT TOP 1 x.TraID, x.RecommandByNam, x.RemarksDet FROM c x
                                   WHERE x.AdmnNo = g.AdmissionNo
                                     AND ISNULL(x.FeeTypID, '') = ISNULL(g.FeeTypID, '')
                                     AND ISNULL(x.TermNam, '')  = ISNULL(g.TermName, '')
                                     AND ISNULL(x.RouteID, '')  = ISNULL(g.RouteID, '')
                                   ORDER BY CASE WHEN x.TraStatus = 'A' THEN 0 ELSE 1 END, x.CrtDat DESC) lr
                      ORDER BY g.AdmissionNo, FeeTypeName, g.TermName",
                    new { academicYear }).AsList();
        }

        // ── Branch id from the licence table (same value for every inserted row) ──
        public string GetBranchId()
        {
            using (var conn = Open())
                return conn.ExecuteScalar<string>("SELECT TOP 1 BranchID FROM SAS_LicenceDet");
        }

        // ── Attendance summary: push one month into SAS_BulkAttendance ────────
        // Re-running a month does NOT delete anything: the previous rows for that
        // admission no + month + academic year are marked AttnStatus='D' and a fresh
        // 'A' row is inserted, so the legacy side keeps its own history.
        // Returns the number of rows superseded (marked 'D').
        public int SaveBulkAttendance(AttendanceExportRow row, DateTime monthStart, string branchId)
        {
            var attnMonth = monthStart.ToString("yyyy-MM-dd");

            using (var conn = Open())
            {
                var superseded = conn.Execute(
                    @"UPDATE SAS_BulkAttendance
                      SET AttnStatus = 'D'
                      WHERE StuAdmn = @admn AND AttnMonth = @month
                        AND AttnAcdYear = @acdYear AND ISNULL(AttnStatus, 'A') <> 'D'",
                    new { admn = row.AdmissionNo, month = attnMonth, acdYear = row.AcademicYear });

                conn.Execute(
                    @"INSERT INTO SAS_BulkAttendance
                        (AttnDat, AttnMonth, StuAdmn, AttnDys, AttnStatus,
                         CrtBy, CrtDat, BranchID, MachID, DeltBy, TraId, AttnAcdYear)
                      VALUES
                        (@attnDat, @month, @admn, @days, 'A',
                         'SyncTool', @crtDat, @branchId, '', '', '', @acdYear)",
                    new
                    {
                        attnDat  = DateTime.Today,
                        month    = attnMonth,
                        admn     = row.AdmissionNo,
                        days     = (double)row.NoOfDaysPresent,
                        crtDat   = DateTime.Now,
                        branchId = branchId ?? "",
                        acdYear  = row.AcademicYear
                    });

                return superseded;
            }
        }

        // ── Download: does a receipt already exist in legacy? ─────────────────
        public bool ReceiptExists(string feeReceiptId)
        {
            using (var conn = Open())
                return conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM SAS_FeeReceipts WHERE FeeReciptID = @id",
                    new { id = feeReceiptId }) > 0;
        }

        // ── Download: insert one legacy receipt line-item row ─────────────────
        // FeeMasterID and FeeID are resolved from the legacy master tables by name:
        //   FeeMasterID ← SAS_FeeMaster   (ClassNam + NofPayments[=term] + FeeTyp + AcdYear, Stat 'A')
        //   FeeID       ← SAS_FeeTypes     (FeeTyp, Status 'A')
        public void InsertReceipt(LegacyFeeReceipt r)
        {

            using (var conn = Open())
            {
                conn.Execute("alter table SAS_FeeReceipts alter column RemarksDet varchar(200)");
                conn.Execute(
                    @"INSERT INTO SAS_FeeReceipts
                        (FeeReciptID, FeeDat, RefNo, StuAdmnNo, FeeTyp, FeeAmt,
                         RemarksDet, PaymentTyp, ClassNam, AcdYear, FeeReceiptStat,
                         FeeMasterID, FeeID, BranchID,
                         CrtBy, CrtDat, PymntTyp,MachID,DeletBy,DeletResn,DelDate)
                      VALUES
                        (@FeeReciptID, @FeeDat, @RefNo, @StuAdmnNo, @FeeTyp, @FeeAmt,
                         @RemarksDet, @PaymentTyp, @ClassNam, @AcdYear, @FeeReceiptStat,
                         (SELECT TOP 1 FeeMasterID FROM SAS_FeeMaster
                            WHERE ClassNam = @ClassNam AND NofPayments = @PaymentTyp
                              AND FeeTyp = @FeeTyp AND FeeMasterStat = 'A' AND AcdYear = @AcdYear),
                         (SELECT TOP 1 FeeTypID FROM SAS_FeeTypes
                            WHERE FeeTyp = @FeeTyp AND FeeTypStatus = 'A'),
                         (SELECT TOP 1 BranchID FROM SAS_LicenceDet),
                         'websync', GETDATE(), @PymntTyp,'','','',getdate())",
                    r);
            }
        }
    }
}
