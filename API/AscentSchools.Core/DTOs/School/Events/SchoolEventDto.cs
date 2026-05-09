using System;

namespace AscentSchools.Core.DTOs.School.Events
{
    public class SchoolEventDto
    {
        public int      EventId      { get; set; }
        public string   Title        { get; set; }
        public string   Description  { get; set; }
        public string   EventDate    { get; set; }   // ISO date string — avoids timezone shifts
        public string   MediaType    { get; set; }   // "image" | "video"
        public string   MediaUrl     { get; set; }
        public string   ThumbnailUrl   { get; set; }
        public string   AttachmentUrl  { get; set; }   // optional PDF/doc URL
        public string   Scope          { get; set; }   // "School" | "Class"
        public int?     ClassId      { get; set; }
        public string   ClassName    { get; set; }
        public bool     IsPinned     { get; set; }
        public string   CreatedBy    { get; set; }
        public DateTime CreatedAt    { get; set; }
    }

    public class SaveSchoolEventRequest
    {
        public string Title        { get; set; }
        public string Description  { get; set; }
        public string EventDate    { get; set; }
        public string MediaType    { get; set; }
        public string MediaUrl     { get; set; }
        public string ThumbnailUrl  { get; set; }
        public string AttachmentUrl { get; set; }
        public string Scope         { get; set; }
        public int?   ClassId      { get; set; }
        public bool   IsPinned     { get; set; }
    }
}
