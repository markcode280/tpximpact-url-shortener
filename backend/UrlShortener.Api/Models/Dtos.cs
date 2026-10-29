using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Api.Models;

public class ShortenUrlRequest
{
    [Required(ErrorMessage = "fullUrl is required.")]
    [Url(ErrorMessage = "fullUrl must be a valid URL.")]
    [StringLength(2048)]
    public required string FullUrl { get; set; }

    public string? CustomAlias { get; set; }
}

public class ShortenUrlResponse
{
    public required string ShortUrl { get; set; }
    public required string Alias { get; set; }
    public required string FullUrl { get; set; }
}

public class UrlListItem
{
    public required string Alias { get; set; }
    public required string FullUrl { get; set; }
    public required string ShortUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record PagedUrls(IReadOnlyList<UrlListItem> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}
