using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using OTKnowledgeOKF.Dto;
using OTKnowledgeOKF.Utils;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static OTKnowledgeOKF.Dto.OllamaDto;

namespace OTKnowledgeOKF.Scraping;

public sealed class OpenTextScraperService(
    IOptions<OpenTextScraperOptions> configuredOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<OpenTextScraperService> logger)
{
    private const string SubmittedTicketMode = "SUBMITTED_TICKET";
    private const string PublicTicketMode = "PUBLIC_TICKET";
    private readonly SemaphoreSlim runLock = new(1, 1);

    public async Task<ScrapeResult> RunAsync(RunScrapeRequest? request, CancellationToken cancellationToken)
    {
        if (!await runLock.WaitAsync(0, cancellationToken))
            throw new InvalidOperationException("A scrape is already in progress. The persistent browser profile cannot be shared.");

        try
        {
            var options = configuredOptions.Value;
            var timeFilter = options.TimeFilter.Trim().ToUpperInvariant();
            if (options.ScrapMode is not (SubmittedTicketMode or PublicTicketMode))
                throw new InvalidOperationException("Mode must be SUBMITTED_TICKET or PUBLIC_TICKET.");
            if (string.IsNullOrWhiteSpace(options.UserName) || string.IsNullOrWhiteSpace(options.UserPassword))
                throw new InvalidOperationException("OpenTextScraper credentials are not configured.");

            using var playwright = await Playwright.CreateAsync();
            var context = await playwright.Chromium.LaunchPersistentContextAsync(options.UserDataDir,
                new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = options.HeadlessMode,
                    ExecutablePath = string.IsNullOrWhiteSpace(options.ExecutablePath) ? null : options.ExecutablePath,
                    UserAgent = string.IsNullOrWhiteSpace(options.BrowserUserAgent) ? null : options.BrowserUserAgent,
                    ViewportSize = null,
                    Args = ["--disable-http2", "--disable-blink-features=AutomationControlled", "--no-sandbox"]
                });
            try
            {
                var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
                page.SetDefaultTimeout(options.DefaultTimeout);
                var url = options.ScrapMode == SubmittedTicketMode
                    ? "https://support.opentext.com/csm?id=csm_my_cases"
                    : BuildPublicTicketUrl(options.TimeFilter);

                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
                await LoginIfNeededAsync(page, options, cancellationToken);
                var filesWritten = options.ScrapMode == SubmittedTicketMode
                    ? await ScrapeSubmittedTicketsAsync(page, options.OkfPath, cancellationToken)
                    : await ScrapePublicTicketsAsync(page, options.OkfPath, cancellationToken);
                return new ScrapeResult(options.ScrapMode, filesWritten, Path.GetFullPath(options.OkfPath));
            }
            finally { await context.CloseAsync(); }
        }
        finally { runLock.Release(); }
    }

    private static async Task LoginIfNeededAsync(IPage page, OpenTextScraperOptions options, CancellationToken cancellationToken)
    {
        var login = page.Locator("button#sitenav-login-button");
        try { await login.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 }); }
        catch (TimeoutException) { return; }
        await page.Locator("#user").FillAsync(options.UserName);
        await page.Locator("#password").FillAsync(options.UserPassword);
        await page.Locator("#signon").ClickAsync();
    }

    private async Task<int> ScrapePublicTicketsAsync(IPage page, string outputPath, CancellationToken cancellationToken)
    {
        var count = 0;
        while (true)
        {
            var links = await page.Locator(".knowledge-articles .kb-article-summary a").AllAsync();
            foreach (var link in links)
            {
                var newPageTask = page.Context.WaitForPageAsync();
                await link.ClickAsync(new() { Modifiers = [KeyboardModifier.Control] });
                var ticket = await newPageTask;
                try
                {
                    await ticket.BringToFrontAsync();
                    var title = await ticket.Locator("h2.widget-header").InnerTextAsync();
                    var summary = await ExtractTicketSectionAsync(ticket, "Summary");
                    var resolution = await ExtractTicketSectionAsync(ticket, "Resolution");
                    var sections = new[] {
                        "# " + title,
                        TicketSection("Problem Description", summary), 
                        //TicketSection("Symptoms", await ExtractTicketSectionAsync(ticket, "Additional Information")),
                        TicketSection("Root Cause", await ExtractTicketSectionAsync(ticket, "Cause")),
                        TicketSection("Resolution Steps", resolution), 
                        TicketSection("Verification", await ExtractTicketSectionAsync(ticket, "Additional Information")),
                        //TicketSection("Related Articles & References", await ExtractTicketSectionAsync(ticket, "Additional Information")),
                    };
                    var caseNumber = await ticket.Locator(".kb-number-info .ng-binding").First.InnerTextAsync();
                    await WriteOkfFileAsync(outputPath, new()
                    {
                        Id = caseNumber,
                        Sensitivity = OKFHeaderSensitivity.Internal,
                        Type = OKFHeaderType.KnowledgeBaseArticle,
                        Confidence = OKFHeaderConfidence.Probable,
                        Status = resolution.Length > 0 ? OKFHeaderStatus.Solved : OKFHeaderStatus.Process,
                        Related = [],
                    }, string.Join("\n\n", sections), cancellationToken);
                    count++;
                }
                finally { await ticket.CloseAsync(); }
            }
            var parent = page.Locator(".page-link[aria-label='Next']").First.Locator("..");
            if (await parent.Locator(".disabled").CountAsync() > 0) break;
            await page.Locator(".page-link[aria-label='Next']").ClickAsync();
        }
        return count;
    }

    private async Task<int> ScrapeSubmittedTicketsAsync(IPage page, string outputPath, CancellationToken cancellationToken)
    {
        var accounts = new HashSet<string>(); var count = 0;
        while (true)
        {
            await InteractionUtils.ClickElement(page, ".UserName.ng-binding");
            await InteractionUtils.ClickElement(page, ".selectAccount.ng-scope");
            await Task.Delay(2000, cancellationToken);
            await page.Locator(".padStyle div.select2-container a.select2-choice").DispatchEventAsync("mousedown");
            var accountOptions = page.Locator("div.select2-result-label"); await accountOptions.First.WaitForAsync();
            var account = (await accountOptions.AllInnerTextsAsync()).Select(x => x.Trim()).FirstOrDefault(x => !accounts.Contains(x));
            if (account is null) break;
            accounts.Add(account);
            await accountOptions.Filter(new() { HasTextString = account }).First.ClickAsync();
            await page.Locator(".accountButton").ClickAsync(); await Task.Delay(3000, cancellationToken);
            while (true)
            {
                var ticketLinks = page.Locator(".otTableFont.ng-scope .ng-binding[role='link']");
                try { await ticketLinks.First.WaitForAsync(); } catch (TimeoutException) { break; }
                var linkCount = await ticketLinks.CountAsync();
                for (var index = 0; index < linkCount; index++)
                {
                    var ticketTask = page.Context.WaitForPageAsync(); await page.Locator(".otTableFont.ng-scope .ng-binding[role='link']").Nth(index).ClickAsync();
                    var ticket = await ticketTask;
                    try { count += await ScrapeSubmittedTicketAsync(ticket, outputPath, cancellationToken); }
                    finally { await ticket.CloseAsync(); await page.BringToFrontAsync(); }
                }
                var next = page.Locator("[aria-label='Next page ']");
                if (await next.CountAsync() == 0 || await next.IsDisabledAsync()) break;
                await next.ClickAsync(); await Task.Delay(3000, cancellationToken);
            }
        }
        return count;
    }

    private async Task<int> ScrapeSubmittedTicketAsync(IPage ticket, string outputPath, CancellationToken cancellationToken)
    {
        await ticket.BringToFrontAsync();
        var title = await TextOrDefaultAsync(ticket.Locator(".m-n.sd.ng-binding"), "untitled-ticket");
        var caseNumber = await TextOrDefaultAsync(ticket.Locator(".ot-caseNumber.ng-binding"), "untitled-ticket-number");
        var description = await TextOrDefaultAsync(ticket.Locator("[sn-atf-area='OT Case Description Ticket Tab']"), "");
        var threads = new List<string>(); 
        var timelines = ticket.Locator("div.timeline-panel.timeline-border");
        for (var index = 0; index < await timelines.CountAsync(); index++)
        {
            var timeline = timelines.Nth(index);
            var comments = (await timeline
                .Locator("div.timeline-panel-inner.default-comment p p")
                .AllInnerTextsAsync())
                .Select(x => x.Trim())
                .Where(x => x.Length > 0);
            var author = await timeline.Locator("div.timeline-title.h4.ng-binding").EvaluateAsync<string>(@"
                el => el.childNodes[0].textContent.trim()
            ");

            var time = await InteractionUtils.GetThreadTime(
                timeline.Locator("small time").First
            );
            threads.Add($"## Thread {index + 1}\n\n**Author:** {author}\n\n**Time:** {time}\n\n{string.Join("\n\n", comments)}");
        }
        await WriteOkfFileAsync(outputPath, new()
        {
            Type = OKFHeaderType.SupportCaseThread,
            Sensitivity = OKFHeaderSensitivity.Internal,
            Confidence = OKFHeaderConfidence.Probable,
            Id = caseNumber,
            Related = [],
        }, string.Join("\n\n", threads), cancellationToken);
        return 1;
    }

    private static async Task<string> ExtractTicketSectionAsync(IPage page, string section) =>
        await page.Locator("h3.ng-binding").EvaluateAllAsync<string>("(elements, section) => { const heading = [...elements].find(el => el.innerText.trim() === section); return heading?.parentElement?.querySelector('section.ng-binding.ng-scope')?.innerText?.trim() || ''; }", section);
    private static async Task<string[]> ExtractAppliesToAsync(IPage page) =>
    await page.Locator("h3.ng-binding").EvaluateAllAsync<string[]>(@"
        elements => {
            const h = [...elements].find(el => el.textContent.trim() === 'Applies to');
            if (!h) return [];

            const container = h.parentElement?.parentElement;
            if (!container) return [];

            return Array.from(container.querySelectorAll('section.ng-binding.ng-scope p'))
                .map(p => {
                    const c = p.cloneNode(true);
                    c.querySelectorAll('span').forEach(s => s.remove());
                    return c.textContent.trim();
                })
                .filter(Boolean);
        }
    ") ?? [];
    private static string TicketSection(string title, string text) => text.Length == 0 ? "" : $"## {title}\n\n{text}";
    private static string ListSection(string title, IReadOnlyList<string> values) => $"## {title}\n\n{string.Join("\n", values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => $"- {x}"))}";
    private static async Task<string> TextOrDefaultAsync(ILocator locator, string fallback) 
    { 
        try 
        {
            return await locator.InnerTextAsync(); 
        } 
        catch (PlaywrightException) 
        { 
            return fallback; 
        } 
    }
    private static string BuildPublicTicketUrl(string filter)
    {
        const string url = "https://support.opentext.com/csm?id=ot_kb_search&spa=1&u_product_line=a2ef151c1bb24d10fea2ec20604bcb1a&kb_category=d6344bdadb21781068cfd6c4e296190c";
        var values = new[] { "LAST_YEAR", "LAST_SIX_MONTHS", "LAST_THREE_MONTHS", "LAST_MONTH", "LAST_TWO_WEEKS", "LAST_WEEK" };
        return values.Contains(filter) ? $"{url}&modified={Array.IndexOf(values, filter)}" : url;
    }
    private async Task WriteOkfFileAsync(string root, OKFHeaderConfig config, string body, CancellationToken cancellationToken)
    {
        var safeName = Regex.Replace(config.Id, @"[<>:""/\\|?*]", "_").Trim(); var directory = Path.Combine(root, safeName);
        Directory.CreateDirectory(directory);
        //description = await SimplifyDescriptionAsync(config.Id, title, description, body, cancellationToken);
        var frontmatter = OKFUtils.GenerateHeader(config);
        await File.WriteAllTextAsync(Path.Combine(directory, "index.md"), string.IsNullOrWhiteSpace(body) ? $"{frontmatter}\n" : $"{frontmatter}\n\n{body.Trim()}\n", Encoding.UTF8, cancellationToken);
    }
    private async Task<string> SimplifyDescriptionAsync(
    string name,
    string title,
    string description,
    string body,
    CancellationToken cancellationToken)
    {
        var ollama = configuredOptions.Value.Ollama;
        if (!ollama.Enabled)
            return description;

        var bodyExcerpt = body.Length <= ollama.MaxBodyCharacters
            ? body
            : body[..ollama.MaxBodyCharacters];

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(ollama.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(ollama.TimeoutSeconds);

            using var response = await client.PostAsJsonAsync(
                "api/chat",
                new
                {
                    model = ollama.Model,
                    stream = false,
                    messages = new object[]
                    {
                    new
                    {
                        role = "system",
                        content =
                            """
                            You are generating metadata for a knowledge base.

                            Your task is to write a short DESCRIPTION.

                            Requirements:
                            - Output exactly one paragraph.
                            - Maximum 3 sentences.
                            - No Markdown.
                            - No lists.
                            - No headings.
                            - No quotation marks.
                            - Do not explain your reasoning.
                            - Do not summarize every version number.
                            - Focus on the problem and the solution.
                            - Preserve important product names and error codes.
                            - If there is insufficient information, summarize only what is known.
                            """
                    },
                    new
                    {
                        role = "user",
                        content =
                            $"""
                            Write a concise description (1-3 sentences) for this knowledge record.

                            Name: {name}
                            Title: {title}
                            Existing description: {description}

                            Body:
                            {bodyExcerpt}
                            """
                    }
                    },
                    options = new
                    {
                        temperature = 0.2,
                        num_predict = 120
                    }
                },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);

            var simplified = result?.Message?.Content?.Trim();

            return string.IsNullOrWhiteSpace(simplified)
                ? description
                : simplified;
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or TaskCanceledException
            or JsonException)
        {
            logger.LogWarning(exception,
                "Could not simplify description with Ollama; using the scraped description.");

            return description;
        }
    }
    private static string Yaml(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    private sealed record OllamaGenerateResponse(string? Response);
}
