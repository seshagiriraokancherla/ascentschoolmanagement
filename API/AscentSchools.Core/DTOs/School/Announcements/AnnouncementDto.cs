using System;

namespace AscentSchools.Core.DTOs.School.Announcements
{
    public class AnnouncementDto
    {
        public int      AnnouncementId { get; set; }
        public string   Title          { get; set; }
        public string   Description    { get; set; }
        public string   Scope          { get; set; }   // School / Class
        public string   AttachmentUrl  { get; set; }   // optional PDF/doc URL
        public int?     ClassId        { get; set; }
        public string   ClassName      { get; set; }
        public int?     SectionId      { get; set; }   // optional; NULL = whole class
        public string   SectionName    { get; set; }
        public bool     IsPinned       { get; set; }
        public string   Status         { get; set; }
        public string   CreatedBy      { get; set; }
        public DateTime CreatedAt      { get; set; }
    }

    public class SaveAnnouncementRequest
    {
        public string Title       { get; set; }
        public string Description   { get; set; }
        public string Scope         { get; set; }  // School / Class
        public string AttachmentUrl { get; set; }
        public int?   ClassId     { get; set; }
        public int?   SectionId   { get; set; }  // optional; NULL = whole class
        public bool   IsPinned    { get; set; }
    }
}
