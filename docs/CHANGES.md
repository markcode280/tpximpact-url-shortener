# Change notes

## Fixes

- Deletion now removes the entity and awaits `SaveChangesAsync` before returning `204`. Previously, the save ran before removal and was not awaited. The React client hid the row locally, so the fault only became apparent after a refresh.
- Automatic aliases are eight random base-62 characters. The generator is injectable so collision and retry tests do not depend on chance.
- `POST /shorten` now calls the service and returns `201`, the response body and a `Location` header. The supplied action returned an empty `200`.
- The missing URL table is implemented, including destination links, creation dates and delete controls. The form now validates input and retains it when a request fails.

## Pagination and ordering

`GET /urls` accepts one-based `page` and `pageSize` parameters and returns items with pagination metadata. Page sizes are limited to 100. Sorting uses creation date descending, then ID descending to resolve ties. Paging is applied in the database.

This changes the original array response into a page envelope; the React API client was updated with it. After creation the client loads the first page. After deletion it refreshes the current page, moving back when the last page disappears. Aborted requests cannot replace newer list data.

## Validation and collisions

Destinations must be absolute HTTP(S) URLs without embedded credentials. Aliases use 2–64 ASCII letters, digits or hyphens; application route names are reserved. Existing custom aliases return `400`, preserving the original documented contract.

The initial alias lookup handles the common case. The unique database index remains authoritative if another request inserts the alias before the save. A conflicting automatic alias is retried, while a conflicting custom alias returns a readable error. Automatic retries stop after five attempts.

Redirects remain `302`: a permanent cached redirect could continue to work after its link has been deleted.

## Running and testing

Docker Compose starts the API and Nginx frontend, waits for health checks and persists SQLite in a named volume. The README contains local and Docker commands. A shell smoke test checks health, creation, listing, redirection and deletion.

The backend tests use SQLite to exercise persistence and unique constraints. Frontend tests cover forms, list rendering, paging, retries, failed mutations and stale responses. Incorrect expectations in the supplied tests were corrected against the documented HTTP contract.

Dependency updates address the vulnerabilities reported by npm and NuGet while retaining .NET 8 and React 18. See [validation results](VALIDATION.md) for the recorded checks, including the outstanding Docker run.

## Scope

The existing application structure and SQLite storage are retained. Offset pagination is sufficient for this interface; a cursor would be preferable for a large dataset with frequent inserts. Public deployment would also require access control, abuse prevention, operational monitoring and database migrations.
