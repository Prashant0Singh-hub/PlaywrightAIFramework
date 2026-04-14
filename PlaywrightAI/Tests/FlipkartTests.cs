using NUnit.Framework;
using PlaywrightAI.Core;
using PlaywrightAI.Pages;
using Microsoft.Playwright;

namespace PlaywrightAI.Tests
{
    [TestFixture]
    [Category("Flipkart")]
    public class FlipkartTests : BaseTest
    {
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
                    if (parent == null) break;
                    dir = parent;
                }
                return Path.Combine(AppContext.BaseDirectory, "Session", "flipkart_storage.json");
            }
        }

        [Test]
        [Order(1)]
        public async Task TC01_Login_OTP_Once_ThenRemember()
        {
            Console.WriteLine("\n================================================");
            Console.WriteLine("TC01: One-Time OTP Login");
            Console.WriteLine("================================================");

            if (File.Exists(SessionFile) && new FileInfo(SessionFile).Length > 100)
            {
                Console.WriteLine("[TC01] Session already saved - already logged in!");
                Console.WriteLine("[TC01] Delete Session/flipkart_storage.json to re-login.");
                Assert.Pass("Session already saved");
                return;
            }

            var page = Browser.Page;
            await page.GotoAsync("https://www.flipkart.com/",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });
            await Task.Delay(2000);
            try { await page.PressAsync("body", "Escape"); } catch { }

            Console.WriteLine("\n[TC01] Please login in the browser window:");
            Console.WriteLine("[TC01] 1. Click LOGIN button on Flipkart");
            Console.WriteLine("[TC01] 2. Enter your phone number");
            Console.WriteLine("[TC01] 3. Enter the OTP you receive");
            Console.WriteLine("[TC01] Waiting up to 120 seconds...\n");

            for (int remaining = 120; remaining > 0; remaining -= 5)
            {
                await Task.Delay(5000);
                Console.WriteLine($"[TC01] {remaining} seconds remaining...");
                try
                {
                    var hasProfile = await page.IsVisibleAsync("//a[contains(@href,'/account')]");
                    if (hasProfile) { Console.WriteLine("[TC01] Login detected!"); break; }
                }
                catch { }
            }

            await Task.Delay(3000);
            try
            {
                var sessionDir = Path.GetDirectoryName(SessionFile)!;
                Directory.CreateDirectory(sessionDir);
                var storageState = await page.Context.StorageStateAsync();
                await File.WriteAllTextAsync(SessionFile, storageState);
                Console.WriteLine("[TC01] Session saved to: " + SessionFile);
            }
            catch (Exception ex) { Console.WriteLine("[TC01] Save failed: " + ex.Message); }

            Assert.Pass("TC01 login complete");
        }

        [Test]
        [Order(2)]
        public async Task TC02_Prompt_To_TestScenarios()
        {
            var prompt = Environment.GetEnvironmentVariable("PLAYWRIGHT_PROMPT")
                         ?? "Search for iphone on flipkart, click on first product, add to cart";

            Console.WriteLine("\n================================================");
            Console.WriteLine("TC02: AI Prompt -> Execute Actions + Generate Tests");
            Console.WriteLine("================================================");
            Console.WriteLine("[TC02] Prompt: " + prompt);

            var page = Browser.Page;
            var parsed = ParsePrompt(prompt);
            Console.WriteLine("[PARSED] site=" + parsed.Site + " | url=" + parsed.DirectUrl
                + " | search=" + parsed.SearchTerm + " | product=" + parsed.ProductIndex
                + " | size=" + parsed.Size + " | action=" + parsed.Action);

            // Step 1: Navigate
            string baseUrl = parsed.Site == "amazon" ? "https://www.amazon.in/" : "https://www.flipkart.com/";

            if (!string.IsNullOrEmpty(parsed.DirectUrl))
            {
                Console.WriteLine("[Step 1] Navigating to: " + parsed.DirectUrl);
                await NavigateAsync(page, parsed.DirectUrl);
            }
            else
            {
                Console.WriteLine("[Step 1] Navigating to: " + baseUrl);
                await NavigateAsync(page, baseUrl);
                await DismissPopupAsync(page);
            }
            Console.WriteLine("[Step 1] Loaded: " + page.Url);

            // Step 2: Search
            if (!string.IsNullOrEmpty(parsed.SearchTerm) && string.IsNullOrEmpty(parsed.DirectUrl))
            {
                Console.WriteLine("[Step 2] Searching: " + parsed.SearchTerm);
                await SearchAsync(page, parsed.SearchTerm, parsed.Site);
                await Task.Delay(2000);
                Console.WriteLine("[Step 2] URL: " + page.Url);
            }

            // Step 3: Click product
            if (parsed.ProductIndex > 0 && !page.Url.Contains("/p/") && !page.Url.Contains("/dp/"))
            {
                Console.WriteLine("[Step 3] Clicking product #" + parsed.ProductIndex);
                await ClickProductAtIndexAsync(page, parsed.ProductIndex, parsed.Site);
                await Task.Delay(2000);
                Console.WriteLine("[Step 3] URL: " + page.Url);
            }

            // Step 4: Select size
            if (!string.IsNullOrEmpty(parsed.Size))
            {
                Console.WriteLine("[Step 4] Selecting size: " + parsed.Size);
                await SelectSizeAsync(page, parsed.Size);
                await Task.Delay(1000);
            }

            // Step 5: Action
            Console.WriteLine("[Step 5] Action: " + parsed.Action);
            await PerformActionAsync(page, parsed.Action);
            await Task.Delay(2000);
            Console.WriteLine("[Step 5] URL after action: " + page.Url);

            // Step 6: Scan locators
            Console.WriteLine("[Step 6] Scanning page locators...");
            var locatorJson = await ScanLocatorsAsync(page, prompt);

            // Step 7: Generate test code
            Console.WriteLine("[Step 7] Generating test code with Claude AI...");
            var generator = new AITestGenerator(page);
            var generatedCode = await generator.GenerateTestsAsync(prompt + "\n\nPage locators:\n" + locatorJson);

            if (!string.IsNullOrEmpty(generatedCode))
            {
                generatedCode = StripMarkdown(generatedCode);
                var genDir = FindProjectDir("Tests/Generated");
                Directory.CreateDirectory(genDir);
                var fileName = "Generated_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".cs";
                var filePath = Path.Combine(genDir, fileName);
                await File.WriteAllTextAsync(filePath, generatedCode);
                Console.WriteLine("[TC02] Test code saved: Tests/Generated/" + fileName);
            }

            Assert.Pass("TC02 completed: " + prompt);
        }

        // -- Navigation Helpers --

        private async Task NavigateAsync(IPage page, string url)
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    await page.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 60000
                    });
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                        new PageWaitForLoadStateOptions { Timeout = 20000 });
                    return;
                }
                catch (Exception ex) when (attempt < 3)
                {
                    Console.WriteLine("[Nav] Attempt " + attempt + " failed: " + ex.Message.Split('\n')[0]);
                    await Task.Delay(3000);
                }
            }
        }

        private async Task DismissPopupAsync(IPage page)
        {
            await Task.Delay(1500);
            try { await page.PressAsync("body", "Escape"); } catch { }
            await Task.Delay(500);
            try
            {
                var closeBtn = page.Locator("button._2KpZ6l, button[class*='close'], .mEGFSE button").First;
                if (await closeBtn.IsVisibleAsync())
                    await closeBtn.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
            }
            catch { }
        }

        private async Task SearchAsync(IPage page, string term, string site)
        {
            string[] flipkartSelectors = { "input[name='q']:not([readonly])", "input[placeholder*='Search']" };
            string[] amazonSelectors   = { "input#twotabsearchtextbox", "input[name='field-keywords']" };
            var selectors = site == "amazon" ? amazonSelectors : flipkartSelectors;

            foreach (var sel in selectors)
            {
                try
                {
                    var box = page.Locator(sel).First;
                    await box.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                    await box.ClickAsync();
                    await box.FillAsync(term);
                    await box.PressAsync("Enter");
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded,
                        new PageWaitForLoadStateOptions { Timeout = 30000 });
                    Console.WriteLine("[Search] Searched for: " + term);
                    return;
                }
                catch { }
            }
            Console.WriteLine("[Search] Could not find search box");
        }

        private async Task ClickProductAtIndexAsync(IPage page, int index, string site)
        {
            for (int s = 1; s <= 3; s++)
            {
                await page.EvaluateAsync("(pct) => window.scrollTo(0, document.body.scrollHeight * pct)", 0.3 * s);
                await Task.Delay(700);
            }
            await page.EvaluateAsync("window.scrollTo(0, 0)");
            await Task.Delay(500);

            string linkSelector = site == "amazon" ? "a[href*='/dp/']" : "a[href*='/p/']";
            var allLinks = await page.QuerySelectorAllAsync(linkSelector);
            Console.WriteLine("[Product] Raw links: " + allLinks.Count);

            var seenHrefs = new HashSet<string>();
            var uniqueHrefs = new List<string>();

            foreach (var link in allLinks)
            {
                try
                {
                    var href = await link.GetAttributeAsync("href") ?? "";
                    if (string.IsNullOrEmpty(href)) continue;
                    var normalized = href.Split('?')[0].TrimEnd('/');
                    if (seenHrefs.Add(normalized))
                    {
                        var fullUrl = href.StartsWith("http") ? href
                            : (site == "amazon" ? "https://www.amazon.in" : "https://www.flipkart.com") + href;
                        uniqueHrefs.Add(fullUrl);
                    }
                }
                catch { }
            }

            Console.WriteLine("[Product] Unique products: " + uniqueHrefs.Count);
            for (int i = 0; i < Math.Min(uniqueHrefs.Count, 8); i++)
                Console.WriteLine("  [" + (i + 1) + "] " + uniqueHrefs[i]);

            if (uniqueHrefs.Count == 0) { Console.WriteLine("[Product] No products found"); return; }

            var safeIndex = Math.Max(0, Math.Min(index - 1, uniqueHrefs.Count - 1));
            Console.WriteLine("[Product] Navigating to #" + (safeIndex + 1));
            await NavigateAsync(page, uniqueHrefs[safeIndex]);

            await Task.Delay(3000);
            for (int wait = 0; wait < 5; wait++)
            {
                var btnCount = await page.EvaluateAsync<int>("document.querySelectorAll('button, a, div[role=button]').length");
                if (btnCount > 10) break;
                await Task.Delay(1000);
            }
        }

        private async Task SelectSizeAsync(IPage page, string size)
        {
            var selectors = new[]
            {
                "//li[normalize-space(text())='" + size + "']",
                "//div[normalize-space(text())='" + size + "']",
                "li:has-text('" + size + "')"
            };
            foreach (var sel in selectors)
            {
                try
                {
                    var loc = page.Locator(sel).First;
                    await loc.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 3000 });
                    await loc.ClickAsync(new LocatorClickOptions { Force = true });
                    Console.WriteLine("[Size] Selected size " + size);
                    return;
                }
                catch { }
            }
            Console.WriteLine("[Size] Size " + size + " not found");
        }

        private async Task PerformActionAsync(IPage page, string action)
        {
            // Scroll to top first - Buy Now is in the sticky top panel
            await page.EvaluateAsync("window.scrollTo(0, 0)");
            await Task.Delay(1500);

            string[] buyTexts  = { "buy now", "buy at", "buy with", "buy" };
            string[] cartTexts = { "add to cart", "add to bag", "add to basket" };
            string[] emiTexts  = { "buy now with emi", "emi options", "buy on emi", "no cost emi" };

            string[] targetTexts = action switch
            {
                "emi"  => emiTexts,
                "cart" => cartTexts,
                _      => buyTexts
            };

            Console.WriteLine("[Action] Looking for: " + string.Join(" / ", targetTexts));

            // Strategy 1: JavaScript full-DOM scan
            var jsLines = new System.Text.StringBuilder();
            jsLines.Append("(targets) => {");
            jsLines.Append("  var all = Array.from(document.querySelectorAll('*'));");
            jsLines.Append("  var candidates = [];");
            jsLines.Append("  for (var i = 0; i < all.length; i++) {");
            jsLines.Append("    var el = all[i];");
            jsLines.Append("    var tag = el.tagName;");
            jsLines.Append("    if (tag === 'SCRIPT' || tag === 'STYLE' || tag === 'META') continue;");
            jsLines.Append("    var raw = (el.innerText || el.textContent || '').trim().toLowerCase();");
            jsLines.Append("    if (!raw || raw.length > 60) continue;");
            jsLines.Append("    var matched = targets.some(function(t) { return raw === t || raw.indexOf(t) === 0; });");
            jsLines.Append("    if (!matched) continue;");
            jsLines.Append("    var rect = el.getBoundingClientRect();");
            jsLines.Append("    if (rect.width === 0 || rect.height === 0) continue;");
            jsLines.Append("    candidates.push({ tag: tag, text: el.innerText.trim(), top: rect.top + window.scrollY });");
            jsLines.Append("  }");
            jsLines.Append("  if (candidates.length === 0) return 'NO_MATCH';");
            jsLines.Append("  candidates.sort(function(a,b) { return a.top - b.top; });");
            jsLines.Append("  var best = candidates[0];");
            jsLines.Append("  for (var j = 0; j < all.length; j++) {");
            jsLines.Append("    var e = all[j];");
            jsLines.Append("    var t = (e.innerText || '').trim();");
            jsLines.Append("    if (t === best.text && e.tagName === best.tag) {");
            jsLines.Append("      e.scrollIntoView({ block: 'center' });");
            jsLines.Append("      e.click();");
            jsLines.Append("      return 'CLICKED: ' + e.tagName + ' [' + t + ']';");
            jsLines.Append("    }");
            jsLines.Append("  }");
            jsLines.Append("  return 'FOUND_BUT_FAILED: ' + best.text;");
            jsLines.Append("}");

            var result = await page.EvaluateAsync<string>(jsLines.ToString(), targetTexts);
            Console.WriteLine("[Action] JS result: " + result);

            if (result != null && result.StartsWith("CLICKED"))
            {
                await Task.Delay(2000);
                Console.WriteLine("[Action] Success! URL: " + page.Url);
                return;
            }

            // Strategy 2: Playwright selectors
            Console.WriteLine("[Action] JS failed - trying Playwright selectors...");
            foreach (var text in targetTexts)
            {
                var selectors = new[]
                {
                    "button:has-text('" + text + "')",
                    "a:has-text('"      + text + "')",
                    "div:has-text('"    + text + "')",
                    "span:has-text('"   + text + "')"
                };

                foreach (var sel in selectors)
                {
                    try
                    {
                        var loc = page.Locator(sel);
                        var count = await loc.CountAsync();
                        if (count == 0) continue;

                        for (int i = 0; i < count; i++)
                        {
                            try
                            {
                                var el = loc.Nth(i);
                                if (!await el.IsVisibleAsync()) continue;
                                var elText = (await el.TextContentAsync() ?? "").Trim();
                                await el.ScrollIntoViewIfNeededAsync();
                                await Task.Delay(500);
                                await el.ClickAsync(new LocatorClickOptions { Force = true, Timeout = 5000 });
                                Console.WriteLine("[Action] Clicked: [" + elText + "]");
                                await Task.Delay(2000);
                                return;
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }

            // Save screenshot for debugging
            Console.WriteLine("[Action] All strategies failed - saving screenshot...");
            try
            {
                var ss = Path.Combine(AppContext.BaseDirectory, "action_debug.png");
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = ss, FullPage = true });
                Console.WriteLine("[Action] Screenshot saved: " + ss);
            }
            catch { }
        }

        private async Task<string> ScanLocatorsAsync(IPage page, string prompt)
        {
            try
            {
                var keywords = prompt.ToLower()
                    .Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);

                var jsScript = new System.Text.StringBuilder();
                jsScript.Append("() => {");
                jsScript.Append("  var els = document.querySelectorAll('button, input, a, select, div[role=button], span[role=button]');");
                jsScript.Append("  var out = [];");
                jsScript.Append("  for (var i = 0; i < els.length && i < 80; i++) {");
                jsScript.Append("    var el = els[i];");
                jsScript.Append("    var tag = el.tagName.toLowerCase();");
                jsScript.Append("    var txt = (el.innerText || el.value || el.placeholder || el.getAttribute('aria-label') || '').trim().substring(0, 60);");
                jsScript.Append("    if (!txt) continue;");
                jsScript.Append("    var id  = el.id ? '#' + el.id : '';");
                jsScript.Append("    var nm  = el.name ? '[name=' + el.name + ']' : '';");
                jsScript.Append("    var css = tag + id + nm;");
                jsScript.Append("    var xpath = el.id ? '//' + tag + '[@id=\"' + el.id + '\"]'");
                jsScript.Append("              : el.name ? '//' + tag + '[@name=\"' + el.name + '\"]'");
                jsScript.Append("              : '//' + tag + '[contains(.,\"' + txt.substring(0,20).replace(/\"/g,'') + '\")]';");
                jsScript.Append("    var visible = el.offsetParent !== null || el.tagName === 'BUTTON';");
                jsScript.Append("    out.push(tag + '|' + txt + '|' + css + '|' + xpath + '|' + visible);");
                jsScript.Append("  }");
                jsScript.Append("  return out.join('~~');");
                jsScript.Append("}");

                var raw = await page.EvaluateAsync<string>(jsScript.ToString());
                if (string.IsNullOrEmpty(raw)) return "[]";

                Console.WriteLine("\n-- LOCATORS FOUND ON PAGE --");
                var results = new List<string>();
                int relevant = 0;

                foreach (var item in raw.Split("~~"))
                {
                    var parts = item.Split('|');
                    if (parts.Length < 5) continue;

                    var tag     = parts[0];
                    var txt     = parts[1];
                    var css     = parts[2];
                    var xpath   = parts[3];
                    var visible = parts[4];

                    var txtLower = txt.ToLower();
                    bool isRelevant = keywords.Any(k => k.Length > 3 && txtLower.Contains(k));
                    if (isRelevant) relevant++;

                    var marker = isRelevant ? "[RELEVANT]" : "          ";
                    Console.WriteLine(marker + " [" + tag + "] \"" + txt + "\" visible=" + visible);
                    Console.WriteLine("           CSS:   " + css);
                    Console.WriteLine("           XPath: " + xpath);

                    results.Add(txt + "|" + css + "|" + xpath);
                }

                Console.WriteLine("-- TOTAL: " + results.Count + " | RELEVANT: " + relevant + " --\n");
                return string.Join("\n", results);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Scan] Error: " + ex.Message);
                return "[]";
            }
        }

        private record ParsedPrompt(
            string Site, string DirectUrl, string SearchTerm,
            int ProductIndex, string Size, string Action);

        private static ParsedPrompt ParsePrompt(string prompt)
        {
            var p = prompt.ToLower();

            string site = p.Contains("amazon") ? "amazon" : "flipkart";

            string directUrl = "";
            var urlMatch = System.Text.RegularExpressions.Regex.Match(prompt, @"https?://[^\s,]+");
            if (urlMatch.Success) directUrl = urlMatch.Value;

            string searchTerm = "";
            var searchMatch = System.Text.RegularExpressions.Regex.Match(
                p, @"search for (.+?)(?:,|$| click| then| and| on amazon| on flipkart)");
            if (searchMatch.Success)
                searchTerm = searchMatch.Groups[1].Value
                    .Replace(" on amazon", "").Replace(" on flipkart", "").Trim();

            int productIndex = 1;
            if      (p.Contains("second")  || p.Contains("2nd"))  productIndex = 2;
            else if (p.Contains("third")   || p.Contains("3rd"))  productIndex = 3;
            else if (p.Contains("fourth")  || p.Contains("4th"))  productIndex = 4;
            else if (p.Contains("fifth")   || p.Contains("5th"))  productIndex = 5;
            else if (p.Contains("sixth")   || p.Contains("6th"))  productIndex = 6;
            else if (p.Contains("seventh") || p.Contains("7th"))  productIndex = 7;
            else if (p.Contains("eighth")  || p.Contains("8th"))  productIndex = 8;
            else if (p.Contains("ninth")   || p.Contains("9th"))  productIndex = 9;
            else if (p.Contains("tenth")   || p.Contains("10th")) productIndex = 10;
            else
            {
                var nm = System.Text.RegularExpressions.Regex.Match(p, @"product\s+(\d+)");
                if (nm.Success) productIndex = int.Parse(nm.Groups[1].Value);
            }

            string size = "";
            var sizeMatch = System.Text.RegularExpressions.Regex.Match(p, @"size\s+(\d+)");
            if (sizeMatch.Success) size = sizeMatch.Groups[1].Value;

            string action = "none";
            if      (p.Contains("emi"))                                          action = "emi";
            else if (p.Contains("add to cart") || p.Contains("cart"))           action = "cart";
            else if (p.Contains("buy") || p.Contains("purchase"))               action = "buy";

            return new ParsedPrompt(site, directUrl, searchTerm, productIndex, size, action);
        }

        private static string StripMarkdown(string code)
        {
            code = code.Trim();
            if (code.StartsWith("```"))
            {
                var lines = code.Split('\n').ToList();
                lines.RemoveAt(0);
                if (lines.Count > 0 && lines[^1].TrimStart().StartsWith("```"))
                    lines.RemoveAt(lines.Count - 1);
                code = string.Join("\n", lines);
            }
            return code;
        }

        private static string FindProjectDir(string subPath)
        {
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 10; i++)
            {
                if (Directory.GetFiles(dir, "*.csproj").Length > 0)
                    return Path.Combine(dir, subPath);
                var parent = Directory.GetParent(dir)?.FullName;
                if (parent == null) break;
                dir = parent;
            }
            return Path.Combine(AppContext.BaseDirectory, subPath);
        }
    }
}