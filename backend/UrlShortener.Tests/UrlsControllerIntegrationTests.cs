using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;
using Xunit;

namespace UrlShortener.Tests;

public class UrlsControllerIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UrlsControllerIntegrationTests()
    {
        _connection.Open();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
            }));
        _client = _factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    public void Dispose() { _client.Dispose(); _factory.Dispose(); _connection.Dispose(); }

    [Theory]
    [InlineData(null)]
    [InlineData("my-alias")]
    public async Task Create_List_Redirect_Delete_IsPersistent(string? alias)
    {
        var create = await _client.PostAsJsonAsync("/shorten", new { fullUrl = "https://example.com/path", customAlias = alias });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = (await create.Content.ReadFromJsonAsync<ShortenUrlResponse>())!;
        create.Headers.Location!.ToString().Should().Be(body.ShortUrl);
        if (alias is not null) body.Alias.Should().Be(alias);

        var page = (await _client.GetFromJsonAsync<PagedUrls>("/urls"))!;
        page.Items.Single().Alias.Should().Be(body.Alias);
        page.TotalCount.Should().Be(1);
        page.PageSize.Should().Be(10);

        var redirect = await _client.GetAsync($"/{body.Alias}");
        redirect.StatusCode.Should().Be(HttpStatusCode.Found);
        redirect.Headers.Location!.ToString().Should().Be("https://example.com/path");

        (await _client.DeleteAsync($"/{body.Alias}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Each HTTP request uses a fresh scoped DbContext, proving deletion was saved.
        (await _client.GetAsync($"/{body.Alias}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.GetFromJsonAsync<PagedUrls>("/urls"))!.TotalCount.Should().Be(0);
        (await _client.DeleteAsync($"/{body.Alias}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DuplicateAlias_Returns400WithoutOverwriting()
    {
        await _client.PostAsJsonAsync("/shorten", new { fullUrl = "https://first.com", customAlias = "same" });
        var response = await _client.PostAsJsonAsync("/shorten", new { fullUrl = "https://other.com", customAlias = "same" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("already taken");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/file")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:password@example.com")]
    public async Task InvalidDestination_Returns400WithReadableError(string url)
    {
        var response = await _client.PostAsJsonAsync("/shorten", new { fullUrl = url });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<Dictionary<string, string>>())!["error"].Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MissingDestination_Returns400()
    {
        (await _client.PostAsJsonAsync("/shorten", new { customAlias = "alias" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("bad alias")]
    [InlineData("urls")]
    [InlineData("HEALTH")]
    public async Task InvalidAlias_Returns400(string alias)
    {
        (await _client.PostAsJsonAsync("/shorten", new { fullUrl = "https://example.com", customAlias = alias }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_PaginatesWithoutOverlappingPages()
    {
        for (var i = 0; i < 3; i++)
            await _client.PostAsJsonAsync("/shorten", new { fullUrl = "https://example.com", customAlias = $"alias-{i}" });
        var first = (await _client.GetFromJsonAsync<PagedUrls>("/urls?page=1&pageSize=2"))!;
        var second = (await _client.GetFromJsonAsync<PagedUrls>("/urls?page=2&pageSize=2"))!;
        first.Items.Select(u => u.Alias).Should().Equal("alias-2", "alias-1");
        second.Items.Select(u => u.Alias).Should().Equal("alias-0");
        second.TotalCount.Should().Be(3);
        second.TotalPages.Should().Be(2);
        (await _client.GetFromJsonAsync<PagedUrls>("/urls?page=99&pageSize=2"))!.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("page=abc")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=2147483647&pageSize=100")]
    public async Task InvalidPagination_Returns400(string query)
    {
        (await _client.GetAsync($"/urls?{query}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Health_Returns200()
    {
        (await _client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
