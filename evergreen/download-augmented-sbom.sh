#!/usr/bin/env bash

# Environment variables used as input:
# AWS_ACCESS_KEY_ID
# AWS_SECRET_ACCESS_KEY
# AWS_SESSION_TOKEN

declare -r SSDLC_PATH="./artifacts/ssdlc"
mkdir -p "${SSDLC_PATH}"

echo "Downloading augmented sbom using Kondukto"

# use AWS CLI to get the Kondukto API token from AWS Secrets Manager
kondukto_token=$(aws secretsmanager get-secret-value --secret-id "kondukto-token" --region "us-east-1" --query 'SecretString' --output text)
if [ $? -ne 0 ]; then
    exit 1
fi
# set the KONDUKTO_TOKEN environment variable
echo "KONDUKTO_TOKEN=$kondukto_token" > ${PWD}/kondukto_credentials.env

docker run --platform="linux/amd64" --rm -v ${PWD}:/pwd \
  --env-file ${PWD}/kondukto_credentials.env \
  artifactory.corp.mongodb.com/release-tools-container-registry-public-local/silkbomb:2.0 \
  augment --repo mongodb/mongo-csharp-driver --branch main --sbom-in /pwd/sbom.json --sbom-out /pwd/${SSDLC_PATH}/augmented-sbom.json