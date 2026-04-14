using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using PlaywrightAI.Tests;

namespace PlaywrightAI.Tests.Generated
{
    [TestFixture]
    public class NikeShoesForMenBuyTests : BaseTest
    {
        [Test]
        [Order(1)]
        public async Task SearchForNikeShoesForMen()
        {
            await Browser.Page.GotoAsync("https://www.flipkart.com");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var searchInput = Browser.Page.Locator("input[name='q']");
            await searchInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await searchInput.ClearAsync();
            await searchInput.FillAsync("nike shoes for men");

            var searchButton = Browser.Page.Locator("button[aria-label*='Search for Products, Brands an']");
            await searchButton.ClickAsync();

            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Browser.Page.WaitForTimeoutAsync(3000);

            Assert.That(Browser.Page.Url, Does.Contain("nike+shoes+for+men").Or.Contain("nike%20shoes%20for%20men").Or.Contain("q=nike"));
        }

        [Test]
        [Order(2)]
        public async Task ClickOnThirdProductFromSearchResults()
        {
            await Browser.Page.GotoAsync("https://www.flipkart.com");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var searchInput = Browser.Page.Locator("input[name='q']");
            await searchInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await searchInput.ClearAsync();
            await searchInput.FillAsync("nike shoes for men");

            var searchButton = Browser.Page.Locator("button[aria-label*='Search for Products, Brands an']");
            await searchButton.ClickAsync();

            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Browser.Page.WaitForTimeoutAsync(3000);

            // Click on the third product - NIKE JUNIPER TRAIL 3
            var thirdProduct = Browser.Page.Locator("//a[contains(text(),'NIKE NIKE JUNIPER TRAIL 3 Running Shoes')]").First;
            await thirdProduct.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });

            var newPageTask = Browser.Page.Context.WaitForPageAsync();
            await thirdProduct.ClickAsync();

            var newPage = await newPageTask;
            await newPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await newPage.WaitForTimeoutAsync(2000);

            var pageContent = await newPage.ContentAsync();
            Assert.That(pageContent, Does.Contain("NIKE").Or.Contain("nike"));
        }

        [Test]
        [Order(3)]
        public async Task SearchClickThirdProductSelectSizeAndBuy()
        {
            await Browser.Page.GotoAsync("https://www.flipkart.com");
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Step 1: Search for nike shoes for men
            var searchInput = Browser.Page.Locator("input[name='q']");
            await searchInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await searchInput.ClearAsync();
            await searchInput.FillAsync("nike shoes for men");

            var searchButton = Browser.Page.Locator("button[aria-label*='Search for Products, Brands an']");
            await searchButton.ClickAsync();

            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Browser.Page.WaitForTimeoutAsync(3000);

            // Step 2: Click on the third product (NIKE JUNIPER TRAIL 3)
            var thirdProduct = Browser.Page.Locator("//a[contains(text(),'NIKE NIKE JUNIPER TRAIL 3 Running Shoes')]").First;
            await thirdProduct.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });

            var newPageTask = Browser.Page.Context.WaitForPageAsync();
            await thirdProduct.ClickAsync();

            var productPage = await newPageTask;
            await productPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await productPage.WaitForTimeoutAsync(3000);

            // Step 3: Select size 8
            var sizeOption = productPage.Locator("a").Filter(new LocatorFilterOptions { HasTextString = "8" }).First;
            await sizeOption.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await sizeOption.ClickAsync();
            await productPage.WaitForTimeoutAsync(2000);

            // Step 4: Click Buy Now button
            var buyNowButton = productPage.Locator("button:has-text('Buy Now'), button:has-text('BUY NOW')").First;
            await buyNowButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await buyNowButton.ClickAsync();

            await productPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await productPage.WaitForTimeoutAsync(3000);

            // Verify navigation to checkout or order summary page
            var pageContent = await productPage.ContentAsync();
            Assert.That(pageContent, Does.Contain("Order Summary").Or.Contain("Place Order").Or.Contain("Deliver").Or.Contain("PLACE ORDER").Or.Contain("Checkout"));
        }
    }
}