#!/usr/bin/env bash

# Environment variables used as input:
# SILK_CLIENT_ID
# SILK_CLIENT_SECRET

declare -r SSDLC_PATH="./artifacts/ssdlc"
mkdir -p "${SSDLC_PATH}"

echo "Downloading augmented sbom from silk"

docker run --platform="linux/amd64" --rm -v ${PWD}:/pwd \
  -e SILK_CLIENT_ID \
  -e SILK_CLIENT_SECRET \
  artifactory.corp.mongodb.com/release-tools-container-registry-public-local/silkbomb:1.0 \
  download --silk-asset-group mongodb-dotnet-csharp-driver --sbom-out /pwd/${SSDLC_PATH}/augmented-sbom.json
