namespace AscentSchools.Core.DTOs.Mobile.Data
{
    public class BirthdayStudentDto
    {
        public long   StudentId   { get; set; }
        public string AdmissionNo { get; set; }
        public string StudentName { get; set; }
        public string ClassName   { get; set; }
        public string SectionName { get; set; }
        public string DateOfBirth { get; set; }   // "yyyy-MM-dd" — client computes "turning N"
    }

    public class StudentProfileDto
    {
        public long   StudentId     { get; set; }
        public string AdmissionNo   { get; set; }
        public string FullName      { get; set; }
        public string ClassName     { get; set; }
        public string SectionName   { get; set; }
        public string DateOfBirth   { get; set; }
        public string Gender        { get; set; }
        public string BloodGroup    { get; set; }
        public string FatherName    { get; set; }
        public string MotherName    { get; set; }
        public string Mobile        { get; set; }
        public string Email         { get; set; }
        public string Address       { get; set; }
        public string PhotoPath     { get; set; }
        public string AcademicYear  { get; set; }
    }
}
