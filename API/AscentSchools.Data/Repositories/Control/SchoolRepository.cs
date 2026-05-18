using AscentSchools.Core.DTOs.Control.Schools;
using AscentSchools.Data.ConnectionFactory;
using Dapper;
using System.Collections.Generic;

namespace AscentSchools.Data.Repositories.Control
{
    public class SchoolRepository
    {
        private readonly IConnectionFactory _db;
        public SchoolRepository(IConnectionFactory db) { _db = db; }

        public IEnumerable<SchoolDto> GetByGroupId(int groupId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.Query<SchoolDto>(
                    @"SELECT school_id SchoolId, group_id GroupId, school_name SchoolName,
                             school_caption SchoolCaption, address Address, city City,
                             state State, mobile Mobile, email Email, status Status, created_at CreatedAt
                      FROM schools WHERE group_id = @groupId ORDER BY school_id",
                    new { groupId });
        }

        public SchoolDto GetById(int schoolId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<SchoolDto>(
                    @"SELECT school_id SchoolId, group_id GroupId, school_name SchoolName,
                             school_caption SchoolCaption, address Address, city City,
                             state State, mobile Mobile, email Email, status Status, created_at CreatedAt
                      FROM schools WHERE school_id = @schoolId",
                    new { schoolId });
        }

        public int Create(int groupId, CreateSchoolRequest request)
        {
            using (var conn = _db.GetMasterConnection())
            {
                var schoolId = conn.QuerySingle<int>(
                    @"INSERT INTO schools (group_id, school_name, school_caption, address, city, state, mobile, email)
                      VALUES (@groupId, @SchoolName, @SchoolCaption, @Address, @City, @State, @Mobile, @Email);
                      SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    new { groupId, request.SchoolName, request.SchoolCaption, request.Address,
                          request.City, request.State, request.Mobile, request.Email });

                // Seed a school_settings row so API key and other per-branch config work immediately.
                conn.Execute(
                    @"IF NOT EXISTS (SELECT 1 FROM school_settings WHERE school_id = @schoolId)
                          INSERT INTO school_settings (school_id) VALUES (@schoolId)",
                    new { schoolId });

                return schoolId;
            }
        }

        public void Update(int schoolId, UpdateSchoolRequest request)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    @"UPDATE schools
                      SET school_name = @SchoolName, school_caption = @SchoolCaption,
                          address = @Address, city = @City, state = @State,
                          mobile = @Mobile, email = @Email, status = @Status
                      WHERE school_id = @schoolId",
                    new { request.SchoolName, request.SchoolCaption, request.Address,
                          request.City, request.State, request.Mobile, request.Email,
                          request.Status, schoolId });
        }

        public string GetApiKey(int schoolId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<string>(
                    "SELECT integration_api_key FROM school_settings WHERE school_id = @schoolId",
                    new { schoolId });
        }

        public string GenerateApiKey(int schoolId)
        {
            using (var conn = _db.GetMasterConnection())
            {
                // Ensure settings row exists, then set a fresh NEWID()-based key.
                conn.Execute(
                    @"IF EXISTS (SELECT 1 FROM school_settings WHERE school_id = @schoolId)
                          UPDATE school_settings
                          SET    integration_api_key = LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
                          WHERE  school_id = @schoolId
                      ELSE
                          INSERT INTO school_settings (school_id, integration_api_key)
                          VALUES (@schoolId, LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '')))",
                    new { schoolId });

                return conn.QuerySingle<string>(
                    "SELECT integration_api_key FROM school_settings WHERE school_id = @schoolId",
                    new { schoolId });
            }
        }
    }
}
