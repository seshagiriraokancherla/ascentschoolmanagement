using System;
using System.Configuration;

namespace AscentSyncTool.Config
{
    /// <summary>Reads connection string + API settings from App.config.</summary>
    public static class AppSettings
    {
        public static string LegacyConnectionString
        {
            get
            {
                var cs = ConfigurationManager.ConnectionStrings["LegacyDb"];
                if (cs == null) throw new Exception("App.config: missing connectionString 'LegacyDb'.");
                return cs.ConnectionString;
            }
        }

        public static string ApiBaseUrl
        {
            get
            {
                var url = ConfigurationManager.AppSettings["ApiBaseUrl"];
                if (string.IsNullOrWhiteSpace(url)) throw new Exception("App.config: missing appSetting 'ApiBaseUrl'.");
                return url.EndsWith("/") ? url : url + "/";
            }
        }

        public static string ApiKey
        {
            get
            {
                var key = ConfigurationManager.AppSettings["ApiKey"];
                if (string.IsNullOrWhiteSpace(key) || key == "PASTE-YOUR-KEY-HERE")
                    throw new Exception("App.config: 'ApiKey' is not set. Paste the school's integration_api_key.");
                return key.Trim();
            }
        }

        public static int ReceiptBatchSize =>
            int.TryParse(ConfigurationManager.AppSettings["ReceiptBatchSize"], out var n) && n > 0 ? n : 200;

        public static int StudentBatchSize =>
            int.TryParse(ConfigurationManager.AppSettings["StudentBatchSize"], out var n) && n > 0 ? n : 100;
    }
}
