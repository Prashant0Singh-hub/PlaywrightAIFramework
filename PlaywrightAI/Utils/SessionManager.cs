using Microsoft.Playwright;
using System.Text.Json;
using PlaywrightAI.Config;

namespace PlaywrightAI.Utils
{
    /// <summary>
    /// Saves login session to disk so OTP login only happens ONCE.
    /// Every subsequent test run restores the session automatically.
    /// </summary>
    public static class SessionManager
    {
        private static string CookieFile => Path.Combine(AppConfig.Instance.SessionPath, "session_cookies.json");
        public  static bool   HasSession  => File.Exists(CookieFile);

        public static async Task RestoreAsync(IBrowserContext ctx)
        {
            if (!HasSession) return;
            try
            {
                var json    = await File.ReadAllTextAsync(CookieFile);
                var cookies = JsonSerializer.Deserialize<List<Cookie>>(json);
                if (cookies?.Count > 0)
                {
                    await ctx.AddCookiesAsync(cookies);
                    System.Console.WriteLine($"[Session] ✅ Restored {cookies.Count} cookies — no login needed");
                }
            }
            catch (Exception ex) { System.Console.WriteLine($"[Session] Restore warning: {ex.Message}"); }
        }

        public static async Task SaveAsync(IBrowserContext ctx)
        {
            try
            {
                Directory.CreateDirectory(AppConfig.Instance.SessionPath);
                var cookies = await ctx.CookiesAsync();
                var json    = JsonSerializer.Serialize(cookies, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(CookieFile, json);
                System.Console.WriteLine($"[Session] ✅ Session saved ({cookies.Count} cookies) — login won't be needed next time");
            }
            catch (Exception ex) { System.Console.WriteLine($"[Session] Save warning: {ex.Message}"); }
        }

        /// <summary>
        /// Opens login page and waits for user to complete phone+OTP login manually.
        /// After success, saves session automatically.
        /// </summary>
        public static async Task<bool> DoLoginAsync(IPage page, IBrowserContext ctx)
        {
            System.Console.WriteLine("\n[Session] ════════════════════════════════════════");
            System.Console.WriteLine("[Session] No saved session found.");
            System.Console.WriteLine("[Session] Opening Flipkart login page...");
            System.Console.WriteLine("[Session] Please:");
            System.Console.WriteLine("[Session]   1. Enter your phone number");
            System.Console.WriteLine("[Session]   2. Click REQUEST OTP");
            System.Console.WriteLine("[Session]   3. Enter the OTP");
            System.Console.WriteLine("[Session]   4. Click VERIFY");
            System.Console.WriteLine("[Session] Session will be saved automatically after login.");
            System.Console.WriteLine("[Session] ════════════════════════════════════════\n");

            try
            {
                await page.GotoAsync("https://www.flipkart.com/account/login",
                    new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 20000 });

                // Wait up to 3 minutes for user to complete login
                var deadline = DateTime.Now.AddMinutes(3);
                while (DateTime.Now < deadline)
                {
                    await Task.Delay(2000);
                    var url = page.Url;
                    if (!url.Contains("/account/login"))
                    {
                        System.Console.WriteLine("[Session] ✅ Login detected!");
                        await Task.Delay(2000); // let cookies settle
                        await SaveAsync(ctx);
                        return true;
                    }
                }
                System.Console.WriteLine("[Session] ⚠️ Login timed out after 3 minutes");
                return false;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[Session] Login error: {ex.Message}");
                return false;
            }
        }

        public static void Clear()
        {
            if (File.Exists(CookieFile)) File.Delete(CookieFile);
            System.Console.WriteLine("[Session] Session cleared — next run will require login");
        }
    }
}
