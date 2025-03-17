#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

# Environment variables used as input:
# ARTIFACTORY_PASSWORD
# ARTIFACTORY_USERNAME
# GRS_USERNAME
# GRS_PASSWORD
# PACKAGE_VERSION

echo "${ARTIFACTORY_PASSWORD}" | docker login --password-stdin --username "${ARTIFACTORY_USERNAME}" artifactory.corp.mongodb.com

echo "GRS_CONFIG_USER1_USERNAME=${GRS_USERNAME}" >> "signing-envfile"
echo "GRS_CONFIG_USER1_PASSWORD=${GRS_PASSWORD}" >> "signing-envfile"

docker run --platform="linux/amd64" --env-file=signing-envfile --rm -v $(pwd):/workdir -w /workdir \
  artifactory.corp.mongodb.com/release-tools-container-registry-local/garasign-jsign \
  /bin/bash -c "jsign --tsaurl "http://timestamp.digicert.com" -a ${AUTHENTICODE_KEY_NAME} "./artifacts/nuget/*.$PACKAGE_VERSION.nupkg""
