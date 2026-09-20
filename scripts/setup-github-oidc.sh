#!/usr/bin/env bash
# One-time: lets GitHub Actions deploy to Azure without stored credentials (OIDC federation).
# Creates an Entra app + service principal, a federated credential for the `production`
# environment of this repo, grants Website Contributor on the resource group, and stores the
# ids as GitHub repository variables.
# Usage: scripts/setup-github-oidc.sh <owner/repo> <resource-group> <webapp-name> <appinsights-resource-id>
# Needs: `az login` and `gh auth login` done.
set -euo pipefail
repo="${1:?owner/repo}"; rg="${2:?resource group}"; webapp="${3:?webapp name}"; appi="${4:?app insights resource id}"

sub=$(az account show --query id -o tsv)
tenant=$(az account show --query tenantId -o tsv)
appName="github-${repo//\//-}-deploy"

appId=$(az ad app list --display-name "$appName" --query '[0].appId' -o tsv)
if [[ -z "$appId" ]]; then
  appId=$(az ad app create --display-name "$appName" --query appId -o tsv)
  az ad sp create --id "$appId" --output none
fi
spId=$(az ad sp show --id "$appId" --query id -o tsv)

# GitHub presents the OIDC subject with owner and repo ids embedded
# (repo:<owner>@<ownerId>/<name>@<repoId>:environment:production), so register that form
# as well as the plain one.
ownerId=$(gh api "repos/${repo}" --jq '.owner.id'); repoId=$(gh api "repos/${repo}" --jq '.id')
owner="${repo%%/*}"; name="${repo##*/}"
for entry in "github-production|repo:${repo}:environment:production" \
             "github-production-ids|repo:${owner}@${ownerId}/${name}@${repoId}:environment:production"; do
  az ad app federated-credential create --id "$appId" --parameters "$(jq -cn \
    --arg name "${entry%%|*}" --arg subject "${entry#*|}" '{
    name: $name, issuer: "https://token.actions.githubusercontent.com",
    subject: $subject, audiences: ["api://AzureADTokenExchange"] }')" --output none 2>/dev/null || true
done

az role assignment create --assignee-object-id "$spId" --assignee-principal-type ServicePrincipal \
  --role "Website Contributor" --scope "/subscriptions/${sub}/resourceGroups/${rg}" --output none
# Needed for the release annotation call.
az role assignment create --assignee-object-id "$spId" --assignee-principal-type ServicePrincipal \
  --role "Monitoring Contributor" --scope "$appi" --output none

gh variable set AZURE_CLIENT_ID --repo "$repo" --body "$appId"
gh variable set AZURE_TENANT_ID --repo "$repo" --body "$tenant"
gh variable set AZURE_SUBSCRIPTION_ID --repo "$repo" --body "$sub"
gh variable set AZURE_WEBAPP_NAME --repo "$repo" --body "$webapp"
gh variable set APPINSIGHTS_RESOURCE_ID --repo "$repo" --body "$appi"
echo "GitHub OIDC deploy identity ready: $appId"
