using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using UrlShortener.Api.Data;
using UrlShortener.Api.Middleware;
using UrlShortener.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
    options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(new
    {
        error = string.Join(" ", context.ModelState.Values.SelectMany(v => v.Errors)
            .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid request." : e.ErrorMessage))
    }));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "URL Shortener API", Version = "v1" });
});

// SQLite persistence; database file lives in /data for Docker volume mounting
var dbPath = builder.Configuration["DatabasePath"] ?? "urlshortener.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString()));

var publicBaseUrl = builder.Configuration["PublicBaseUrl"];
if (publicBaseUrl is not null &&
    (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var publicUri) ||
     (publicUri.Scheme != "http" && publicUri.Scheme != "https") ||
     !string.IsNullOrEmpty(publicUri.UserInfo) || !string.IsNullOrEmpty(publicUri.Query) ||
     !string.IsNullOrEmpty(publicUri.Fragment) || publicUri.AbsolutePath != "/"))
    throw new InvalidOperationException("PublicBaseUrl must be an HTTP(S) origin, e.g. https://snip.example.com.");

builder.Services.AddScoped<IUrlShortenerService, UrlShortenerService>();
builder.Services.AddSingleton<IAliasGenerator, RandomAliasGenerator>();

// CORS – allow the React dev server and any configured frontend origin
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000", "http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// --- Ensure DB is created / migrated on startup ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// --- Middleware pipeline ---
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendPolicy");
app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
    await db.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "healthy" }) : Results.StatusCode(503));
app.MapControllers();

app.Run();

// Needed for WebApplicationFactory in integration tests
public partial class Program { }
