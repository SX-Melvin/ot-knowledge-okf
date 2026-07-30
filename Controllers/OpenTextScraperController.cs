using Microsoft.AspNetCore.Mvc;
using OTKnowledgeOKF.Scraping;

namespace OTKnowledgeOKF.Controllers;

[ApiController]
[Route("api/opentext-scraper")]
public sealed class OpenTextScraperController(OpenTextScraperService scraper) : ControllerBase
{
    /// <summary>Runs the configured OpenText scraper. This request remains open until scraping finishes.</summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(ScrapeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ScrapeResult>> Run(
        [FromBody] RunScrapeRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await scraper.RunAsync(request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }
}
