#!/usr/bin/env bash
set -eo pipefail

: "${branch_name:?}"
: "${AWS_ACCESS_KEY_ID:?}"
: "${AWS_SECRET_ACCESS_KEY:?}"
: "${AWS_SESSION_TOKEN:?}"

silkbomb="901841024863.dkr.ecr.us-east-1.amazonaws.com/release-infrastructure/silkbomb:2.0"
docker pull "${silkbomb}"

silkbomb_augment_flags=(
  --repo mongodb/mongo-csharp-driver
  --branch "${branch_name}"
  --sbom-in /pwd/sbom.json
  --sbom-out /pwd/augmented.sbom.json.new
  --no-update-sbom-version
)

docker run --rm -v "$(pwd):/pwd" \
  --env 'AWS_ACCESS_KEY_ID' --env 'AWS_SECRET_ACCESS_KEY' --env 'AWS_SESSION_TOKEN' \
  "${silkbomb}" augment "${silkbomb_augment_flags[@]}"

if [ -f ./augmented.sbom.json ]; then
  jq -S 'del(.metadata.timestamp)' ./augmented.sbom.json >|old.json
else
  echo '{}' >|old.json
fi
jq -S 'del(.metadata.timestamp)' ./augmented.sbom.json.new >|new.json

if ! diff -sty --left-column -W 200 old.json new.json >|diff.txt; then
  declare status
  status='{"status":"failed", "type":"test", "should_continue":true, "desc":"detected significant changes in Augmented SBOM"}'
  curl -sS -d "${status}" -H "Content-Type: application/json" -X POST localhost:2285/task_status || true
fi
