# Validation — 29 October 2026

Base repository: `tpximpact/code-exercise-c-sharp-incomplete`, main branch downloaded on 29 October 2026.
Base commit: `d909adb658e35952c0d2ee1490bed547b31282ae`.

## Checks completed locally

| Check | Result |
| --- | --- |
| .NET service and HTTP integration tests | 50 passed, 0 failed |
| React component, application and API tests | 26 passed, 0 failed |
| Frontend TypeScript and production Vite build | Passed |
| .NET Release publish | Passed |
| NuGet vulnerability check, including transitive dependencies | No vulnerable packages reported by the configured NuGet source |
| npm dependency audit | Zero vulnerabilities reported |
| Live HTTP smoke script | Health, creation, redirect, listing and persistent deletion passed |
| Live Chrome browser test | Custom/automatic creation, duplicate error and retained form values, redirect, newest-first ordering, paging, last-page deletion and persistence after refresh passed |
| Responsive browser check | Desktop and 390px mobile inspected; no page overflow or JavaScript exceptions |
| Compose and workflow YAML parsing | Passed |
| Shell smoke-script syntax and Git whitespace checks | Passed |

The first frontend baseline failed because the table component was absent and client validation was missing. Its existing passing service tests were retained and updated for the deliberately changed pagination contract. The supplied backend test expectations were corrected where they disagreed with the stated `201`/`302` contract; the new tests exercise actual SQLite persistence and constraints.

Environment: macOS ARM64, .NET SDK 8.0.427, Node 24.22.0, Chrome, SQLite. Browser testing used an isolated temporary SQLite database, not personal data.

## Remaining verification limitation

Docker Engine/Desktop is not installed on the test machine. The Dockerfiles and Compose file have been reviewed and the Compose YAML parsed, but **the container images have not been built or run locally**. The included CI workflow runs `docker compose up --build --detach --wait` and the smoke script on a Docker-enabled runner. A hosted workflow run is still pending.

The local browser and smoke tests validate the application and Vite proxy, not the Nginx image or its filesystem permissions. Run the documented Docker command before treating container verification as complete.
