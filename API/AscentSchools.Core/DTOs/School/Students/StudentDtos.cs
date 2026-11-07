using System;
using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Students
{
    /// <summary>Light DTO for list view — avoids SELECT *.</summary>
    public class StudentListDto
    {
        public long      StudentId       { get; set; }
        public int?      StudentUniqueId { get; set; }
        public string    AdmissionNo     { get; set; }
        public string    StudentName     { get; set; }
        public string    ClassName       { get; set; }
        public string    SectionName     { get; set; }
        public string    Gender          { get; set; }
        public string    Status          { get; set; }
        public string    PhotoPath       { get; set; }
        public int       SchoolId        { get; set; }
        public DateTime? DateOfBirth     { get; set; }
        public string    FatherName      { get; set; }
        public string    FatherMobile    { get; set; }
        public string    BloodGroup      { get; set; }
        public string    BlockedReason   { get; set; }
        public bool      IsDetained      { get; set; }
        public string    DetainedReason  { get; set; }
        public string    JoinType        { get; set; }
    }

    public class BlockStudentRequest
    {
        public string Reason { get; set; }
    }

    public class DetainStudentRequest
    {
        public string Reason { get; set; }
    }

    /// <summary>Full DTO for detail / edit view.</summary>
    public class StudentDto
    {
        public long      StudentId              { get; set; }
        // Basic
        public string    AdmissionNo            { get; set; }
        public string    SchoolUniqueId         { get; set; }
        public string    StudentName            { get; set; }
        public string    ShortName              { get; set; }
        public string    JoinType               { get; set; }
        public string    Gender                 { get; set; }
        public DateTime? DateOfBirth            { get; set; }
        public string    BloodGroup             { get; set; }
        public string    Status                 { get; set; }
        public string    PhotoPath              { get; set; }
        // Academic
        public int?      AcademicYearId         { get; set; }
        public string    AcademicYear           { get; set; }
        public int?      ClassId                { get; set; }
        public string    ClassName              { get; set; }
        public string    BranchName             { get; set; }
        public int?      SectionId              { get; set; }
        public string    SectionName            { get; set; }
        public string    RollNo                 { get; set; }
        public DateTime? DateOfJoining          { get; set; }
        public DateTime? AdmissionDate          { get; set; }
        public int?      FeeCategoryId          { get; set; }
        public string    JoiningClass           { get; set; }
        public string    JoinTerm               { get; set; }
        // Family
        public string    GuardianType           { get; set; }
        public string    FatherName             { get; set; }
        public string    FatherQualification    { get; set; }
        public string    FatherOccupation       { get; set; }
        public string    FatherEmploymentType   { get; set; }
        public string    FatherMobile           { get; set; }
        public string    MotherName             { get; set; }
        public string    MotherQualification    { get; set; }
        public string    MotherOccupation       { get; set; }
        public string    MotherMobile           { get; set; }
        public decimal?  AnnualIncome           { get; set; }
        public int?      FamilyChildrenCount    { get; set; }
        // Address
        public string    DoorNo                 { get; set; }
        public string    AddressArea            { get; set; }
        public string    AddressCity            { get; set; }
        public string    AddressState           { get; set; }
        public string    PermanentAddress       { get; set; }
        public string    Email                  { get; set; }
        // Identity & Docs
        public string    Caste                  { get; set; }
        public string    CasteCode              { get; set; }
        public string    Religion               { get; set; }
        public string    Nationality            { get; set; }
        public string    AadharNo               { get; set; }
        public string    UdiseNo                { get; set; }
        public string    DobProofSubmitted      { get; set; }
        public string    CasteCertSubmitted     { get; set; }
        public string    OtherCertificates      { get; set; }
        public string    DisabilityStatus       { get; set; }
        public string    DisabilityType         { get; set; }
        public string    BiometricId            { get; set; }
        // Transport
        public string    TransportType          { get; set; }
        public int?      BusRouteId             { get; set; }
        public int?      BusId                  { get; set; }
        public string    BusTrip                { get; set; }
        // Other
        public string    StudentType            { get; set; }
        public int?      HostelId               { get; set; }
        public string    HostelName             { get; set; }   // from JOIN on hostels
        public string    ScholarshipStatus      { get; set; }
        public string    ScholarshipDescription { get; set; }
        public string    MotherTongue           { get; set; }
        public string    FirstLanguage          { get; set; }
        public string    SecondLanguage         { get; set; }
        public string    ThirdLanguage          { get; set; }
        public string    ReferenceName          { get; set; }
        public string    Remarks                { get; set; }
        public string    SpareField1            { get; set; }
        public string    SpareField2            { get; set; }
        public int       SchoolId               { get; set; }
        public string    CreatedBy              { get; set; }
        public System.DateTime? CreatedAt       { get; set; }
        public string    UpdatedBy              { get; set; }
        public System.DateTime? UpdatedAt       { get; set; }
    }

    // One snapshot from the students temporal history (Phase 79).
    public class StudentHistoryDto
    {
        public System.DateTime  ValidFrom    { get; set; }  // when this version became current (UTC)
        public System.DateTime  ValidTo      { get; set; }  // when superseded (max for the live row)
        public string    CreatedBy    { get; set; }
        public string    UpdatedBy    { get; set; }
        public System.DateTime? UpdatedAt { get; set; }
        public string    AdmissionNo  { get; set; }
        public string    StudentName  { get; set; }
        public string    Gender       { get; set; }
        public System.DateTime? DateOfBirth { get; set; }
        public string    BloodGroup   { get; set; }
        public string    Status       { get; set; }
        public string    AcademicYear { get; set; }
        public string    ClassName    { get; set; }
        public string    SectionName  { get; set; }
        public string    RollNo       { get; set; }
        public string    FatherName   { get; set; }
        public string    FatherMobile { get; set; }
        public string    MotherName   { get; set; }
        public string    MotherMobile { get; set; }
        public string    Email        { get; set; }
        public string    AadharNo     { get; set; }
    }

    public class SavePhotoUrlRequest
    {
        public string PhotoUrl { get; set; }   // R2 public URL of the uploaded photo
    }

    public class SaveStudentRequest
    {
        // Basic
        public string    AdmissionNo            { get; set; }
        public string    SchoolUniqueId         { get; set; }
        public string    StudentName            { get; set; }
        public string    ShortName              { get; set; }
        public string    JoinType               { get; set; }
        public string    Gender                 { get; set; }
        public DateTime? DateOfBirth            { get; set; }
        public string    BloodGroup             { get; set; }
        public string    Status                 { get; set; }
        // Academic
        public int?      AcademicYearId         { get; set; }
        public int?      ClassId                { get; set; }
        public string    BranchName             { get; set; }
        public int?      SectionId              { get; set; }
        public string    RollNo                 { get; set; }
        public DateTime? DateOfJoining          { get; set; }
        public DateTime? AdmissionDate          { get; set; }
        public int?      FeeCategoryId          { get; set; }
        public string    JoiningClass           { get; set; }
        public string    JoinTerm               { get; set; }
        // Family
        public string    GuardianType           { get; set; }
        public string    FatherName             { get; set; }
        public string    FatherQualification    { get; set; }
        public string    FatherOccupation       { get; set; }
        public string    FatherEmploymentType   { get; set; }
        public string    FatherMobile           { get; set; }
        public string    MotherName             { get; set; }
        public string    MotherQualification    { get; set; }
        public string    MotherOccupation       { get; set; }
        public string    MotherMobile           { get; set; }
        public decimal?  AnnualIncome           { get; set; }
        public int?      FamilyChildrenCount    { get; set; }
        // Address
        public string    DoorNo                 { get; set; }
        public string    AddressArea            { get; set; }
        public string    AddressCity            { get; set; }
        public string    AddressState           { get; set; }
        public string    PermanentAddress       { get; set; }
        public string    Email                  { get; set; }
        // Identity & Docs
        public string    Caste                  { get; set; }
        public string    CasteCode              { get; set; }
        public string    Religion               { get; set; }
        public string    Nationality            { get; set; }
        public string    AadharNo               { get; set; }
        public string    UdiseNo                { get; set; }
        public string    DobProofSubmitted      { get; set; }
        public string    CasteCertSubmitted     { get; set; }
        public string    OtherCertificates      { get; set; }
        public string    DisabilityStatus       { get; set; }
        public string    DisabilityType         { get; set; }
        public string    BiometricId            { get; set; }
        // Transport
        public string    TransportType          { get; set; }
        public int?      BusRouteId             { get; set; }
        public int?      BusId                  { get; set; }
        public string    BusTrip                { get; set; }
        // Other
        public string    StudentType            { get; set; }
        public int?      HostelId               { get; set; }
        public string    ScholarshipStatus      { get; set; }
        public string    ScholarshipDescription { get; set; }
        public string    MotherTongue           { get; set; }
        public string    FirstLanguage          { get; set; }
        public string    SecondLanguage         { get; set; }
        public string    ThirdLanguage          { get; set; }
        public string    ReferenceName          { get; set; }
        public string    Remarks                { get; set; }
        public string    SpareField1            { get; set; }
        public string    SpareField2            { get; set; }
    }

    public class ChangeSectionRequest
    {
        public int SectionId { get; set; }
    }

    // ── Student 360 (GET school/students/{id}/overview) ───────────────────
    // Everything below is for ONE academic year of the student (the selected year),
    // except Years, Receipts and Sms which span every year (linked by student_unique_id).
    public class StudentOverviewDto
    {
        public long    StudentId        { get; set; }  // the selected year's student row
        public int?    StudentUniqueId  { get; set; }
        public int?    AcademicYearId   { get; set; }
        public string  AcademicYear     { get; set; }
        public bool    IsDetained       { get; set; }
        public string  DetainedReason   { get; set; }
        public string  BlockedReason    { get; set; }
        public string  RouteName        { get; set; }
        public string  BusName          { get; set; }
        public bool    HasMobileApp     { get; set; }  // a parent_children link exists (master DB)
        public System.DateTime? AppLinkedOn { get; set; }
        public List<StudentEnrollmentDto>      Years          { get; set; }
        public List<StudentAttendanceMonthDto> Attendance     { get; set; }
        public List<StudentAbsenceDto>         RecentAbsences { get; set; }
        public List<StudentExamSummaryDto>     Exams          { get; set; }
        public List<StudentReceiptSummaryDto>  Receipts       { get; set; }
        public List<StudentSmsDto>             Sms            { get; set; }
    }

    public class StudentEnrollmentDto
    {
        public long    StudentId      { get; set; }
        public int?    AcademicYearId { get; set; }
        public string  AcademicYear   { get; set; }
        public string  ClassName      { get; set; }
        public string  SectionName    { get; set; }
        public string  RollNo         { get; set; }
        public string  Status         { get; set; }
        public bool    IsDetained     { get; set; }
    }

    public class StudentAttendanceMonthDto
    {
        public int     Year        { get; set; }
        public int     Month       { get; set; }
        public int     WorkingDays { get; set; }  // days marked for this student, Holiday excluded
        public int     Present     { get; set; }
        public int     Absent      { get; set; }
        public int     Late        { get; set; }
        public int     HalfDay     { get; set; }
    }

    public class StudentAbsenceDto
    {
        public System.DateTime AttendanceDate { get; set; }
        public string  Status  { get; set; }
        public string  Remarks { get; set; }
    }

    public class StudentExamSummaryDto
    {
        public int     ExamTypeId      { get; set; }
        public string  ExamTypeName    { get; set; }
        public decimal Total           { get; set; }  // marks + activity; grade-only subjects excluded
        public decimal MaxTotal        { get; set; }
        public int     SubjectCount    { get; set; }
        public int     AbsentSubjects  { get; set; }
        public int     Rank            { get; set; }  // competition rank within class + section
        public int     RankOutOf       { get; set; }
    }

    public class StudentReceiptSummaryDto
    {
        public int     ReceiptId    { get; set; }
        public string  ReceiptNo    { get; set; }
        public System.DateTime PaymentDate { get; set; }
        public decimal TotalAmount  { get; set; }
        public string  PaymentMode  { get; set; }
        public string  Status       { get; set; }
        public string  AcademicYear { get; set; }
        public string  CreatedBy    { get; set; }
    }

    public class StudentSmsDto
    {
        public System.DateTime SentAt { get; set; }
        public string  SmsType  { get; set; }
        public string  Mobile   { get; set; }
        public string  Message  { get; set; }
        public string  Status   { get; set; }
        public string  SentBy   { get; set; }
    }
}
