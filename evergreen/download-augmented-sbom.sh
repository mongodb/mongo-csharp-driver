#!/usr/bin/env bash
set -o errexit
set -o pipefail

# Environment variables used as input:
# AWS_ACCESS_KEY_ID
# AWS_SECRET_ACCESS_KEY
# AWS_SESSION_TOKEN
#
# Prerequisite: kondukto_credentials.env must already exist (written by fetch-kondukto-token.sh).

declare -r SSDLC_PATH="./artifacts/ssdlc"
mkdir -p "${SSDLC_PATH}"

echo "Downloading augmented sbom using Kondukto"

aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin 901841024863.dkr.ecr.us-east-1.amazonaws.com

docker run --platform="linux/amd64" --rm -v ${PWD}:/pwd \
  --env-file ${PWD}/kondukto_credentials.env \
  901841024863.dkr.ecr.us-east-1.amazonaws.com/release-infrastructure/silkbomb:2.0 \
  augment --repo mongodb/mongo-csharp-driver --branch main --sbom-in /pwd/sbom.json --sbom-out /pwd/${SSDLC_PATH}/augmented-sbom.json
