using Microsoft.Playwright;
using PlaywrightAI.Config;

namespace PlaywrightAI.Core
{
    public class BrowserFactory
    {
        private IPlaywright? _pw;
        private IBrowser?     _browser;
        private IBrowserContext? _context;
        private IPage?        _page;

        public IPage Page => _page ?? throw new InvalidOperationException("Browser not initialized. Call InitAsync first.");

        // ── Unified session path ──────────────────────────────────────────────
        // Both TC01 (save) and TC02 (load) MUST use the exact same path.
        // We walk up from the output directory to find the .csproj root.
        public static string SessionFile
        {
            get
            {
                var dir = AppContext.BaseDirectory;
                for (int i = 0; i < 10; i++)
                {
                    if (Directory.GetFiles(dir, "*.csproj").Length > 0)
                        return Path.Combine(dir, "Session", "flipkart_storage.json");
                    var parent = Directory.GetParent(dir)?.FullName;
                    if (parent == null || parent == dir) break;
                    dir = parent;
                }
                // Fallback: output dir
                return Path.Combine(AppContext.BaseDirectory, "Session", "flipkart_storage.json");
            }
        }

        private static readonly string[] ChromePaths = new[]
        {
            @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
            Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe"),
        };

        public async Task InitAsync(bool withSession = true)
        {
            var cfg = AppConfig.Instance;
            _pw = await Playwright.CreateAsync();

            var launchOpts = new BrowserTypeLaunchOptions
            {
                Headless = cfg.Headless,
                SlowMo   = cfg.SlowMo,
                Args     = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--lang=en-IN,en",
                    "--disable-web-security",
                    "--allow-running-insecure-content"
                }
            };

            var chromePath = ChromePaths.FirstOrDefault(File.Exists);
            if (chromePath != null)
            {
                Console.WriteLine($"[Browser] Using Chrome: {chromePath}");
                launchOpts.ExecutablePath = chromePath;
            }
            else
            {
                Console.WriteLine("[Browser] Using Playwright Chromium");
            }

            _browser = await _pw.Chromium.LaunchAsync(launchOpts);

            // Context options
            var ctxOpts = new BrowserNewContextOptions
            {
                ViewportSize      = new ViewportSize { Width = 1440, Height = 900 },
                UserAgent         = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
                IgnoreHTTPSErrors = true,
                Locale            = "en-IN",
                TimezoneId        = "Asia/Kolkata",
            };

            // Load session if it exists and withSession is true
            var sf = SessionFile;
            if (withSession && File.Exists(sf) && new FileInfo(sf).Length > 10)
            {
                ctxOpts.StorageStatePath = sf;
                Console.WriteLine($"[Session] Loaded session: {sf}");
            }
            else
            {
                Console.WriteLine($"[Session] No session found — starting fresh");
            }

            _context = await _browser.NewContextAsync(ctxOpts);

            // Remove webdriver fingerprint
            await _context.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                window.chrome = { runtime: {} };
                Object.defineProperty(navigator, 'plugins', { get: () => [1,2,3,4,5] });
                Object.defineProperty(navigator, 'languages', { get: () => ['en-IN', 'en'] });
            ");

            _page = await _context.NewPageAsync();
            _page.SetDefaultTimeout(cfg.Timeout);
            _page.SetDefaultNavigationTimeout(120_000);

            Console.WriteLine("[Browser] Ready");
        }

        /// <summary>
        /// Saves the full browser storage state (cookies + localStorage) to SessionFile.
        /// Call this AFTER login is complete.
        /// </summary>
        public async Task SaveSessionAsync()
        {
            if (_context == null)
            {
                Console.WriteLine("[Session] ERROR: context is null, cannot save session");
                return;
            }
            var sf = SessionFile;
            var dir = Path.GetDirectoryName(sf)!;
            Directory.CreateDirectory(dir);

            // Wait a moment to ensure Flipkart has committed all cookies
            await Task.Delay(2000);

            await _context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = sf });

            var fi = new FileInfo(sf);
            Console.WriteLine($"[Session] ✅ Saved to: {sf} ({fi.Length} bytes)");

            if (fi.Length < 50)
                Console.WriteLine("[Session] ⚠️ WARNING: Session file is very small — login may not have completed");
        }

        public async Task DisposeAsync()
        {
            if (_context != null) { await _context.CloseAsync(); _context = null; }
            if (_browser != null) { await _browser.CloseAsync(); _browser = null; }
            _pw?.Dispose();
            _pw = null;
            _page = null;
            Console.WriteLine("[Browser] Disposed");
        }
    }
}
