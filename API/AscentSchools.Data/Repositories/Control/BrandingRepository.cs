using AscentSchools.Core.DTOs.Control.Branding;
using AscentSchools.Data.ConnectionFactory;
using Dapper;

namespace AscentSchools.Data.Repositories.Control
{
    public class BrandingRepository
    {
        private readonly IConnectionFactory _db;
        public BrandingRepository(IConnectionFactory db) { _db = db; }

        public GroupBrandingDto GetGroupBranding(int groupId)
        {
            using (var conn = _db.GetMasterConnection())
                return conn.QueryFirstOrDefault<GroupBrandingDto>(
                    @"SELECT group_id GroupId, logo_path LogoPath, favicon_path FaviconPath,
                             primary_color PrimaryColor, secondary_color SecondaryColor,
                             header_bg_color HeaderBgColor, nav_text_color NavTextColor,
                             display_name DisplayName, tagline Tagline,
                             receipt_footer_text ReceiptFooterText, login_bg_path LoginBgPath
                      FROM group_branding WHERE group_id = @groupId",
                    new { groupId });
        }

        public void UpsertGroupBranding(int groupId, UpdateGroupBrandingRequest r)
        {
            using (var conn = _db.GetMasterConnection())
                conn.Execute(
                    @"IF EXISTS (SELECT 1 FROM group_branding WHERE group_id = @groupId)
                          UPDATE group_branding
                          SET logo_path = @LogoPath, favicon_path = @FaviconPath,
                              primary_color = @PrimaryColor, secondary_color = @SecondaryColor,
                              header_bg_color = @HeaderBgColor, nav_text_color = @NavTextColor,
                              display_name = @DisplayName, tagline = @Tagline,
                              receipt_footer_text = @ReceiptFooterText, login_bg_path = @LoginBgPath,
                              updated_at = GETDATE()
                          WHERE group_id = @groupId
                      ELSE
                          INSERT INTO group_branding
                              (group_id, logo_path, favicon_path, primary_color, secondary_color,
                               header_bg_color, nav_text_color, display_name, tagline, receipt_footer_text, login_bg_path)
                          VALUES
                              (@groupId, @LogoPath, @FaviconPath, @PrimaryColor, @SecondaryColor,
                               @HeaderBgColor, @NavTextColor, @DisplayName, @Tagline, @ReceiptFooterText, @LoginBgPath)",
                    new { groupId, r.LogoPath, r.FaviconPath, r.PrimaryColor, r.SecondaryColor,
                          r.HeaderBgColor, r.NavTextColor, r.DisplayName, r.Tagline,
                          r.ReceiptFooterText, r.LoginBgPath });
        }
    }
}
