using AscentSchools.Core.DTOs.School.Media;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;
using System.Linq;

namespace AscentSchools.Data.Repositories.School
{
    /// <summary>R2-uploaded files for homework / announcements / events (media_uploads).</summary>
    public class MediaUploadsRepository
    {
        private readonly IConnectionFactory _db;
        public MediaUploadsRepository(IConnectionFactory db) { _db = db; }

        public MediaUploadDto Insert(string tenantDbName, int schoolId, AttachMediaRequest r, string createdBy)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
            {
                var id = conn.QuerySingle<long>(
                    @"INSERT INTO media_uploads
                          (entity_type, entity_id, file_name, file_url, file_type, file_size_kb, school_id, created_by)
                      VALUES
                          (@EntityType, @EntityId, @FileName, @FileUrl, @FileType, @FileSizeKb, @schoolId, @createdBy);
                      SELECT CAST(SCOPE_IDENTITY() AS BIGINT)",
                    new { r.EntityType, r.EntityId, r.FileName, r.FileUrl, r.FileType, r.FileSizeKb, schoolId, createdBy });

                return new MediaUploadDto
                {
                    UploadId = id, EntityType = r.EntityType, EntityId = r.EntityId,
                    FileName = r.FileName, FileUrl = r.FileUrl, FileType = r.FileType, FileSizeKb = r.FileSizeKb
                };
            }
        }

        public IEnumerable<MediaUploadDto> GetByEntity(string tenantDbName, int schoolId, string entityType, long entityId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<MediaUploadDto>(
                    @"SELECT upload_id UploadId, entity_type EntityType, entity_id EntityId,
                             file_name FileName, file_url FileUrl, file_type FileType, file_size_kb FileSizeKb
                      FROM media_uploads
                      WHERE entity_type = @entityType AND entity_id = @entityId AND school_id = @schoolId
                      ORDER BY upload_id",
                    new { entityType, entityId, schoolId });
        }

        // For mobile list screens — all attachments for a set of entity ids.
        public IEnumerable<MediaUploadDto> GetForEntities(string tenantDbName, string entityType, IEnumerable<long> ids)
        {
            var list = ids?.ToList() ?? new List<long>();
            if (list.Count == 0) return Enumerable.Empty<MediaUploadDto>();
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.Query<MediaUploadDto>(
                    @"SELECT upload_id UploadId, entity_type EntityType, entity_id EntityId,
                             file_name FileName, file_url FileUrl, file_type FileType, file_size_kb FileSizeKb
                      FROM media_uploads
                      WHERE entity_type = @entityType AND entity_id IN @ids
                      ORDER BY upload_id",
                    new { entityType, ids = list });
        }

        public void Delete(string tenantDbName, int schoolId, long uploadId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                conn.Execute(
                    "DELETE FROM media_uploads WHERE upload_id = @uploadId AND school_id = @schoolId",
                    new { uploadId, schoolId });
        }

        public int CountByEntity(string tenantDbName, int schoolId, string entityType, long entityId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM media_uploads WHERE entity_type=@entityType AND entity_id=@entityId AND school_id=@schoolId",
                    new { entityType, entityId, schoolId });
        }

        // Current active academic year string — used to build homework's year-wise R2 folder.
        public string GetCurrentAcademicYear(string tenantDbName, int schoolId)
        {
            using (var conn = _db.GetTenantConnection(tenantDbName))
                return conn.ExecuteScalar<string>(
                    @"SELECT TOP 1 academic_year FROM academic_years
                      WHERE school_id = @schoolId AND status = 'Active'
                      ORDER BY academic_year_id DESC",
                    new { schoolId });
        }
    }
}
