#!/usr/bin/env bash
# Resolves every Azure DevOps work item referenced as AB#<id> in a commit message, once that
# commit has been deployed. Makes the loop independent of the Boards ↔ GitHub integration.
# Usage: scripts/ado-resolve-from-commit.sh <sha> <tag> <repo-url>
# Needs: AZDO_ORG, AZDO_PROJECT, AZDO_PAT
set -euo pipefail
sha="${1:?sha}"; tag="${2:?tag}"; repo="${3:?repo url}"
: "${AZDO_ORG:?}" "${AZDO_PROJECT:?}" "${AZDO_PAT:?}"

ids=$(git log -1 --pretty=%B "$sha" | grep -oE 'AB#[0-9]+' | tr -d 'AB#' | sort -u)
if [[ -z "$ids" ]]; then
  echo "No AB# references in $sha"; exit 0
fi
for id in $ids; do
  msg="Deployed in ${tag} (commit ${sha:0:7}, ${repo}/commit/${sha})."
  scripts/ado-update-workitem.sh "$id" --comment "$msg" --state Resolved || echo "Could not resolve work item $id" >&2
done
