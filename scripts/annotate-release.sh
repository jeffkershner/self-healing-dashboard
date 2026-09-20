#!/usr/bin/env bash
# Writes a deployment annotation to Application Insights so charts show when each version went live.
# Usage: scripts/annotate-release.sh <tag> <sha> <actor>
# Needs: APPINSIGHTS_RESOURCE_ID and an authenticated `az` session.
set -euo pipefail
tag="${1:?tag}"; sha="${2:?sha}"; actor="${3:-ci}"
: "${APPINSIGHTS_RESOURCE_ID:?}"

id=$(uuidgen | tr '[:upper:]' '[:lower:]')
now=$(date -u +%Y-%m-%dT%H:%M:%SZ)
props=$(jq -cn --arg tag "$tag" --arg sha "$sha" --arg actor "$actor" \
  '{ReleaseName:$tag, Commit:$sha, TriggerBy:$actor, Source:"GitHub Actions"}')
body=$(jq -cn --arg id "$id" --arg tag "$tag" --arg now "$now" --arg props "$props" \
  '{Id:$id, AnnotationName:$tag, EventTime:$now, Category:"Deployment", Properties:$props}')

az rest --method put \
  --url "https://management.azure.com${APPINSIGHTS_RESOURCE_ID}/Annotations?api-version=2015-05-01" \
  --body "$body" --output none
echo "Annotated ${tag} at ${now}"
