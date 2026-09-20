# Self-Healing Dashboard

Blazor WebAssembly dashboard + ASP.NET Core API + SignalR, deployed to Azure App Service, monitored by
Application Insights. Exceptions become Azure DevOps bugs; an agent fixes them via pull request.

## Layout

- `src/Dashboard.Server` – API (minimal APIs in `Endpoints/`), SignalR hub (`Hubs/`), services, auth. Hosts the WASM client.
- `src/Dashboard.Client` – Blazor WASM. Pages: `Home` (live dashboard), `Chaos` (fault triggers).
- `src/Dashboard.Shared` – DTOs shared by both.
- `tests/Dashboard.Tests` – xUnit. `ApiTests` boots the real server with the Development auth mode.
- `infra/` – Bicep (App Service, App Insights, alert rule, two Logic Apps). `azd provision` applies it.
- `.github/workflows/` – `ci.yml` (PRs), `release.yml` (main → tag → deploy), `agent-fix.yml` (bug → PR).
- `scripts/` – Azure DevOps helpers used by CI and the agent.

## Commands

```bash
dotnet build Dashboard.slnx -c Release
dotnet test Dashboard.slnx -c Release
scripts/dev.sh            # run locally on http://localhost:5027 with a fake signed-in user
```

.NET 10 SDK. On this Mac it is installed under `~/.dotnet` (see `~/.zshrc`).

## Conventions

- **Commit and PR titles use Conventional Commits.** `fix:` → patch, `feat:` → minor, `feat!:` or a
  `BREAKING CHANGE:` footer → major. The release workflow computes the version from these, so a
  wrong prefix ships the wrong version.
- **Reference Azure DevOps work items as `AB#<id>`** in the PR body (`Fixes AB#123`). Azure Boards links
  the PR and resolves the bug on merge.
- **Squash merge only.** The PR title becomes the commit message on `main`.
- **Every bug fix adds a regression test** in `tests/Dashboard.Tests` that fails before the fix.
- Fix root causes in `src/`, do not catch-and-ignore. Endpoints should return a sensible result
  (empty, 404, clamped) for bad input rather than 500.
- Do not touch `ChaosCatalog` scenarios or the Chaos page when fixing a bug; they are the demo's triggers.
- Auth has two modes (`Auth:Mode`): `AzureAd` (Entra ID, production) and `Development` (fixed local
  user; server refuses it outside the Development environment). Tests rely on `Development`.
- No database. Metrics are synthetic and in-memory (`MetricsStore`).
