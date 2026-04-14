```csharp
using NUnit.Framework;
using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

namespace PlaywrightAI.Tests.Generated
{
    [TestFixture]
    public class FlipkartCampusShoesTests : BaseTest
    {
        [Test]
        [Order(1)]
        public async Task SearchForCampusShoesForMen()
        {
            Console.WriteLine("Step 1: Navigate to Flipkart homepage");
            await Browser.Page.GotoAsync("https://www.flipkart.com/");
            
            Console.WriteLine("Step 2: Wait for search input to be visible");
            await Browser.Page.WaitForSelectorAsync("input[name='q']");
            
            Console.WriteLine("Step 3: Enter search term 'campus shoes for men'");
            await Browser.Page.FillAsync("input[name='q']", "campus shoes for men");
            
            Console.WriteLine("Step 4: Click search button");
            await Browser.Page.ClickAsync("button[aria-label*='Search for Prod']");
            
            Console.WriteLine("Step 5: Wait for search results to load");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Assert.IsTrue(await Browser.Page.IsVisibleAsync("text=campus"), "Search results should be visible");
        }

        [Test]
        [Order(2)]
        public async Task ClickOnHurricaneRunningShoes()
        {
            Console.WriteLine("Step 1: Navigate to search results for campus shoes");
            await Browser.Page.GotoAsync("https://www.flipkart.com/");
            await Browser.Page.FillAsync("input[name='q']", "campus shoes for men");
            await Browser.Page.ClickAsync("button[aria-label*='Search for Prod']");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Console.WriteLine("Step 2: Look for HURRICANE Running Shoes");
            await Browser.Page.WaitForSelectorAsync("text=HURRICANE", new PageWaitForSelectorOptions { Timeout = 10000 });
            
            Console.WriteLine("Step 3: Click on HURRICANE Running Shoes");
            await Browser.Page.ClickAsync("text=HURRICANE");
            
            Console.WriteLine("Step 4: Wait for product page to load");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Assert.IsTrue(await Browser.Page.IsVisibleAsync("text=HURRICANE"), "HURRICANE product page should be visible");
        }

        [Test]
        [Order(3)]
        public async Task SelectSize8()
        {
            Console.WriteLine("Step 1: Navigate to HURRICANE shoes product page");
            await Browser.Page.GotoAsync("https://www.flipkart.com/");
            await Browser.Page.FillAsync("input[name='q']", "campus shoes for men");
            await Browser.Page.ClickAsync("button[aria-label*='Search for Prod']");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Browser.Page.ClickAsync("text=HURRICANE");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Console.WriteLine("Step 2: Look for size selection area");
            await Browser.Page.WaitForSelectorAsync("text=Size", new PageWaitForSelectorOptions { Timeout = 10000 });
            
            Console.WriteLine("Step 3: Select size 8");
            await Browser.Page.ClickAsync("text=8");
            
            Console.WriteLine("Step 4: Verify size 8 is selected");
            await Browser.Page.WaitForTimeoutAsync(2000);
            
            Assert.IsTrue(await Browser.Page.IsVisibleAsync("text=8"), "Size 8 should be selected");
        }

        [Test]
        [Order(4)]
        public async Task ClickBuyAt696()
        {
            Console.WriteLine("Step 1: Navigate to HURRICANE shoes product page and select size 8");
            await Browser.Page.GotoAsync("https://www.flipkart.com/");
            await Browser.Page.FillAsync("input[name='q']", "campus shoes for men");
            await Browser.Page.ClickAsync("button[aria-label*='Search for Prod']");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Browser.Page.ClickAsync("text=HURRICANE");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Browser.Page.ClickAsync("text=8");
            
            Console.WriteLine("Step 2: Look for Buy Now button with price 696");
            await Browser.Page.WaitForSelectorAsync("text=Buy Now", new PageWaitForSelectorOptions { Timeout = 10000 });
            
            Console.WriteLine("Step 3: Verify price is 696");
            var priceVisible = await Browser.Page.IsVisibleAsync("text=₹696");
            Assert.IsTrue(priceVisible, "Price ₹696 should be visible");
            
            Console.WriteLine("Step 4: Click Buy Now button");
            await Browser.Page.ClickAsync("text=Buy Now");
            
            Console.WriteLine("Step 5: Wait for checkout/login page to load");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            var loginVisible = await Browser.Page.IsVisibleAsync("text=Login");
            var checkoutVisible = await Browser.Page.IsVisibleAsync("text=Checkout");
            
            Assert.IsTrue(loginVisible || checkoutVisible, "Should navigate to login or checkout page");
        }

        [Test]
        [Order(5)]
        public async Task CompleteFlowSearchToBuy()
        {
            Console.WriteLine("Complete Flow Test: Search for campus shoes, select HURRICANE, size 8, and buy at 696");
            
            Console.WriteLine("Step 1: Navigate to Flipkart homepage");
            await Browser.Page.GotoAsync("https://www.flipkart.com/");
            
            Console.WriteLine("Step 2: Search for campus shoes for men");
            await Browser.Page.FillAsync("input[name='q']", "campus shoes for men");
            await Browser.Page.ClickAsync("button[aria-label*='Search for Prod']");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Console.WriteLine("Step 3: Click on HURRICANE Running Shoes");
            await Browser.Page.ClickAsync("text=HURRICANE");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Console.WriteLine("Step 4: Select size 8");
            await Browser.Page.ClickAsync("text=8");
            await Browser.Page.WaitForTimeoutAsync(2000);
            
            Console.WriteLine("Step 5: Verify price is 696 and click Buy Now");
            Assert.IsTrue(await Browser.Page.IsVisibleAsync("text=₹696"), "Price should be ₹696");
            await Browser.Page.ClickAsync("text=Buy Now");
            
            Console.WriteLine("Step 6: Verify navigation to login/checkout page");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            var loginVisible = await Browser.Page.IsVisibleAsync("text=Login");
            var checkoutVisible = await Browser.Page.IsVisibleAsync("text=Checkout");
            
            Assert.IsTrue(loginVisible || checkoutVisible, "Should successfully navigate to purchase flow");
            Console.WriteLine("Complete flow test passed successfully");
        }
    }
}
```