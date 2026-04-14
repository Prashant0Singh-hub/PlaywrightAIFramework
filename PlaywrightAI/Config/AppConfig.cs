using System.Text.Json;

namespace PlaywrightAI.Config
{
    public sealed class AppConfig
    {
        private static AppConfig? _instance;
        public static AppConfig Instance => _instance ??= Load();

        public string BaseUrl      { get; private set; } = "https://www.flipkart.com";
        public bool   Headless     { get; private set; } = false;
        public int    SlowMo       { get; private set; } = 30;
        public int    Timeout      { get; private set; } = 30000;
        public string ApiKey       { get; private set; } = "";
        public string Model        { get; private set; } = "claude-opus-4-5";
        public string ReportsPath  { get; private set; } = "Reports";
        public string SessionPath  { get; private set; } = "Session";
        public bool   AutoLogin    { get; private set; } = true;
        public bool   Screenshots  { get; private set; } = true;

        public bool HasApiKey => !string.IsNullOrEmpty(ApiKey) && !ApiKey.Contains("YOUR_");

        private static AppConfig Load()
        {
            var cfg = new AppConfig();
            var searchPaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Config", "appsettings.json"),
                Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Config", "appsettings.json"),
            };

            var path = searchPaths.FirstOrDefault(System.IO.File.Exists);
            if (path == null) { Log($"appsettings.json not found, using defaults"); return cfg; }

            Log($"Config loaded from: {path}");
            try
            {
                var json = System.IO.File.ReadAllText(path);
                var doc  = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("Playwright", out var pw))
                {
                    if (pw.TryGetProperty("BaseUrl",  out var v)) cfg.BaseUrl  = v.GetString() ?? cfg.BaseUrl;
                    if (pw.TryGetProperty("Headless", out v))     cfg.Headless = v.GetBoolean();
                    if (pw.TryGetProperty("SlowMo",   out v))     cfg.SlowMo   = v.GetInt32();
                    if (pw.TryGetProperty("Timeout",  out v))     cfg.Timeout  = v.GetInt32();
                }
                if (root.TryGetProperty("Claude", out var cl))
                {
                    if (cl.TryGetProperty("ApiKey", out var v)) cfg.ApiKey = v.GetString() ?? "";
                    if (cl.TryGetProperty("Model",  out v))     cfg.Model  = v.GetString() ?? cfg.Model;
                }
                if (root.TryGetProperty("Reporting", out var rp))
                {
                    if (rp.TryGetProperty("OutputPath",          out var v)) cfg.ReportsPath = v.GetString() ?? cfg.ReportsPath;
                    if (rp.TryGetProperty("ScreenshotsOnFailure", out v))    cfg.Screenshots = v.GetBoolean();
                }
                if (root.TryGetProperty("Session", out var ss))
                {
                    if (ss.TryGetProperty("SavePath",  out var v)) cfg.SessionPath = v.GetString() ?? cfg.SessionPath;
                    if (ss.TryGetProperty("AutoLogin", out v))     cfg.AutoLogin   = v.GetBoolean();
                }
            }
            catch (Exception ex) { Log($"Config parse error: {ex.Message}"); }
            return cfg;
        }

        private static void Log(string msg) => System.Console.WriteLine($"[Config] {msg}");
    }
}
