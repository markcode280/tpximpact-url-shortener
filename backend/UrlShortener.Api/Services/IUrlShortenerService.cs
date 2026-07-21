using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public interface IUrlShortenerService
{
    Task<ShortenUrlResponse> ShortenAsync(ShortenUrlRequest request, string baseUrl);
    Task<string?> GetFullUrlAsync(string alias);
    Task<IEnumerable<UrlListItem>> GetAllAsync(string baseUrl);
    Task<bool> DeleteAsync(string alias);
}
