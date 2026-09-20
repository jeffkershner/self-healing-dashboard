#!/usr/bin/env bash
# Prints an Azure DevOps work item as JSON.
# Usage: scripts/ado-get-workitem.sh <id>
# Needs: AZDO_ORG, AZDO_PROJECT, AZDO_PAT
set -euo pipefail
id="${1:?work item id}"
: "${AZDO_ORG:?}" "${AZDO_PROJECT:?}" "${AZDO_PAT:?}"
curl -fsS -u ":${AZDO_PAT}" \
  "https://dev.azure.com/${AZDO_ORG}/${AZDO_PROJECT}/_apis/wit/workitems/${id}?api-version=7.1"
