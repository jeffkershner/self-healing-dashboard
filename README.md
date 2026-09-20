# Self-Healing Dashboard

A demo of a closed loop: **an exception in production becomes a bug, an agent fixes it, the merge
ships a new version.**

```
 merge PR ──► GitHub Actions: tag vX.Y.Z · GitHub Release · deploy ──► Azure App Service
                                                                            │
 Chaos page calls a real endpoint with bad input ──► exception ──► Application Insights
                                                                            │
 Azure Monitor alert (every 5 min) ──► Logic App A ──► Bug in Azure DevOps (tag: agent-fix)
                                                                            │
 Azure DevOps service hook ──► Logic App B ──► GitHub repository_dispatch ──► agent-fix workflow
                                                                            │
 Claude Code: failing test → fix → PR "fix: … (AB#123)" ──► human merges ──► back to the top
```

Stack: Blazor WebAssembly · ASP.NET Core minimal API · SignalR · Microsoft Entra ID (MSAL) ·
Azure App Service (Linux) · Application Insights · Azure Monitor · Logic Apps · Azure Boards ·
GitHub Actions · Claude Code GitHub Action · Bicep + azd.

## Run locally

```bash
scripts/dev.sh        # http://localhost:5027 — Development auth mode, no Azure needed
dotnet test Dashboard.slnx
```

The dashboard streams synthetic metrics over SignalR. The **Chaos** page lists four scenarios;
each calls a real endpoint with an input the current code does not handle, and shows the 500.

## Setup checklist (one time, needs a browser)

Nothing here is automated because each step needs your own sign-in. Roughly one hour.

### 1. Accounts and CLIs
- [ ] `brew install azure-cli azd gh` (done on this Mac) and the .NET 10 SDK (`~/.dotnet`).
- [ ] Azure account (free credit). `az login`, `azd auth login`.
- [ ] GitHub repo (public keeps Actions free). `gh auth login`. Push this code to `main`.
- [ ] Repo settings → Actions → General → **Allow GitHub Actions to create and approve pull requests**.
- [ ] Repo settings → General → Pull Requests → only **Allow squash merging**.
- [ ] Branch protection on `main`: require PR, require the `CI` check.

### 2. Microsoft Entra ID (two app registrations)
- [ ] **API** app: Expose an API → Application ID URI `api://<api-client-id>` → add scope `access_as_user`.
- [ ] **Client** app: platform *Single-page application*, redirect URI
      `https://<webapp>.azurewebsites.net/authentication/login-callback` (add
      `http://localhost:5027/authentication/login-callback` too). API permissions → add the
      `access_as_user` scope of the API app → grant admin consent.
- [ ] Put the ids in the repo (they are not secrets):
      `src/Dashboard.Server/appsettings.json` → `AzureAd.TenantId`, `AzureAd.ClientId` (= API app id);
      `src/Dashboard.Client/wwwroot/appsettings.json` → `Authority` (tenant), `ClientId` (= client app id),
      `ApiScope` = `api://<api-client-id>/access_as_user`.

### 3. Azure DevOps
- [ ] Organization + project using the **Agile** process (work item type "Bug").
- [ ] PAT with **Work Items: Read & write** and **Service Connections/Hooks: Read & write** → `AZDO_PAT`.
- [ ] Project settings → GitHub connections → connect the repo (enables `AB#123` linking and auto-resolve).

### 4. Provision Azure
```bash
azd init -e demo            # picks up azure.yaml + infra/
azd env set ENTRA_TENANT_ID <tenant> API_CLIENT_ID <api app id>
azd env set AZDO_ORG <org> AZDO_PROJECT <project> AZDO_PAT <pat>
azd env set GITHUB_REPO <owner/repo> GITHUB_DISPATCH_TOKEN <fine-grained PAT: contents write>
azd env set APP_SERVICE_SKU B1
azd provision               # ~5 min
azd env get-values          # note AZURE_WEBAPP_NAME, APPINSIGHTS_RESOURCE_ID, LOGIC_APP_B_NAME
```

### 5. Wire GitHub ↔ Azure ↔ Azure DevOps
```bash
scripts/setup-github-oidc.sh <owner/repo> rg-demo <AZURE_WEBAPP_NAME> <APPINSIGHTS_RESOURCE_ID>
scripts/setup-ado-servicehook.sh rg-demo <LOGIC_APP_B_NAME>
gh secret set AZDO_PAT --body "<pat>"
gh variable set AZDO_ORG --body "<org>"; gh variable set AZDO_PROJECT --body "<project>"
claude setup-token          # then:
gh secret set CLAUDE_CODE_OAUTH_TOKEN --body "<token>"
gh secret set GH_PR_TOKEN --body "<fine-grained PAT: contents + pull requests write>"
```
`GH_PR_TOKEN` matters: PRs opened with the built-in `GITHUB_TOKEN` do not trigger the CI workflow.

### 6. First release
Push to `main` (or merge a PR). The **Release** workflow tags `v0.1.1`, creates a GitHub Release,
deploys, annotates App Insights, and smoke-tests `/api/version`.

## Demo script

1. Open the app, sign in, watch the live chart. Footer shows the version.
2. **Chaos** → trigger *Summary over an empty window*. The page shows HTTP 500.
3. Application Insights → Failures: the exception is there within ~2 min.
4. Within ~5–8 min the alert fires and Logic App A files a Bug (Boards → Work items, tag `agent-fix`).
   In a hurry? App Insights → the exception → **Create work item** does the same by hand.
5. The service hook fires Logic App B → GitHub Actions **Agent fix** runs. Watch it in the Actions tab.
6. A PR appears: `fix: … (AB#<id>)`, with a regression test, CI green. The bug gets a comment with the link.
7. Merge it. **Release** bumps the patch version and deploys. Azure Boards resolves the bug.
8. Trigger the same scenario again: HTTP 200. Move on to the next scenario for the next run.

## Versioning and deployment history

- Version = git tag, computed by `mathieudutour/github-tag-action` from Conventional Commit messages
  on `main`. Built into the binary via `-p:Version`, shown in the footer, stamped on every telemetry item.
- Deployment history = GitHub Releases (auto-generated notes list the PRs) + App Insights release annotations.

## Cost

B1 App Service (~$13/mo, from the free credit) is the only meaningful line item. Everything else is
within free tiers at demo volume. `azd down` removes it all; `APP_SERVICE_SKU=F1` is free but allows
only 5 WebSocket connections and no Always On.
