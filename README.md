## Coding Assessment Tasks

### Intro
This is a URL shortener that has been created by a junior engineer. Your task is to review the solution, check that it works and find any errors they may have made as well as complete the fixes and TODOs listed below.

This solution has been pre-seeded with data in the database already.

Note that these are not necessarily in the order you will need to address them.

### Fixes

- **Fix Delete Behaviour:** The client has reported strange behaviour whenc clicking on delete on a URL, investigate and fix the issue
- **Generate Alias:** Implement the ```generateAlias``` function
- **UI Implementation:** Build out the UI to display the URLs on the main page

### TODO

- Add paging for for the retrieval of the URLs
- Sort URLs by their creation date
- Create the Docker commands to run the solution
- Replace this ReadMe with an appropriate one

---


# Snip – URL Shortener

A full-stack URL shortener built with **C# / ASP.NET Core 8** (backend) and **React + TypeScript** (frontend), containerised with Docker.

---

## Architecture

```
frontend/      React + TypeScript (Vite)
backend/
  UrlShortener.Api/      ASP.NET Core 8 Web API + SQLite (EF Core)
  UrlShortener.Tests/    xUnit unit + integration tests
docker-compose.yml       Orchestrates both services
```

The frontend proxies API calls through Nginx in production, so there are no CORS concerns once containerised. In development, Vite's dev server proxies to the local API.

---

## API reference

The API matches the provided `openapi.yaml` contract exactly.

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/shorten` | Create a shortened URL |
| `GET` | `/urls` | List all shortened URLs |
| `GET` | `/{alias}` | Redirect to the full URL |
| `DELETE` | `/{alias}` | Delete a shortened URL |

### POST /shorten

```json
// Request
{
  "fullUrl": "https://example.com/very/long/path",
  "customAlias": "my-alias"   // optional
}

// Response 201
{
  "alias": "my-alias",
  "fullUrl": "https://example.com/very/long/path",
  "shortUrl": "http://localhost:8080/my-alias"
}
```

Returns `400` if the URL is invalid, the alias is malformed, or the alias is already taken.

### GET /urls

```json
// Response 200
[
  {
    "alias": "my-alias",
    "fullUrl": "https://example.com/very/long/path",
    "shortUrl": "http://localhost:8080/my-alias"
  }
]
```

### GET /{alias}

Returns `302 Redirect` to the full URL, or `404` if the alias doesn't exist.

### DELETE /{alias}

Returns `204 No Content` on success, `404` if alias not found.

---

## Design decisions & assumptions

**Persistence** — SQLite via EF Core. Zero-dependency, file-based, and entirely sufficient for this exercise. The database file path is configurable via `DatabasePath` in `appsettings.json` or an environment variable, and is volume-mounted in Docker so data survives container restarts.

**Alias validation** — Aliases must be 2–64 characters, containing only letters, numbers, and hyphens. This mirrors typical URL shortener conventions and avoids characters that are awkward in URLs.

**Random alias generation** — 

**Error handling** — A global exception middleware catches unhandled exceptions and returns a clean JSON `{ "error": "..." }` envelope. Domain errors (duplicate alias, invalid input) are surfaced as typed exceptions from the service layer, translated to 400s in the controller.

**Testing** — The backend has both unit tests (service layer, in-memory EF) and integration tests (`WebApplicationFactory` + in-memory EF, full HTTP stack). The frontend has component tests (React Testing Library) and service layer tests (mocked fetch).

**Frontend** — Intentionally lightweight: a custom hook (`useUrlShortener`) owns all state and API interaction; components are purely presentational. No external state management library is needed at this scale.

**CORS** — Allowed origins are configured in `appsettings.json`. In the Docker Compose setup, the Nginx reverse proxy means the frontend and API appear on the same origin to the browser, so CORS is only relevant in local development.
