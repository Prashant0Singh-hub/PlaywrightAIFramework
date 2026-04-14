using NUnit.Framework;
using PlaywrightAI.Core;

namespace PlaywrightAI.Tests
{
    /// <summary>
    /// Base class for all Playwright tests.
    /// Handles browser lifecycle: SetUp creates the browser, TearDown disposes it.
    /// TC01 (Login) sets a longer session so TearDown waits for it.
    /// </summary>
    public class BaseTest
    {
        protected BrowserFactory Browser { get; private set; } = null!;

        [SetUp]
        public async Task SetUp()
        {
            Browser = new BrowserFactory();

            // TC01 must NOT load session (it's creating one).
            // All other tests load session if available.
            var testName = TestContext.CurrentContext.Test.Name;
            bool isLoginTest = testName.Contains("TC01") || testName.Contains("Login_OTP");

            await Browser.InitAsync(withSession: !isLoginTest);
        }

        [TearDown]
        public async Task TearDown()
        {
            // Give the page a moment before disposing — prevents premature close
            // especially important for TC01 which may still be waiting for user input
            try { await Task.Delay(500); } catch { }

            if (Browser != null)
                await Browser.DisposeAsync();
        }
    }
}
