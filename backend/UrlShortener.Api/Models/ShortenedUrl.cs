namespace UrlShortener.Api.Models;

public class ShortenedUrl
{
    public int Id { get; set; }
    public required string Alias { get; set; }
    public required string FullUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
