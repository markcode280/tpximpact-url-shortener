using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public interface IUrlShortenerService
{
    Task<ShortenUrlResponse> ShortenAsync(ShortenUrlRequest request, string baseUrl, CancellationToken cancellationToken = default);
    Task<string?> GetFullUrlAsync(string alias, CancellationToken cancellationToken = default);
    Task<PagedUrls> GetPageAsync(string baseUrl, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string alias, CancellationToken cancellationToken = default);
}
