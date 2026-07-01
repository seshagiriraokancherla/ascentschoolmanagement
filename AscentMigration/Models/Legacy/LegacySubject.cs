using System;

namespace AscentMigration.Models.Legacy
{
    public class LegacySubject
    {
        public string    SubJectID     { get; set; }
        public string    SubjectNam    { get; set; }
        public string    ShortNam      { get; set; }
        public string    SubjectTyp    { get; set; }
        public string    Descrpt       { get; set; }
        public string    RemarksDet    { get; set; }
        public string    AcdYear       { get; set; }
        public string    SubjectStatus { get; set; }
        public string    Crtby         { get; set; }
        public DateTime? CrtDat        { get; set; }
        public string    BranchID      { get; set; }
        public string    MachID        { get; set; }
    }
}
