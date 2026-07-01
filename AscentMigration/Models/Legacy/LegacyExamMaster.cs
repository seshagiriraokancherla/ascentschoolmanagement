using System;

namespace AscentMigration.Models.Legacy
{
    // Full SAS_ExamNam row (exam_master source). The same table feeds ExamTypesMigrator,
    // which reads only the DISTINCT name columns; this model reads the per-class/subject detail.
    public class LegacyExamMaster
    {
        public string    ExamID         { get; set; }
        public string    ExamNam        { get; set; }
        public string    ClasId         { get; set; }
        public int?      ExamTotalMarks { get; set; }
        public int?      ExamMinMarks   { get; set; }
        public int?      SubMinMrks     { get; set; }
        public int?      SubMaxMrks     { get; set; }
        public string    ExamRemarks    { get; set; }
        public string    AcdYear        { get; set; }
        public string    SubjectID      { get; set; }
        public string    ExamStatus     { get; set; }
        public string    CrtBy          { get; set; }
        public DateTime? CrtDat         { get; set; }
        public string    BranchID       { get; set; }
        public string    MachID         { get; set; }
        public string    ExamCatgry     { get; set; }
        public DateTime? ExamDatTim     { get; set; }
        public string    GradeTyp       { get; set; }
    }
}
