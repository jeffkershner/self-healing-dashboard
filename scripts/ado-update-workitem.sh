#!/usr/bin/env bash
# Adds a comment to a work item and optionally changes its state.
# Usage: scripts/ado-update-workitem.sh <id> --comment "text" [--state Active|New|Resolved]
# Needs: AZDO_ORG, AZDO_PROJECT, AZDO_PAT
set -euo pipefail
id="${1:?work item id}"; shift
comment=""; state=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --comment) comment="$2"; shift 2 ;;
    --state) state="$2"; shift 2 ;;
    *) echo "unknown arg: $1" >&2; exit 2 ;;
  esac
done
: "${AZDO_ORG:?}" "${AZDO_PROJECT:?}" "${AZDO_PAT:?}"

ops="[]"
if [[ -n "$comment" ]]; then
  ops=$(jq -c --arg v "$comment" '. + [{op:"add", path:"/fields/System.History", value:$v}]' <<<"$ops")
fi
if [[ -n "$state" ]]; then
  ops=$(jq -c --arg v "$state" '. + [{op:"add", path:"/fields/System.State", value:$v}]' <<<"$ops")
fi

curl -fsS -u ":${AZDO_PAT}" -X PATCH \
  -H "Content-Type: application/json-patch+json" \
  --data "$ops" \
  "https://dev.azure.com/${AZDO_ORG}/${AZDO_PROJECT}/_apis/wit/workitems/${id}?api-version=7.1" \
  | jq -r '"Updated work item \(.id): state=\(.fields["System.State"])"'
