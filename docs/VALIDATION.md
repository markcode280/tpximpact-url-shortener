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

## Hosted verification

[GitHub Actions run 40869815078](https://github.com/markcode280/tpximpact-url-shortener/actions/runs/40869815078) passed for implementation commit `f2b661d4f04e43ec2d3a7ffd02fb08bbbf7a5531` on 29 October 2026.

| Job | Result |
| --- | --- |
| Backend | Release .NET test suite passed |
| Frontend | Clean dependency installation, tests, TypeScript check and production build passed |
| Containers | Both images built, Compose services became healthy, and the HTTP smoke test passed through Nginx |

The container job ran `docker compose up --build --detach --wait`, followed by `bash scripts/smoke-test.sh`. It checked creation, listing, redirect and persistent deletion against the assembled application. Docker was unavailable on the local Mac, so this check ran on a GitHub-hosted Linux runner instead.
