using System;
using System.Collections.Generic;
using AscentSchools.Core.DTOs.School.Media;

namespace AscentSchools.Core.DTOs.Mobile.Data
{
    public class HomeworkDto
    {
        public int      HomeworkId    { get; set; }
        public string   Title         { get; set; }
        public string   Description   { get; set; }
        public string   SubjectName   { get; set; }
        public DateTime  AssignedDate  { get; set; }
        public DateTime? DueDate       { get; set; }   // retired — null for new homework
        public string   AttachmentUrl { get; set; }
        public IEnumerable<AttachmentDto> Attachments { get; set; }
        public IEnumerable<MediaUploadDto> Media { get; set; }   // R2 uploads (Phase B)
    }

    public class AttachmentDto
    {
        public int    AttachmentId { get; set; }
        public string FileName     { get; set; }
        public string FilePath     { get; set; }
        public int?   FileSizeKb   { get; set; }
    }
}
