using System;

namespace AscentSchools.Core.DTOs.Control.SchoolGroups
{
    public class SchoolGroupDto
    {
        public int      GroupId     { get; set; }
        public string   GroupName   { get; set; }
        public string   Subdomain   { get; set; }
        public string   DbName      { get; set; }
        public string   DbUsername  { get; set; }
        public string   DbPassword  { get; set; }
        public string   Description { get; set; }
        public string   Status      { get; set; }
        public DateTime CreatedAt   { get; set; }
    }
}
