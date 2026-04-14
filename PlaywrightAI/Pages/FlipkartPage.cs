using Microsoft.Playwright;
using PlaywrightAI.Core;

namespace PlaywrightAI.Pages
{
    public class FlipkartPage
    {
        private readonly IPage _page;
        private readonly BrowserFactory _browser;

        public FlipkartPage(IPage page, BrowserFactory browser)
        {
            _page    = page;
            _browser = browser;
        }

        public async Task GoToAsync()
        {
            Console.WriteLine("[FlipkartPage] Navigating to Flipkart...");
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    await _page.GotoAsync("https://www.flipkart.com/",
                        new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
                    await _page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                        new() { Timeout = 20_000 }).ConfigureAwait(false);
                    Console.WriteLine($"[FlipkartPage] ✅ Loaded (attempt {attempt})");
                    await DismissLoginPopupAsync();
                    return;
                }
                catch (Exception ex) when (attempt < 3)
                {
                    Console.WriteLine($"[FlipkartPage] Attempt {attempt} failed: {ex.Message.Split('\n')[0]}");
                    await Task.Delay(3000);
                }
            }
        }

        public async Task DismissLoginPopupAsync()
        {
            await Task.Delay(1000);
            try { await _page.PressAsync("body", "Escape"); } catch { }
            await Task.Delay(500);
            try
            {
                foreach (var sel in new[] { "button._2KpZ6l", "button[class*='close']", "._2doB4z", "span._30XB9U" })
                {
                    var btn = _page.Locator(sel).First;
                    if (await IsVisibleQuick(btn))
                    {
                        await btn.ClickAsync();
                        return;
                    }
                }
            }
            catch { /* no popup */ }
        }

        public async Task SearchAsync(string term)
        {
            Console.WriteLine($"[FlipkartPage] Searching: {term}");
            var searchBox = _page.Locator("input[name='q']:not([readonly])").First;
            await searchBox.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
            await searchBox.ClickAsync();
            await searchBox.FillAsync(term);
            await searchBox.PressAsync("Enter");
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = 30_000 });
            Console.WriteLine("[FlipkartPage] ✅ Search done");
        }

        /// <summary>
        /// Clicks the Nth product in search results (1-based index).
        /// Uses GotoAsync with href to avoid new-tab issues.
        /// </summary>
        public async Task ClickProductAtIndexAsync(int index)
        {
            Console.WriteLine($"[FlipkartPage] Clicking product #{index}...");
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Collect all product links (links with /p/ in href)
            var allLinks = await _page.Locator("a[href*='/p/']").AllAsync();
            var hrefs = new List<string>();
            foreach (var link in allLinks)
            {
                try
                {
                    var href = await link.GetAttributeAsync("href");
                    if (!string.IsNullOrEmpty(href) && !hrefs.Contains(href))
                        hrefs.Add(href);
                }
                catch { }
            }

            Console.WriteLine($"[FlipkartPage] Found {hrefs.Count} product links");

            var targetIndex = Math.Max(0, index - 1); // Convert to 0-based
            if (hrefs.Count == 0)
            {
                Console.WriteLine("[FlipkartPage] ⚠️ No product links found, trying first visible product...");
                await _page.Locator("a[href*='/p/']").First.ClickAsync();
            }
            else
            {
                if (targetIndex >= hrefs.Count)
                {
                    Console.WriteLine($"[FlipkartPage] ⚠️ Only {hrefs.Count} products found, clicking last one");
                    targetIndex = hrefs.Count - 1;
                }
                var href = hrefs[targetIndex];
                if (!href.StartsWith("http")) href = "https://www.flipkart.com" + href;
                Console.WriteLine($"[FlipkartPage] Navigating to product {index}: {href.Substring(0, Math.Min(80, href.Length))}");
                await _page.GotoAsync(href, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            }

            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Console.WriteLine($"[FlipkartPage] ✅ On product page: {_page.Url.Substring(0, Math.Min(80, _page.Url.Length))}");
        }

        public async Task SelectSizeAsync(string size)
        {
            Console.WriteLine($"[FlipkartPage] Selecting size: {size}");
            foreach (var sel in new[]
            {
                $"//li[normalize-space(text())='{size}']",
                $"//div[normalize-space(text())='{size}' and contains(@class,'size')]",
                $"li:has-text('{size}')",
                $"div:has-text('{size}')"
            })
            {
                try
                {
                    var loc = _page.Locator(sel).First;
                    await loc.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 3000 });
                    await loc.ClickAsync(new() { Force = true });
                    Console.WriteLine($"[FlipkartPage] ✅ Size {size} selected");
                    return;
                }
                catch { }
            }
            Console.WriteLine($"[FlipkartPage] ⚠️ Size {size} not found");
        }

        /// <summary>
        /// Clicks the product Buy Now button.
        /// Flipkart's product page has the real Buy button in the sticky bar at the bottom.
        /// We target it specifically and skip any review-section buttons.
        /// </summary>
        public async Task ClickBuyAsync()
        {
            Console.WriteLine("[FlipkartPage] Looking for Buy Now button...");
            await Task.Delay(1500); // Let page fully settle

            // Print all visible buttons for debugging
            try
            {
                var buttons = await _page.Locator("button").AllAsync();
                var buttonTexts = new List<string>();
                foreach (var btn in buttons)
                {
                    try
                    {
                        var text = (await btn.InnerTextAsync()).Trim();
                        if (!string.IsNullOrEmpty(text) && text.Length < 50)
                            buttonTexts.Add(text);
                    }
                    catch { }
                }
                Console.WriteLine($"[FlipkartPage] Visible buttons: [{string.Join(", ", buttonTexts.Distinct().Take(15))}]");
            }
            catch { }

            // Strategy 1: Flipkart's sticky bottom bar Buy Now button
            // The sticky bar at the bottom has class containing "GNWQQ4" or similar
            // Target: button with buy text that is NOT inside a review card
            var buySelectors = new[]
            {
                // Most specific — Flipkart's sticky action bar
                "div[class*='_3B5gMu'] button, div[class*='GNWQQ4'] button, div[class*='_3qOrv9'] button",
                // XPath: buy button NOT inside a review section
                "//button[contains(translate(text(),'buyBUY','buyBUY'),'buy') and not(ancestor::div[contains(@class,'review')]) and not(ancestor::div[contains(@class,'Review')])]",
                // Has-text selectors for the actual price button
                "button:has-text('Buy Now')",
                "button:has-text('BUY NOW')",
                "button:has-text('Buy at')",
                // Flipkart uses span inside button
                "//button[.//span[contains(translate(text(),'BUYNOW','buynow'),'buy')]]",
            };

            foreach (var sel in buySelectors)
            {
                try
                {
                    var locs = _page.Locator(sel);
                    var count = await locs.CountAsync();
                    if (count == 0) continue;

                    // Try the LAST match (the sticky bar button is typically after review section)
                    var loc = locs.Last;
                    await loc.ScrollIntoViewIfNeededAsync();
                    await Task.Delay(500);

                    var isVisible = await loc.IsVisibleAsync();
                    if (!isVisible) continue;

                    var btnText = await loc.InnerTextAsync();
                    Console.WriteLine($"[FlipkartPage] Trying selector '{sel.Substring(0, Math.Min(50, sel.Length))}' → text='{btnText.Trim()}'");

                    // Skip review-related buttons
                    if (btnText.Length > 40 || btnText.ToLower().Contains("helpful") || btnText.ToLower().Contains("report"))
                        continue;

                    await loc.ClickAsync(new() { Force = true, Timeout = 5000 });
                    Console.WriteLine($"[FlipkartPage] ✅ Clicked Buy button: '{btnText.Trim()}'");
                    await Task.Delay(2000);
                    Console.WriteLine($"[FlipkartPage] Now at: {_page.Url.Substring(0, Math.Min(80, _page.Url.Length))}");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FlipkartPage] Selector failed: {ex.Message.Split('\n')[0]}");
                }
            }

            // Last resort: JavaScript click on the LAST button containing "buy" text
            Console.WriteLine("[FlipkartPage] Trying JS fallback click...");
            try
            {
                await _page.EvaluateAsync(@"
                    () => {
                        const allEls = Array.from(document.querySelectorAll('button, a'));
                        const buyEls = allEls.filter(el => {
                            const t = (el.innerText || el.textContent || '').trim().toLowerCase();
                            return t.includes('buy') && t.length < 30 && !t.includes('helpful');
                        });
                        console.log('JS buy candidates:', buyEls.map(e => e.innerText.trim()));
                        if (buyEls.length > 0) {
                            // Click the last one (bottom of page = actual buy button)
                            buyEls[buyEls.length - 1].click();
                        }
                    }
                ");
                await Task.Delay(2000);
                Console.WriteLine($"[FlipkartPage] JS fallback done. Now at: {_page.Url.Substring(0, Math.Min(80, _page.Url.Length))}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FlipkartPage] JS fallback failed: {ex.Message.Split('\n')[0]}");
            }
        }

        public string GetCurrentUrl() => _page.Url;

        private async Task<bool> IsVisibleQuick(ILocator loc)
        {
            try { return await loc.IsVisibleAsync(); }
            catch { return false; }
        }
    }
}
