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

        // ── Export: receipts by FeeDat range ──────────────────────────────────
        public List<LegacyFeeReceipt> GetReceipts(DateTime from, DateTime toInclusive)
        {
            using (var conn = Open())
                return conn.Query<LegacyFeeReceipt>(
                    @"SELECT FeeReciptID, FeeDat, StuAdmnNo, AcdYear, FeeTyp, PaymentTyp,
                             FeeAmt, PymntTyp, RefNo, RemarksDet, FeeReceiptStat, DeletResn
                      FROM SAS_FeeReceipts
                      WHERE FeeDat >= @from AND FeeDat < @toExclusive
                      ORDER BY FeeReciptID, Id_new",
                    new { from = from.Date, toExclusive = toInclusive.Date.AddDays(1) }).AsList();
        }

        // ── Export: students by CrtDat range ──────────────────────────────────
        public List<LegacyStudent> GetStudents(DateTime from, DateTime toInclusive)
        {
            using (var conn = Open())
                return conn.Query<LegacyStudent>(
                    @"SELECT StuAmnNo, StuName, StuGender, StuDOB, StuAcdYear, StuClassNam,
                             StuSect, StuClassRolNo, StuFatherName, StuMotherName,
                             StuMobile1, StuMobile2, StuAdarNo, StuCaste, StuCasteCode,
                             StuReligion, StuStartClass, MothrLang, StuStatus, CrtDat
                      FROM SAS_StudentMaster
                      WHERE CrtDat >= @from AND CrtDat < @toExclusive
                      ORDER BY CrtDat",
                    new { from = from.Date, toExclusive = toInclusive.Date.AddDays(1) }).AsList();
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
