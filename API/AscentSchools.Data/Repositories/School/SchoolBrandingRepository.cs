using AscentSchools.Core.DTOs.Branding;
using AscentSchools.Data.ConnectionFactory;
using Dapper;

namespace AscentSchools.Data.Repositories.School
{
    public class SchoolBrandingRepository
    {
        private readonly IConnectionFactory _db;
        public SchoolBrandingRepository(IConnectionFactory db) { _db = db; }

        /// <summary>
        /// Returns branding for a subdomain.
        /// If schoolId is supplied, school_branding values override group_branding (column-by-column).
        /// Falls back to group_branding, then school name as display name if nothing is set.
        /// Returns null if subdomain is not found or inactive.
        /// </summary>
        public BrandingResponse GetBySubdomain(string subdomain, int? schoolId = null)
        {
            using (var conn = _db.GetMasterConnection())
            {
                if (schoolId.HasValue)
                {
                    return conn.QueryFirstOrDefault<BrandingResponse>(
                        @"SELECT
                              ISNULL(sb.display_name,        ISNULL(gb.display_name,        sg.group_name)) DisplayName,
                              ISNULL(sb.tagline,             gb.tagline)             Tagline,
                              ISNULL(sb.logo_path,           gb.logo_path)           LogoPath,
                              ISNULL(sb.favicon_path,        gb.favicon_path)        FaviconPath,
                              ISNULL(sb.primary_color,       gb.primary_color)       PrimaryColor,
                              ISNULL(sb.secondary_color,     gb.secondary_color)     SecondaryColor,
                              ISNULL(sb.header_bg_color,     gb.header_bg_color)     HeaderBgColor,
                              ISNULL(sb.nav_text_color,      gb.nav_text_color)      NavTextColor,
                              ISNULL(sb.login_bg_path,       gb.login_bg_path)       LoginBgPath,
                              ISNULL(sb.receipt_footer_text, gb.receipt_footer_text) ReceiptFooterText
                          FROM school_groups sg
                          LEFT JOIN group_branding  gb ON gb.group_id  = sg.group_id
                          LEFT JOIN school_branding sb ON sb.school_id = @schoolId
                          WHERE sg.subdomain = @subdomain AND sg.status = 'Active'",
                        new { subdomain, schoolId });
                }

                return conn.QueryFirstOrDefault<BrandingResponse>(
                    @"SELECT
                          ISNULL(gb.display_name,        sg.group_name) DisplayName,
                          gb.tagline             Tagline,
                          gb.logo_path           LogoPath,
                          gb.favicon_path        FaviconPath,
                          gb.primary_color       PrimaryColor,
                          gb.secondary_color     SecondaryColor,
                          gb.header_bg_color     HeaderBgColor,
                          gb.nav_text_color      NavTextColor,
                          gb.login_bg_path       LoginBgPath,
                          gb.receipt_footer_text ReceiptFooterText
                      FROM school_groups sg
                      LEFT JOIN group_branding gb ON gb.group_id = sg.group_id
                      WHERE sg.subdomain = @subdomain AND sg.status = 'Active'",
                    new { subdomain });
            }
        }
    }
}
