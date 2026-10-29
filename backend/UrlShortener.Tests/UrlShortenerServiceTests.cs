using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;
using Xunit;

namespace UrlShortener.Tests;

public class UrlShortenerServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly AppDbContext _db;
    private readonly UrlShortenerService _sut;
    private const string BaseUrl = "http://localhost:8080";

    public UrlShortenerServiceTests()
    {
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _sut = new UrlShortenerService(_db, new RandomAliasGenerator());
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    [Fact]
    public async Task Shorten_GeneratesValidAliasAndPersistsDestination()
    {
        var result = await _sut.ShortenAsync(new() { FullUrl = "https://example.com/path" }, BaseUrl);
        result.Alias.Should().MatchRegex("^[a-zA-Z0-9]{8}$");
        result.ShortUrl.Should().Be($"{BaseUrl}/{result.Alias}");
        _db.ChangeTracker.Clear();
        (await _sut.GetFullUrlAsync(result.Alias)).Should().Be("https://example.com/path");
    }

    [Fact]
    public async Task Shorten_TrimsCustomAliasAndDestination()
    {
        var result = await _sut.ShortenAsync(new()
            { FullUrl = " https://example.com ", CustomAlias = " my-alias " }, BaseUrl + "/");
        result.Alias.Should().Be("my-alias");
        result.FullUrl.Should().Be("https://example.com");
        result.ShortUrl.Should().Be($"{BaseUrl}/my-alias");
    }

    [Theory]
    [InlineData("a")]
    [InlineData("bad alias")]
    [InlineData("bad/alias")]
    [InlineData("éclair")]
    [InlineData("urls")]
    [InlineData("SHORTEN")]
    [InlineData("health")]
    [InlineData("swagger")]
    [InlineData("assets")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task Shorten_RejectsMalformedOrReservedAliases(string alias)
    {
        await _sut.Invoking(s => s.ShortenAsync(new() { FullUrl = "https://example.com", CustomAlias = alias }, BaseUrl))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/file")]
    [InlineData("https://user:password@example.com")]
    [InlineData("")]
    public async Task Shorten_RejectsInvalidDestination(string url)
    {
        await _sut.Invoking(s => s.ShortenAsync(new() { FullUrl = url }, BaseUrl))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Shorten_DuplicateCustomAliasDoesNotOverwriteOriginal()
    {
        await _sut.ShortenAsync(new() { FullUrl = "https://first.com", CustomAlias = "same" }, BaseUrl);
        await _sut.Invoking(s => s.ShortenAsync(new() { FullUrl = "https://other.com", CustomAlias = "same" }, BaseUrl))
            .Should().ThrowAsync<AliasConflictException>();
        (await _sut.GetFullUrlAsync("same")).Should().Be("https://first.com");
    }

    [Fact]
    public async Task Shorten_RetriesGeneratedAliasCollision()
    {
        await _sut.ShortenAsync(new() { FullUrl = "https://first.com", CustomAlias = "taken" }, BaseUrl);
        var generator = new Mock<IAliasGenerator>();
        generator.SetupSequence(g => g.GenerateAlias()).Returns("taken").Returns("available");
        var service = new UrlShortenerService(_db, generator.Object);
        (await service.ShortenAsync(new() { FullUrl = "https://new.com" }, BaseUrl)).Alias.Should().Be("available");
        generator.Verify(g => g.GenerateAlias(), Times.Exactly(2));
    }

    [Fact]
    public async Task Shorten_BoundsCollisionRetries()
    {
        await _sut.ShortenAsync(new() { FullUrl = "https://first.com", CustomAlias = "taken" }, BaseUrl);
        var generator = new Mock<IAliasGenerator>();
        generator.Setup(g => g.GenerateAlias()).Returns("taken");
        var service = new UrlShortenerService(_db, generator.Object);
        await service.Invoking(s => s.ShortenAsync(new() { FullUrl = "https://new.com" }, BaseUrl))
            .Should().ThrowAsync<AliasGenerationException>();
        generator.Verify(g => g.GenerateAlias(), Times.Exactly(5));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Shorten_HandlesUniqueConstraintRaceAtSave(bool customAlias)
    {
        await using var raceDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).AddInterceptors(new InsertCompetingAlias()).Options);
        var generator = new Mock<IAliasGenerator>();
        generator.SetupSequence(g => g.GenerateAlias()).Returns("racealias").Returns("nextalias");
        var service = new UrlShortenerService(raceDb, generator.Object);
        var request = new ShortenUrlRequest { FullUrl = "https://new.com", CustomAlias = customAlias ? "racealias" : null };
        if (customAlias)
            await service.Invoking(s => s.ShortenAsync(request, BaseUrl)).Should().ThrowAsync<AliasConflictException>();
        else
            (await service.ShortenAsync(request, BaseUrl)).Alias.Should().Be("nextalias");
        (await _sut.GetFullUrlAsync("racealias")).Should().Be("https://competitor.com");
    }

    [Fact]
    public async Task GetPage_OrdersNewestFirstAndBreaksTiesById()
    {
        var timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _db.ShortenedUrls.AddRange(
            new ShortenedUrl { Alias = "old", FullUrl = "https://old.com", CreatedAt = timestamp.AddDays(-1) },
            new ShortenedUrl { Alias = "middle", FullUrl = "https://middle.com", CreatedAt = timestamp },
            new ShortenedUrl { Alias = "new", FullUrl = "https://new.com", CreatedAt = timestamp });
        await _db.SaveChangesAsync();
        var first = await _sut.GetPageAsync(BaseUrl, 1, 2);
        var second = await _sut.GetPageAsync(BaseUrl, 2, 2);
        first.Items.Select(i => i.Alias).Should().Equal("new", "middle");
        second.Items.Select(i => i.Alias).Should().Equal("old");
        first.TotalCount.Should().Be(3);
        first.TotalPages.Should().Be(2);
        first.Items[0].CreatedAt.Should().Be(timestamp);
        first.Items[0].ShortUrl.Should().Be($"{BaseUrl}/new");
    }

    [Fact]
    public async Task GetPage_EmptyAndOutOfRangePagesHaveConsistentMetadata()
    {
        var result = await _sut.GetPageAsync(BaseUrl, 3, 10);
        result.Items.Should().BeEmpty();
        result.Page.Should().Be(3);
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public async Task GetPage_RejectsInvalidAndOverflowingPagination(int page, int pageSize)
    {
        await _sut.Invoking(s => s.GetPageAsync(BaseUrl, page, pageSize)).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Delete_PersistsRemovalAcrossContexts()
    {
        await _sut.ShortenAsync(new() { FullUrl = "https://example.com", CustomAlias = "delete-me" }, BaseUrl);
        (await _sut.DeleteAsync("delete-me")).Should().BeTrue();
        await using var freshDb = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        (await freshDb.ShortenedUrls.AnyAsync(u => u.Alias == "delete-me")).Should().BeFalse();
        (await _sut.DeleteAsync("delete-me")).Should().BeFalse();
    }

    private sealed class InsertCompetingAlias : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var db = eventData.Context!;
            if (db.ChangeTracker.Entries<ShortenedUrl>().Any(e => e.State == EntityState.Added && e.Entity.Alias == "racealias"))
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO ShortenedUrls (Alias, FullUrl, CreatedAt) VALUES ({"racealias"}, {"https://competitor.com"}, {DateTime.UtcNow})",
                    cancellationToken);
            return result;
        }
    }
}
