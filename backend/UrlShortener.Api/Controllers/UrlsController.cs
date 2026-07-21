using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
public class UrlsController : ControllerBase
{
    private readonly IUrlShortenerService _service;
    private readonly ILogger<UrlsController> _logger;

    public UrlsController(IUrlShortenerService service, ILogger<UrlsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // POST /shorten
    [HttpPost("shorten")]
    public async Task<IActionResult> Shorten([FromBody] ShortenUrlRequest request)
    {        
                            return Ok();
    }

    // GET /urls
    [HttpGet("urls")]
    [ProducesResponseType(typeof(IEnumerable<UrlListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var baseUrl = GetBaseUrl();
        var urls = await _service.GetAllAsync(baseUrl);
        return Ok(urls);
    }

    // GET /{alias}  — redirect
    [HttpGet("{alias}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectToUrl(string alias)
    {
        var fullUrl = await _service.GetFullUrlAsync(alias);
        if (fullUrl is null)
            return NotFound(new { error = $"No URL found for alias '{alias}'." });

        return Redirect(fullUrl);
    }

    // DELETE /{alias}
    [HttpDelete("{alias}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string alias)
    {
        var deleted = await _service.DeleteAsync(alias);
        if (!deleted)
            return NotFound(new { error = $"No URL found for alias '{alias}'." });

        return NoContent();
    }

    private string GetBaseUrl()
    {
        return $"{Request.Scheme}://{Request.Host}";
    }
}
