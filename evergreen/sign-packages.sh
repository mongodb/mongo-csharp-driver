#!/usr/bin/env bash
set -o errexit
set -o pipefail

# Environment variables used as input:
# AUTHENTICODE_KEY_NAME
# AWS_ACCESS_KEY_ID
# AWS_SECRET_ACCESS_KEY
# AWS_SESSION_TOKEN
# GRS_USERNAME
# GRS_PASSWORD
# PACKAGE_VERSION

aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin 901841024863.dkr.ecr.us-east-1.amazonaws.com

echo "GRS_CONFIG_USER1_USERNAME=${GRS_USERNAME}" >> "signing-envfile"
echo "GRS_CONFIG_USER1_PASSWORD=${GRS_PASSWORD}" >> "signing-envfile"

docker run --platform="linux/amd64" --env-file=signing-envfile --rm -v $(pwd):/workdir -w /workdir \
  901841024863.dkr.ecr.us-east-1.amazonaws.com/release-infrastructure/garasign-jsign \
  /bin/bash -c "jsign --tsaurl "http://timestamp.digicert.com" -a ${AUTHENTICODE_KEY_NAME} "./artifacts/nuget/*.$PACKAGE_VERSION.nupkg""
