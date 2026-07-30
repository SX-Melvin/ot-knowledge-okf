namespace OTKnowledgeOKF.Scraping;

public sealed class OpenTextScraperOptions
{
    public const string SectionName = "OpenTextScraper";

    public string UserName { get; init; } = "";
    public string UserPassword { get; init; } = "";
    public bool HeadlessMode { get; init; }
    public string BrowserUserAgent { get; init; } = "";
    public int DefaultTimeout { get; init; } = 60_000;
    public string ScrapMode { get; init; } = "SUBMITTED_TICKET";
    public string TimeFilter { get; init; } = "";
    public string OkfPath { get; init; } = ".";
    public string UserDataDir { get; init; } = "./chrome-profile";
    public string? ExecutablePath { get; init; }
}

public sealed record RunScrapeRequest();
public sealed record ScrapeResult(string Mode, int FilesWritten, string OutputPath);
