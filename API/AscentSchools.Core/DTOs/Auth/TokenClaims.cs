namespace AscentSchools.Core.DTOs.Auth
{
    public class TokenClaims
    {
        public int     UserId       { get; set; }
        public string  FullName     { get; set; }
        public int     GroupId      { get; set; }
        public int?    SchoolId     { get; set; }
        public string  TenantDbName { get; set; }
        public string[] Permissions { get; set; }
    }
}
