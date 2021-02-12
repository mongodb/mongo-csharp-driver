#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with an error if any of the commands fail

# Environment variables used as input:
#   CLIENT_PEM                      Path to mongo -orchestration's client.pem: must be set.
#   MONGO_X509_CLIENT_P12           Filename for client certificate in p12 format
#
# Environment variables produced as output:
#   MONGODB_X509_CLIENT_P12_PATH            Absolute path to client certificate in p12 format
#   MONGO_X509_CLIENT_CERTIFICATE_PASSWORD  Password for client certificate


CLIENT_PEM=${CLIENT_PEM:-nil}
MONGO_X509_CLIENT_P12=${MONGO_X509_CLIENT_P12:-client.p12}
MONGO_X509_CLIENT_CERTIFICATE_PASSWORD=${MONGO_X509_CLIENT_CERTIFICATE_PASSWORD:-Picard-Alpha-Alpha-3-0-5}

if [[ "$CLIENT_PEM" == "nil" ]]; then
  exit 1
fi

openssl pkcs12 -export -in "${CLIENT_PEM}" \
  -out "${MONGO_X509_CLIENT_P12}" \
  -name "Drivers Client Certificate" \
  -password "pass:${MONGO_X509_CLIENT_CERTIFICATE_PASSWORD}"

if [[ "$OS" =~ MAC|Mac|mac ]]; then
  # this function is not available on mac OS
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
MONGO_X509_CLIENT_CERTIFICATE_PATH=$(realpath "${MONGO_X509_CLIENT_P12}")

if [[ "$OS" =~ Windows|windows ]]; then
  MONGO_X509_CLIENT_CERTIFICATE_PATH=$(cygpath -w "${MONGO_X509_CLIENT_CERTIFICATE_PATH}")
fi

export MONGO_X509_CLIENT_CERTIFICATE_PATH
export MONGO_X509_CLIENT_CERTIFICATE_PASSWORD
