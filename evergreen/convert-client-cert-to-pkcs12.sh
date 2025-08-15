#!/usr/bin/env bash

set -o errexit  # Exit the script with an error if any of the commands fail

# Input environment variables
: "${CLIENT_PEM_VAR_NAME:="CLIENT_PEM"}"      # Name of the input variable for the client.pem file
: "${OUTPUT_VAR_PREFIX:="MONGO_X509_CLIENT"}"        # Prefix for output environment variables
: "${CERTIFICATE_NAME:="Drivers Client Certificate"}" # Name for the exported certificate

#todo, need to take the input name for client file and password, and not use the convoluted system we have
#I think I need to add those to the input environment variables and then use those in the export down here (where the default values for output variables are)

CLIENT_PEM=${!CLIENT_PEM_VAR_NAME:-nil}
OUT_CLIENT_P12_VAR="${OUTPUT_VAR_PREFIX}_CLIENT_P12"
OUT_CLIENT_PASSWORD_VAR="${OUTPUT_VAR_PREFIX}_CERTIFICATE_PASSWORD"
OUT_CLIENT_PATH_VAR="${OUTPUT_VAR_PREFIX}_CERTIFICATE_PATH"

# Default values for output variables (can be overridden via the environment)
export "${OUT_CLIENT_P12_VAR}"="${!OUT_CLIENT_P12_VAR:-client.p12}"
export "${OUT_CLIENT_PASSWORD_VAR}"="${!OUT_CLIENT_PASSWORD_VAR:-Picard-Alpha-Alpha-3-0-5}"

if [[ "$CLIENT_PEM" == "nil" ]]; then
  echo "Error: ${CLIENT_PEM_VAR_NAME} must be set."
  exit 1
fi

P12_FILENAME=${!OUT_CLIENT_P12_VAR}
CERT_PASSWORD=${!OUT_CLIENT_PASSWORD_VAR}

openssl pkcs12 -export -keypbe PBE-SHA1-3DES -certpbe PBE-SHA1-3DES -macalg sha1 -in "${CLIENT_PEM}" \
  -out "${P12_FILENAME}" \
  -name "${CERTIFICATE_NAME}" \
  -password "pass:${CERT_PASSWORD}"

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

export "${OUT_CLIENT_PATH_VAR}"="${CERT_PATH}"

echo "Exported variables:"
echo "${OUT_CLIENT_P12_VAR}=${!OUT_CLIENT_P12_VAR}"
echo "${OUT_CLIENT_PASSWORD_VAR}=${!OUT_CLIENT_PASSWORD_VAR}"
echo "${OUT_CLIENT_PATH_VAR}=${!OUT_CLIENT_PATH_VAR}"