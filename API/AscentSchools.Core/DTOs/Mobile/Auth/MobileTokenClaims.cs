namespace AscentSchools.Core.DTOs.Mobile.Auth
{
    /// <summary>Claims embedded in a student mobile JWT (tokenType=student).</summary>
    public class MobileStudentClaims
    {
        public int    AccountId   { get; set; }   // student_mobile_accounts.account_id
        public long   StudentId   { get; set; }   // students.student_id
        public int    GroupId     { get; set; }
        public int    SchoolId    { get; set; }
        public string DbName      { get; set; }
        public string StudentName { get; set; }
        public string ClassName   { get; set; }
        public string AdmissionNo { get; set; }
    }

    /// <summary>Claims embedded in a parent mobile JWT.</summary>
    public class MobileParentClaims
    {
        public int    ParentId    { get; set; }
        public string FullName    { get; set; }
        /// <summary>Non-zero only after select-child.</summary>
        public long   StudentId   { get; set; }
        public int    GroupId     { get; set; }
        public int    SchoolId    { get; set; }
        public string DbName      { get; set; }
        public string StudentName { get; set; }
        public string ClassName   { get; set; }
        public string AdmissionNo { get; set; }
    }
}
