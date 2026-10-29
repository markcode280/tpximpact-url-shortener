using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public class UrlShortenerService(AppDbContext db, IAliasGenerator aliasGenerator) : IUrlShortenerService
{
    private const int MaxAliasAttempts = 5;
    private static readonly HashSet<string> ReservedAliases =
        new(StringComparer.OrdinalIgnoreCase) { "urls", "shorten", "health", "swagger", "assets" };

    public async Task<ShortenUrlResponse> ShortenAsync(
        ShortenUrlRequest request, string baseUrl, CancellationToken cancellationToken = default)
    {
        var fullUrl = request.FullUrl.Trim();
        if (fullUrl.Length > 2048 ||
            !Uri.TryCreate(fullUrl, UriKind.Absolute, out var destination) ||
            (destination.Scheme != Uri.UriSchemeHttp && destination.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrEmpty(destination.Host) || !string.IsNullOrEmpty(destination.UserInfo))
            throw new ArgumentException("fullUrl must be an absolute HTTP or HTTPS URL without credentials (maximum 2048 characters).");

        var customAlias = string.IsNullOrWhiteSpace(request.CustomAlias) ? null : request.CustomAlias.Trim();
        if (customAlias is not null && !IsValidAlias(customAlias))
            throw new ArgumentException("Alias may only contain ASCII letters, numbers, and hyphens (2–64 characters).");
        if (customAlias is not null && ReservedAliases.Contains(customAlias))
            throw new ArgumentException("This alias is reserved for an application route.");

        for (var attempt = 0; attempt < MaxAliasAttempts; attempt++)
        {
            var alias = customAlias ?? aliasGenerator.GenerateAlias();
            if (ReservedAliases.Contains(alias) || await db.ShortenedUrls.AnyAsync(u => u.Alias == alias, cancellationToken))
            {
                if (customAlias is not null) throw new AliasConflictException(alias);
                continue;
            }

            var entry = new ShortenedUrl { Alias = alias, FullUrl = fullUrl };
            db.ShortenedUrls.Add(entry);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return new ShortenUrlResponse
                {
                    Alias = alias, FullUrl = fullUrl, ShortUrl = $"{baseUrl.TrimEnd('/')}/{alias}"
                };
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
            {
                // The unique index is authoritative when concurrent requests select the same alias.
                db.Entry(entry).State = EntityState.Detached;
                if (customAlias is not null) throw new AliasConflictException(alias);
            }
        }

        throw new AliasGenerationException();
    }

    public Task<string?> GetFullUrlAsync(string alias, CancellationToken cancellationToken = default) =>
        db.ShortenedUrls.AsNoTracking().Where(u => u.Alias == alias)
            .Select(u => u.FullUrl).FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedUrls> GetPageAsync(
        string baseUrl, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new ArgumentException("page must be positive and within range; pageSize must be between 1 and 100.");

        var query = db.ShortenedUrls.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UrlListItem
            {
                Alias = u.Alias, FullUrl = u.FullUrl,
                ShortUrl = $"{baseUrl.TrimEnd('/')}/{u.Alias}", CreatedAt = u.CreatedAt
            }).ToListAsync(cancellationToken);

        return new PagedUrls(items, page, pageSize, totalCount);
    }

    public async Task<bool> DeleteAsync(string alias, CancellationToken cancellationToken = default)
    {
        var entry = await db.ShortenedUrls.FirstOrDefaultAsync(u => u.Alias == alias, cancellationToken);
        if (entry is null) return false;
        db.ShortenedUrls.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool IsValidAlias(string alias) =>
        alias.Length is >= 2 and <= 64 && alias.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');
}
