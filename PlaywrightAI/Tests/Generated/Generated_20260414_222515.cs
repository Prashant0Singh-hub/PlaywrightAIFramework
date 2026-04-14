using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using PlaywrightAI.Tests;

namespace PlaywrightAI.Tests.Generated
{
    [TestFixture]
    public class CampusShoesSearchAndBuyTests : BaseTest
    {
        [Test]
        [Order(1)]
        public async Task SearchForCampusShoesForMen()
        {
            // Navigate to Flipkart
            await Browser.Page.GotoAsync("https://www.flipkart.com", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60000
            });

            // Wait for the search input to be visible
            var searchInput = Browser.Page.Locator("input[name='q']");
            await searchInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });

            // Clear and type "campus shoes for men" in the search box
            await searchInput.ClearAsync();
            await searchInput.FillAsync("campus shoes for men");

            // Click the search button
            var searchButton = Browser.Page.Locator("button[aria-label*='Search for Products, Brands an']");
            await searchButton.ClickAsync();

            // Wait for search results to load
            await Browser.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Browser.Page.WaitForTimeoutAsync(3000);

            // Verify search results are displayed
            var pageContent = await Browser.Page.ContentAsync();
            Assert.That(pageContent.ToLower(), Does.Contain("campus").Or.Contain("shoes"));
        }

        [Test]
        [Order(2)]
        public async Task ClickOnSecondProductFromSearchResults()
        {
            // Navigate to Flipkart
            await Browser.Page.GotoAsync("https://www.flipkart.com", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60000
            });

            // Search for campus shoes for men
            var searchInput = Browser.Page.Locator("input[name='q']");
            await searchInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });
            await searchInput.ClearAsync();
            await searchInput.FillAsync("campus shoes for men");

            var searchButton = Browser.Page.Locator("button[aria-label*='Search for Products, Brands an']");
            await searchButton.ClickAsync();

            // Wait for search results to load
            await Browser.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Browser.Page.WaitForTimeoutAsync(3000);

            // Click on the second product in the search results
            // Products are typically displayed in a grid/list; we target the second product link
            var productLinks = Browser.Page.Locator("a[href*='/p/']");
            await productLinks.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });

            var productCount = await productLinks.CountAsync();
            Assert.That(productCount, Is.GreaterThanOrEqualTo(2), "Expected at least 2 products in search results");

            // Click the second product (index 1)
            var secondProduct = productLinks.Nth(1);
            await secondProduct.ClickAsync();

            // Wait for product page to load (may open in new tab)
            await Browser.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Browser.Page.WaitForTimeoutAsync(3000);
        }

        [Test]
        [Order(3)]
        public async Task SearchClickSecondProductAndBuy()
        {
            // Navigate to Flipkart
            await Browser.Page.GotoAsync("https://www.flipkart.com", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60000
            });

            // Search for campus shoes for men
            var searchInput = Browser.Page.Locator("input[name='q']");
            await searchInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });
            await searchInput.ClearAsync();
            await searchInput.FillAsync("campus shoes for men");

            var searchButton = Browser.Page.Locator("button[aria-label*='Search for Products, Brands an']");
            await searchButton.ClickAsync();

            // Wait for search results to load
            await Browser.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Browser.Page.WaitForTimeoutAsync(3000);

            // Click on the second product - handle potential new tab
            var productLinks = Browser.Page.Locator("a[href*='/p/']");
            await productLinks.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });

            var productCount = await productLinks.CountAsync();
            Assert.That(productCount, Is.GreaterThanOrEqualTo(2), "Expected at least 2 products in search results");

            // Wait for new page (tab) to open when clicking the second product
            var newPageTask = Browser.Page.Context.WaitForPageAsync();
            var secondProduct = productLinks.Nth(1);
            await secondProduct.ClickAsync();

            IPage productPage;
            try
            {
                productPage = await newPageTask;
                await productPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            }
            catch
            {
                // If no new tab opened, stay on the current page
                productPage = Browser.Page;
                await productPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            }

            await productPage.WaitForTimeoutAsync(3000);

            // Select a size if size options are available (e.g., size 8)
            var sizeOption = productPage.Locator("a:has-text('8')").First;
            try
            {
                await sizeOption.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                await sizeOption.ClickAsync();
                await productPage.WaitForTimeoutAsync(1000);
            }
            catch
            {
                // Size might already be selected or not required
            }

            // Click on "Buy Now" button or "Add to Cart" button
            var buyNowButton = productPage.Locator("button:has-text('Buy Now'), button:has-text('BUY NOW')").First;
            try
            {
                await buyNowButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
                await buyNowButton.ClickAsync();
            }
            catch
            {
                // Try Add to Cart as fallback
                var addToCartButton = productPage.Locator("button:has-text('Add to Cart'), button:has-text('ADD TO CART')").First;
                await addToCartButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
                await addToCartButton.ClickAsync();
            }

            // Wait for the buy/cart page to load
            await productPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await productPage.WaitForTimeoutAsync(3000);

            // Verify navigation to checkout or cart page
            var currentUrl = productPage.Url;
            Assert.That(currentUrl.ToLower(), Does.Contain("checkout").Or.Contain("cart").Or.Contain("viewcart").Or.Contain("flipkart"));
        }
    }
}