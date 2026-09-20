#!/usr/bin/env bash
# One-time: subscribes Azure DevOps "work item created" (Bugs) to Logic App B's HTTP trigger.
# Usage: scripts/setup-ado-servicehook.sh <resource-group> <logic-app-b-name>
# Needs: AZDO_ORG, AZDO_PROJECT, AZDO_PAT and an authenticated `az` session.
set -euo pipefail
rg="${1:?resource group}"; logicApp="${2:?logic app name}"
: "${AZDO_ORG:?}" "${AZDO_PROJECT:?}" "${AZDO_PAT:?}"

sub=$(az account show --query id -o tsv)
url=$(az rest --method post \
  --url "https://management.azure.com/subscriptions/${sub}/resourceGroups/${rg}/providers/Microsoft.Logic/workflows/${logicApp}/triggers/manual/listCallbackUrl?api-version=2019-05-01" \
  --query value -o tsv)

projectId=$(curl -fsS -u ":${AZDO_PAT}" \
  "https://dev.azure.com/${AZDO_ORG}/_apis/projects/${AZDO_PROJECT}?api-version=7.1" | jq -r .id)

body=$(jq -cn --arg url "$url" --arg projectId "$projectId" '{
  publisherId: "tfs",
  eventType: "workitem.created",
  resourceVersion: "1.0",
  consumerId: "webHooks",
  consumerActionId: "httpRequest",
  publisherInputs: { projectId: $projectId, workItemType: "Bug" },
  consumerInputs: { url: $url, resourceDetailsToSend: "all", messagesToSend: "none", detailedMessagesToSend: "none" }
}')

curl -fsS -u ":${AZDO_PAT}" -X POST -H "Content-Type: application/json" --data "$body" \
  "https://dev.azure.com/${AZDO_ORG}/_apis/hooks/subscriptions?api-version=7.1" \
  | jq -r '"Service hook created: \(.id) (\(.status))"'
