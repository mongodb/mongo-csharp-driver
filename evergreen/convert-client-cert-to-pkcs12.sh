#!/usr/bin/env bash

set -o errexit  # Exit the script with an error if any of the commands fail

# Input environment variables
: "${CLIENT_PEM_VAR_NAME:="CLIENT_PEM"}"      # Name of the input variable for the client.pem file
: "${OUTPUT_VAR_PREFIX:="MONGO_X509_CLIENT"}"        # Prefix for output environment variables
: "${FRIENDLY_NAME:="Drivers Client Certificate"}" # Friendly name for the exported certificate
: "${P12_FILENAME:="client.p12"}"
: "${P12_PASSWORD:="Picard-Alpha-Alpha-3-0-5"}"
: "${OUT_CLIENT_PASSWORD_VAR:="MONGO_X509_CLIENT_CERTIFICATE_PASSWORD"}"
: "${OUT_CLIENT_PATH_VAR:="MONGO_X509_CLIENT_CERTIFICATE_PATH"}"

CLIENT_PEM=${!CLIENT_PEM_VAR_NAME:-nil}

if [[ "$CLIENT_PEM" == "nil" ]]; then
  echo "Error: ${CLIENT_PEM_VAR_NAME} must be set."
  exit 1
fi

openssl pkcs12 -export -keypbe PBE-SHA1-3DES -certpbe PBE-SHA1-3DES -macalg sha1 -in "${CLIENT_PEM}" \
  -out "${P12_FILENAME}" \
  -name "${FRIENDLY_NAME}" \
  -password "pass:${P12_PASSWORD}"

# Determine path using realpath (compatible across macOS, Linux, and Windows)
if [[ "$OS" =~ MAC|Mac|mac ]]; then
  # Functionality to mimic `realpath` on macOS
  function realpath() {
    OURPWD=$PWD
    cd "$(dirname "$1")"
    LINK=$(readlink "$(basename "$1")")
    while [ "$LINK" ]; do
      cd "$(dirname "$LINK")"
      LINK=$(readlink "$(basename "$1")")
    done
    REALPATH="$PWD/$(basename "$1")"
    cd "$OURPWD"
    echo "$REALPATH"
  }
fi

CERT_PATH=$(realpath "${P12_FILENAME}")

if [[ "$OS" =~ Windows|windows ]]; then
  CERT_PATH=$(cygpath -w "${CERT_PATH}")
fi

export "${OUT_CLIENT_PASSWORD_VAR}"="${P12_PASSWORD}"
export "${OUT_CLIENT_PATH_VAR}"="${CERT_PATH}"

echo "Exported variables:"
echo "${OUT_CLIENT_PASSWORD_VAR}=${!OUT_CLIENT_PASSWORD_VAR}"
echo "${OUT_CLIENT_PATH_VAR}=${!OUT_CLIENT_PATH_VAR}"