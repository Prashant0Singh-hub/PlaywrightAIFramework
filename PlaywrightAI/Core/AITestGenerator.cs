using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using PlaywrightAI.Config;

namespace PlaywrightAI.Core
{
    public class AITestGenerator
    {
        private readonly IPage _page;
        private readonly string _apiKey;
        private readonly string _model;

        public AITestGenerator(IPage page)
        {
            _page = page;
            _apiKey = AppConfig.Instance.ApiKey ?? "";
            _model  = AppConfig.Instance.Model ?? "claude-sonnet-4-6";
        }

        /// <summary>
        /// Scans the current page for all interactive elements, then asks Claude to generate
        /// NUnit test scenarios matching the user's prompt. Returns the generated C# code
        /// and saves it to Tests/Generated/.
        /// </summary>
        public async Task<string> GenerateTestsAsync(string userPrompt)
        {
            Console.WriteLine("[AI] Scanning page for all elements...");
            var locatorsJson = await ScanPageLocatorsAsync();

            Console.WriteLine($"[AI] Captured locator data, calling Claude...");
            var generatedCode = await CallClaudeAsync(userPrompt, locatorsJson);

            // Strip markdown fences if Claude wrapped the response
            generatedCode = StripMarkdownFences(generatedCode);

            // Save to Tests/Generated/ inside the project source tree
            var projectRoot = FindProjectRoot();
            var generatedDir = Path.Combine(projectRoot, "Tests", "Generated");
            Directory.CreateDirectory(generatedDir);

            var fileName = $"Generated_{DateTime.Now:yyyyMMdd_HHmmss}.cs";
            var filePath = Path.Combine(generatedDir, fileName);
            await File.WriteAllTextAsync(filePath, generatedCode, Encoding.UTF8);

            Console.WriteLine($"[AI] ✅ Saved to: {filePath}");
            return generatedCode;
        }

        private async Task<string> ScanPageLocatorsAsync()
        {
            var js = @"
() => {
    const elements = [];
    const selectors = [
        'input', 'button', 'a[href]', 'select', 'textarea',
        '[role=""button""]', '[role=""link""]', '[role=""textbox""]',
        'form', '[data-id]', '[aria-label]'
    ];
    const seen = new Set();
    selectors.forEach(sel => {
        document.querySelectorAll(sel).forEach(el => {
            if (seen.has(el)) return;
            seen.add(el);
            const rect = el.getBoundingClientRect();
            if (rect.width === 0 && rect.height === 0) return;
            const tag  = el.tagName.toLowerCase();
            const type = el.getAttribute('type') || '';
            const name = el.getAttribute('name') || '';
            const id   = el.getAttribute('id') || '';
            const aria = el.getAttribute('aria-label') || '';
            const text = (el.innerText || el.value || '').trim().substring(0, 60);
            const cls  = (el.className || '').toString().split(' ').filter(c => c && !c.match(/^_[0-9a-z]{5,}$/i)).slice(0,3).join('.');
            // Build CSS selector
            let css = tag;
            if (id) css = `#${id}`;
            else if (name) css += `[name='${name}']`;
            else if (aria) css += `[aria-label*='${aria.substring(0,30)}']`;
            else if (cls) css += `.${cls}`;
            // Build XPath
            let xpath = `//${tag}`;
            if (id) xpath = `//*[@id='${id}']`;
            else if (name) xpath += `[@name='${name}']`;
            else if (text) xpath += `[contains(text(),'${text.substring(0,40).replace(/'/g,'').trim()}')]`;
            elements.push(JSON.stringify({ tag, type, name, id, aria, text, css, xpath }));
        });
    });
    return '[' + elements.slice(0, 60).join(',') + ']';
}";
            var result = await _page.EvaluateAsync<string>(js);
            return result ?? "[]";
        }

        private async Task<string> CallClaudeAsync(string userPrompt, string locatorsJson)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("YOUR_"))
            {
                Console.WriteLine("[AI] ⚠️  No API key — returning demo code");
                return BuildDemoCode(userPrompt);
            }

            var systemPrompt = @"You are a C# Playwright/NUnit test automation expert.
Generate complete, compilable NUnit test code based on the user's requirement and the page locators.
Rules:
- Namespace: PlaywrightAI.Tests.Generated
- Inherit from BaseTest (in PlaywrightAI.Tests namespace)
- Access the page via Browser.Page (NOT Page directly)
- Use async/await throughout
- Use [TestFixture] and [Test] [Order(N)] attributes
- Each test method must be self-contained (navigate fresh)
- Use the CSS selectors and XPaths from the locator list
- Return ONLY valid C# code — no markdown, no explanation, no backticks";

            var userMessage = $@"User requirement: {userPrompt}

Page locators (JSON array):
{locatorsJson}

Generate complete NUnit test class that fulfills the requirement step by step.";

            var requestBody = new
            {
                model = _model,
                max_tokens = 4096,
                system = systemPrompt,
                messages = new[] { new { role = "user", content = userMessage } }
            };

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };
            http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await http.PostAsync("https://api.anthropic.com/v1/messages", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[AI] API error {response.StatusCode}: {responseBody}");
                return BuildDemoCode(userPrompt);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var text = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";

            return text;
        }

        private static string StripMarkdownFences(string code)
        {
            code = code.Trim();
            if (!code.StartsWith("```")) return code;
            var lines = code.Split('\n').ToList();
            lines.RemoveAt(0); // remove ```csharp or ```
            if (lines.Count > 0 && lines[^1].TrimStart().StartsWith("```"))
                lines.RemoveAt(lines.Count - 1);
            return string.Join("\n", lines).Trim();
        }

        private static string FindProjectRoot()
        {
            // Walk up from output dir to find the .csproj
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8; i++)
            {
                if (Directory.GetFiles(dir, "*.csproj").Length > 0)
                    return dir;
                var parent = Directory.GetParent(dir)?.FullName;
                if (parent == null) break;
                dir = parent;
            }
            // Fallback: save next to the output dll
            return AppContext.BaseDirectory;
        }

        private static string BuildDemoCode(string userPrompt) => $@"using NUnit.Framework;
using Microsoft.Playwright;
using System;
using System.Threading.Tasks;
using PlaywrightAI.Tests;

namespace PlaywrightAI.Tests.Generated
{{
    [TestFixture]
    public class GeneratedTests : BaseTest
    {{
        [Test]
        [Order(1)]
        public async Task Step1_NavigateToFlipkart()
        {{
            // Prompt: {userPrompt}
            await Browser.Page.GotoAsync(""https://www.flipkart.com/"");
            await Browser.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            Console.WriteLine(""Step 1: Opened Flipkart"");
        }}

        [Test]
        [Order(2)]
        public async Task Step2_SearchProduct()
        {{
            await Browser.Page.GotoAsync(""https://www.flipkart.com/"");
            await Browser.Page.WaitForSelectorAsync(""input[name='q']:not([readonly])"");
            await Browser.Page.Locator(""input[name='q']:not([readonly])"").First.FillAsync(""campus shoes"");
            await Browser.Page.Keyboard.PressAsync(""Enter"");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            Console.WriteLine(""Step 2: Searched for product"");
        }}
    }}
}}";
    }
}
