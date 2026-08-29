#!/usr/bin/env bash
set -eo pipefail

: "${branch_name:?}"
: "${AWS_ACCESS_KEY_ID:?}"
: "${AWS_SECRET_ACCESS_KEY:?}"
: "${AWS_SESSION_TOKEN:?}"

silkbomb="901841024863.dkr.ecr.us-east-1.amazonaws.com/release-infrastructure/silkbomb:2.0"
docker pull "${silkbomb}"

# "upload" publishes sbom.json directly to Dependency-Track and Kondukto in one call, using
# only the Silkbomb IAM role's credentials below -- unlike "augment", it needs no separately
# fetched Kondukto token. Matches the pattern in mongo-go-driver's upload-sbom
# (internal/cmd/upload-sbom/main.go), which was rebuilt on this same subcommand for the same
# reason.
silkbomb_upload_flags=(
  --repo mongodb/mongo-csharp-driver
  --branch "${branch_name}"
  --sbom-in /pwd/sbom.json
)

docker run --rm -v "$(pwd):/pwd" \
  --user "$(id -u):$(id -g)" \
  --env 'AWS_ACCESS_KEY_ID' --env 'AWS_SECRET_ACCESS_KEY' --env 'AWS_SESSION_TOKEN' \
  "${silkbomb}" upload "${silkbomb_upload_flags[@]}"
