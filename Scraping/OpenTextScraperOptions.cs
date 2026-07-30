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
    public OllamaOptions Ollama { get; init; } = new();
}

public sealed class OllamaOptions
{
    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = "http://localhost:11434";
    public string Model { get; init; } = "llama3.2";
    public int TimeoutSeconds { get; init; } = 60;
    public int MaxBodyCharacters { get; init; } = 12_000;
}

public sealed record RunScrapeRequest();
public sealed record ScrapeResult(string Mode, int FilesWritten, string OutputPath);
