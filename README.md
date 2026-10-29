# Snip — URL shortener

A small full-stack URL shortener using ASP.NET Core 8, EF Core and SQLite, with a React/TypeScript interface. Create automatic or custom aliases, follow redirects, browse links newest first, and delete links permanently.

## Run with Docker

Requires Docker Engine/Desktop and the Docker Compose v2 plugin.

```sh
docker compose up --build --detach --wait
```

Open **http://localhost:3000**. Only the frontend is published, on loopback; Nginx proxies requests to the API over the Compose network. Both image builds run their respective test suites. Health checks wait for the database and proxy to become available.

```sh
bash scripts/smoke-test.sh
docker compose logs --follow
docker compose down
```

The named `url-data` volume preserves SQLite data across container restarts and `docker compose down`. To use a different public origin, change `PublicBaseUrl` and the frontend port binding in `docker-compose.yml` together. For an HTTPS deployment set `PublicBaseUrl` to the external HTTPS origin.

The exercise brief mentions pre-seeded data, but the source repository contains no database or seed routine. A fresh installation therefore starts empty; create links through the form. Existing database rows are retained.

## Run locally

Requires the **.NET 8 SDK**, **Node.js 24 LTS** (or Node 22.12+) and npm. Run these from the repository root in two terminals:

```sh
dotnet run --project backend/UrlShortener.Api --urls http://localhost:8080
```

```sh
cd frontend
npm ci
npm run dev
```

Open **http://localhost:3000**. Vite proxies API and alias requests to port 8080. Short links use the incoming request origin, so links created through the browser point back to port 3000. The frontend displays ten links per page.

SQLite is created automatically. `DatabasePath` defaults to `urlshortener.db` relative to the API's working directory. For a custom path, ensure its parent directory exists and is writable by the API process.

| Configuration | Purpose |
| --- | --- |
| `DatabasePath` | SQLite file path; Compose uses `/data/urlshortener.db` |
| `PublicBaseUrl` | Optional external HTTP(S) origin for generated links, e.g. `https://snip.example.com`; otherwise use request scheme/host |
| `AllowedOrigins` | CORS allowlist for separate-origin clients; same-origin Vite/Nginx proxying does not require CORS |
| `ASPNETCORE_ENVIRONMENT` | Set to `Development` to enable `/swagger` |
| `ASPNETCORE_URLS` | API listening address; Docker uses port 8080 |

## Build and test

From the repository root:

```sh
dotnet test backend/UrlShortener.sln -c Release
dotnet publish backend/UrlShortener.Api -c Release -o artifacts/api
```

From `frontend`:

```sh
npm ci
npm test
npm run build
npm audit
```

Backend service and HTTP integration tests use real SQLite in memory, including its unique constraints. Each test has an isolated database. Tests cover creation, validation, alias collisions (including a race at save time), ordering, pagination, redirects and deletion across contexts. Frontend tests cover the table, validation, API requests, loading/retry, pagination, stale requests, failed mutations and clipboard failures.

The GitHub Actions workflow runs backend and frontend checks plus a Docker Compose build and HTTP smoke test on pushes and pull requests.

## API

| Method | Path | Success |
| --- | --- | --- |
| `POST` | `/shorten` | `201`, JSON link and `Location` header |
| `GET` | `/urls?page=1&pageSize=10` | `200`, paginated JSON |
| `GET` | `/{alias}` | `302`, `Location` points to destination |
| `DELETE` | `/{alias}` | `204`, removal saved before responding |
| `GET` | `/health` | `200` when SQLite is reachable, otherwise `503` |

Create a link:

```sh
curl -i http://localhost:3000/shorten \
  -H 'Content-Type: application/json' \
  -d '{"fullUrl":"https://example.com/long/path","customAlias":"example-link"}'
```

```json
{
  "shortUrl": "http://localhost:3000/example-link",
  "alias": "example-link",
  "fullUrl": "https://example.com/long/path"
}
```

Omit `customAlias` (or leave it blank) for an eight-character random alias. Custom aliases are case-sensitive, contain 2–64 ASCII letters, numbers or hyphens, and cannot be application routes: `urls`, `shorten`, `health`, `swagger`, `assets` (reserved case-insensitively). Destinations must be absolute HTTP(S) URLs, without embedded credentials, up to 2048 characters.

Invalid input, a reserved alias, an existing custom alias, or invalid pagination returns `400` with `{ "error": "..." }`. Duplicate aliases retain `400` to follow the original exercise contract. Exhausting five automatic-alias attempts returns `503`; unexpected errors return a generic `500` and are logged server-side. Missing redirect/deletion aliases return `404`. Redirects are temporary because links can be deleted; permanent browser caching would bypass that behaviour.

Pagination response:

```json
{
  "items": [
    {
      "alias": "example-link",
      "fullUrl": "https://example.com/long/path",
      "shortUrl": "http://localhost:3000/example-link",
      "createdAt": "2026-10-29T10:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1
}
```

Pages are one-based; page size defaults to 10 and is limited to 1–100. Results are ordered by UTC creation date descending, then ID descending to break timestamp ties. A valid page beyond the end returns empty `items` with the requested page and current counts. An empty database has zero total pages. The UI returns to the last available page after a deletion removes a page.

**Contract change:** `/urls` now returns a page envelope instead of the original array. The supplied React client has been updated with it. There is no `openapi.yaml` in the source repository; Swagger is generated from the API instead.

## Design and scope

- The existing controller/service/EF separation and React custom hook are retained. The application does not need an additional state library.
- Aliases use cryptographic randomness over 62 characters. The database unique index protects against concurrent collisions; automatic generation retries up to five times. Aliases are identifiers, not authentication secrets.
- `Skip`/`Take` runs in SQLite, with a creation-date/ID index for ordering. Offset pagination supports page numbers, but inserts between requests can move records between pages. A cursor would suit a larger, rapidly changing dataset.
- Deletion is saved before success is returned. The frontend reloads page/count data after mutations and cancels stale list requests.
- Failed submissions retain form input. Load failures show an error and retry control rather than an empty-list message.
- The API runs as a non-root user in Docker with a persistent data volume. The frontend serves static assets; Node and test tools stay in the build stage.

This is an exercise/demo application with an intentionally shared, unauthenticated link list. Before exposing it publicly, add ownership/authentication, authorisation for deletion, rate limits and abuse controls, trusted-host/TLS configuration, operational monitoring, backups and schema migrations. Do not store private or credential-bearing destination URLs. `EnsureCreated` is appropriate for a fresh exercise database; it does not migrate an existing schema or add new indexes to it.

See [change notes](docs/CHANGES.md) for the fixes and design decisions, and [validation results](docs/VALIDATION.md) for checks performed and remaining verification.
