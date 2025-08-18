#!/usr/bin/env bash

set -o errexit  # Exit the script with an error if any of the commands fail

# Environment variables used as input:
#   CLIENT_PEM                      Path to mongo client.pem: must be set
#   P12_FILENAME                    Filename for client certificate in p12 format
#   P12_PASSWORD                    Password for client certificate in p12 format
#   FRIENDLY_NAME                   Friendly name for client certificate in p12 format
#   OUT_CLIENT_PATH_VAR             Name of the output variable containing the path of the p12 certificate
#   OUT_CLIENT_PASSWORD_VAR         Name of the output variable containing the password for the p12 certificate
#
# Environment variables produced as output:
#   {!OUT_CLIENT_PATH_VAR}          Absolute path to client certificate in p12 format (OUT_CLIENT_PATH_VAR contains the actual variable being exported)
#   {!OUT_CLIENT_PASSWORD_VAR}      Password for client certificate (OUT_CLIENT_PASSWORD_VAR contains the actual variable being exported)


# Input environment variables and default values
: "${CLIENT_PEM:=nil}"
: "${FRIENDLY_NAME:="Drivers Client Certificate"}"
: "${P12_FILENAME:="client.p12"}"
: "${P12_PASSWORD:="Picard-Alpha-Alpha-3-0-5"}"
: "${OUT_CLIENT_PATH_VAR:="MONGO_X509_CLIENT_CERTIFICATE_PATH"}"
: "${OUT_CLIENT_PASSWORD_VAR:="MONGO_X509_CLIENT_CERTIFICATE_PASSWORD"}"

if [[ "$CLIENT_PEM" == "nil" ]]; then
  echo "Error: CLIENT_PEM must be set."
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

# Output environment variables
export "${OUT_CLIENT_PASSWORD_VAR}"="${P12_PASSWORD}"
export "${OUT_CLIENT_PATH_VAR}"="${CERT_PATH}"

echo "Exported variables:"
echo "${OUT_CLIENT_PASSWORD_VAR}=${!OUT_CLIENT_PASSWORD_VAR}"
echo "${OUT_CLIENT_PATH_VAR}=${!OUT_CLIENT_PATH_VAR}"