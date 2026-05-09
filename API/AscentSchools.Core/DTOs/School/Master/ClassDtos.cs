namespace AscentSchools.Core.DTOs.School.Master
{
    public class ClassDto
    {
        public int     ClassId        { get; set; }
        public string  ClassName      { get; set; }
        public string  BranchName     { get; set; }
        public string  Description    { get; set; }
        public string  Status         { get; set; }
        public int?    ClassGroupId   { get; set; }
        public string  ClassGroupName { get; set; }
        public int?    FeeCategoryId  { get; set; }
        public int?    SequenceNo     { get; set; }
        public int     SchoolId       { get; set; }
    }

    public class SaveClassRequest
    {
        public string ClassName     { get; set; }
        public string BranchName    { get; set; }
        public string Description   { get; set; }
        public string Status        { get; set; }
        public int?   ClassGroupId  { get; set; }
        public int?   FeeCategoryId { get; set; }
        public int?   SequenceNo    { get; set; }
    }
}
