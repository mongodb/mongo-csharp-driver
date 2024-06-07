#!/usr/bin/env bash

cat << EOF > silkbomb.env
SILK_CLIENT_ID=${SILK_CLIENT_ID}
SILK_CLIENT_SECRET=${SILK_CLIENT_SECRET}
EOF

declare -r SSDLC_PATH="./artifacts/ssdlc"
mkdir -p "${SSDLC_PATH}"

echo "Downloading augmented sbom from silk"

docker run --platform="linux/amd64" --rm -v ${PWD}:/pwd \
  --env-file silkbomb.env \
  artifactory.corp.mongodb.com/release-tools-container-registry-public-local/silkbomb:1.0 \
  download --silk-asset-group mongodb-dotnet-csharp-driver --sbom-out /pwd/${SSDLC_PATH}/augmented-sbom.json
