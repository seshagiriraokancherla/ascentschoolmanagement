using System;
using System.Collections.Generic;
using AscentSchools.Core.DTOs.School.Media;

namespace AscentSchools.Core.DTOs.Mobile.Data
{
    public class AnnouncementDto
    {
        public int      AnnouncementId { get; set; }
        public string   Title          { get; set; }
        public string   Description    { get; set; }
        public string   Scope          { get; set; }   // School / Class
        public bool     IsPinned       { get; set; }
        public DateTime CreatedAt      { get; set; }
        public IEnumerable<MediaUploadDto> Media { get; set; }   // R2 uploads (Phase B)
    }
}
