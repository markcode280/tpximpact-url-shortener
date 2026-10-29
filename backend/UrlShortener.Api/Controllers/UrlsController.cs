using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

namespace UrlShortener.Api.Controllers;

[ApiController]
public class UrlsController(IUrlShortenerService service, IConfiguration configuration) : ControllerBase
{
    [HttpPost("shorten")]
    [ProducesResponseType(typeof(ShortenUrlResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Shorten([FromBody] ShortenUrlRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.ShortenAsync(request, GetBaseUrl(), cancellationToken);
            return Created(result.ShortUrl, result);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (AliasConflictException ex) { return BadRequest(new { error = ex.Message }); }
        catch (AliasGenerationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    [HttpGet("urls")]
    [ProducesResponseType(typeof(PagedUrls), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPage([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.GetPageAsync(GetBaseUrl(), query.Page, query.PageSize, cancellationToken));
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("{alias}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectToUrl(string alias, CancellationToken cancellationToken)
    {
        var fullUrl = await service.GetFullUrlAsync(alias, cancellationToken);
        return fullUrl is null ? NotFound(new { error = $"No URL found for alias '{alias}'." }) : Redirect(fullUrl);
    }

    [HttpDelete("{alias}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string alias, CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(alias, cancellationToken);
        return deleted ? NoContent() : NotFound(new { error = $"No URL found for alias '{alias}'." });
    }

    private string GetBaseUrl() => configuration["PublicBaseUrl"]?.TrimEnd('/')
        ?? $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
}
