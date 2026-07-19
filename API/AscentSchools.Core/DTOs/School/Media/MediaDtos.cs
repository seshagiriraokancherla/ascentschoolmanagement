using System.Collections.Generic;

namespace AscentSchools.Core.DTOs.School.Media
{
    // A file uploaded to R2 for a homework / announcement / event.
    public class MediaUploadDto
    {
        public long   UploadId   { get; set; }
        public string EntityType { get; set; }   // homework | announcement | event
        public long   EntityId   { get; set; }
        public string FileName   { get; set; }
        public string FileUrl    { get; set; }    // R2 public URL
        public string FileType   { get; set; }    // image | doc | audio | video
        public int?   FileSizeKb { get; set; }
    }

    // Saved after a presigned upload completes.
    public class AttachMediaRequest
    {
        public string EntityType { get; set; }
        public long   EntityId   { get; set; }
        public string FileName   { get; set; }
        public string FileUrl    { get; set; }
        public string FileType   { get; set; }
        public int?   FileSizeKb { get; set; }
    }
}
