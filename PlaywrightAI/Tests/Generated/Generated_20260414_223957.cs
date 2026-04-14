using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using PlaywrightAI.Tests;

namespace PlaywrightAI.Tests.Generated
{
    [TestFixture]
    public class FlipkartNikeShoesTests : BaseTest
    {
        [Test]
        [Order(1)]
        public async Task SearchForNikeShoesAndClickThirdProductAndBuyWithUPI()
        {
            // Step 1: Navigate to Flipkart
            await Browser.Page.GotoAsync("https://www.flipkart.com", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await Browser.Page.WaitForTimeoutAsync(3000);

            // Step 2: Search for "nike shoes for men"
            var searchInput = Browser.Page.Locator("input[name='q']");
            await searchInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await searchInput.ClearAsync();
            await searchInput.FillAsync("nike shoes for men");
            await Browser.Page.WaitForTimeoutAsync(1000);

            // Step 3: Click the search button
            var searchButton = Browser.Page.Locator("button[aria-label*='Search for Products, Brands an']");
            await searchButton.ClickAsync();
            await Browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Browser.Page.WaitForTimeoutAsync(3000);

            // Step 4: Click on the third product in the search results
            // The search results page typically has product cards as anchor links
            // We'll look for product links in the results grid and click the third one
            var productLinks = Browser.Page.Locator("div[data-id] a[href*='/p/']").First;
            
            // Try to get all product cards on the search results page
            var allProductCards = Browser.Page.Locator("//div[@data-id]");
            await allProductCards.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });
            await Browser.Page.WaitForTimeoutAsync(2000);

            // Click on the third product (index 2)
            var thirdProduct = allProductCards.Nth(2).Locator("a").First;
            
            // Get the href to navigate in case it opens in a new tab
            var href = await thirdProduct.GetAttributeAsync("href");
            
            if (href != null && !href.StartsWith("http"))
            {
                href = "https://www.flipkart.com" + href;
            }

            // Click on the third product - handle potential new tab
            var pagePromise = Browser.Page.Context.WaitForPageAsync();
            await thirdProduct.ClickAsync();
            
            IPage productPage;
            try
            {
                productPage = await pagePromise;
                await productPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            catch
            {
                // If no new page opened, continue on the same page
                productPage = Browser.Page;
                await productPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            
            await productPage.WaitForTimeoutAsync(3000);

            // Step 5: Select a size if size options are available (pick size 8 as a common size)
            try
            {
                var sizeOption = productPage.Locator("a").Filter(new LocatorFilterOptions { HasTextString = "8" });
                var sizeLinks = productPage.Locator("//a[contains(@class,'') and normalize-space(text())='8']");
                
                // Try clicking on size 8 from the size selector area
                var sizeContainer = productPage.Locator("div:has(> a:has-text('Size Chart'))");
                var size8 = sizeContainer.Locator("a").Filter(new LocatorFilterOptions { HasTextString = "8" }).First;
                await size8.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                await size8.ClickAsync();
                await productPage.WaitForTimeoutAsync(2000);
            }
            catch
            {
                // Size selection may not be required or already selected
            }

            // Step 6: Click on "Buy Now" button
            var buyNowButton = productPage.Locator("button:has-text('Buy Now'), button:has-text('BUY NOW')").First;
            await buyNowButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            await buyNowButton.ClickAsync();
            await productPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await productPage.WaitForTimeoutAsync(5000);

            // Step 7: On the order summary / address page, click Continue if needed
            try
            {
                var continueButton = productPage.Locator("button:has-text('Continue'), button:has-text('CONTINUE')").First;
                await continueButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 8000 });
                await continueButton.ClickAsync();
                await productPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await productPage.WaitForTimeoutAsync(3000);
            }
            catch
            {
                // Continue button may not appear if address is already selected
            }

            // Step 8: On the order summary page, click Continue again if present (for delivery options)
            try
            {
                var continueButton2 = productPage.Locator("button:has-text('Continue'), button:has-text('CONTINUE')").First;
                await continueButton2.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                await continueButton2.ClickAsync();
                await productPage.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await productPage.WaitForTimeoutAsync(3000);
            }
            catch
            {
                // May not be present
            }

            // Step 9: Select UPI payment option
            try
            {
                // Look for UPI option in payment methods
                var upiOption = productPage.Locator("text=UPI").First;
                await upiOption.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
                await upiOption.ClickAsync();
                await productPage.WaitForTimeoutAsync(3000);
            }
            catch
            {
                // Try alternative UPI selectors
                try
                {
                    var upiRadio = productPage.Locator("label:has-text('UPI'), div:has-text('UPI')").First;
                    await upiRadio.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                    await upiRadio.ClickAsync();
                    await productPage.WaitForTimeoutAsync(2000);
                }
                catch
                {
                    // UPI option might have a different structure
                    var paymentOptions = productPage.Locator("//div[contains(text(),'UPI')] | //span[contains(text(),'UPI')] | //label[contains(text(),'UPI')]").First;
                    await paymentOptions.ClickAsync();
                    await productPage.WaitForTimeoutAsync(2000);
                }
            }

            // Verify we are on the UPI payment section
            var upiSection = productPage.Locator("text=/UPI/i").First;
            await upiSection.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            Assert.That(await upiSection.IsVisibleAsync(), Is.True, "UPI payment option should be visible and selected");
        }
    }
}