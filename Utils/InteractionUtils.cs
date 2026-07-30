using Microsoft.Playwright;

namespace OTKnowledgeOKF.Utils
{
    public static class InteractionUtils
    {
        public static async Task ClickElement(IPage page, string selector)
        {
            await page.Locator(selector).First.EvaluateAsync("element => element.click()");
        }
        public static async Task<string> GetThreadTime(
            ILocator locator,
            int timeoutMs = 4000,
            int intervalMs = 200)
        {
            var end = DateTime.UtcNow.AddMilliseconds(timeoutMs);

            while (DateTime.UtcNow < end)
            {
                var time = (await locator.GetAttributeAsync("data-original-title"))?.Trim();

                if (string.IsNullOrWhiteSpace(time))
                    time = (await locator.GetAttributeAsync("title"))?.Trim();

                if (!string.IsNullOrWhiteSpace(time))
                    return time;

                await Task.Delay(intervalMs);
            }

            return "N/A";
        }
    }
}
