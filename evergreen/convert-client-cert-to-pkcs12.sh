#!/usr/bin/env bash

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

CLIENT_NO_USER_PEM=${CLIENT_NO_USER_PEM:-nil}
MONGO_X509_CLIENT_NO_USER_P12=${MONGO_X509_CLIENT_NO_USER_P12:-client_no_user.p12}
MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PASSWORD=${MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PASSWORD:-Picard-Alpha-Alpha-3-0-5}

function create_p12 {
  local PEM_FILE=$1
  local P12_FILE=$2
  local PASSWORD=$3

  if [[ ! -f "$PEM_FILE" ]]; then
    echo "Warning: PEM file '$PEM_FILE' does not exist. Skipping generation of '$P12_FILE'."
    return 1
  fi

  openssl pkcs12 -export -keypbe PBE-SHA1-3DES -certpbe PBE-SHA1-3DES -macalg sha1 -in "$PEM_FILE" \
    -out "$P12_FILE" \
    -name "Drivers Client Certificate" \
    -password "pass:${PASSWORD}"
}

function get_realpath {
  local FILE=$1
  if [[ "$OS" =~ MAC|Mac|mac ]]; then
    # realpath function for Mac OS
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

  if [[ "$OS" =~ Windows|windows ]]; then
    echo "$(cygpath -w "$FILE")"
  else
    echo "$(realpath "$FILE")"
  fi
}

# Create the primary client's p12 certificate if the PEM file exists
if create_p12 "$CLIENT_PEM" "$MONGO_X509_CLIENT_P12" "$MONGO_X509_CLIENT_CERTIFICATE_PASSWORD"; then
  MONGO_X509_CLIENT_CERTIFICATE_PATH=$(get_realpath "$MONGO_X509_CLIENT_P12")
  export MONGO_X509_CLIENT_CERTIFICATE_PATH
  export MONGO_X509_CLIENT_CERTIFICATE_PASSWORD
  echo "Primary certificate path: $MONGO_X509_CLIENT_CERTIFICATE_PATH"
  echo "Primary certificate password: $MONGO_X509_CLIENT_CERTIFICATE_PASSWORD"
else
  echo "Skipping primary certificate creation."
fi

# Create the secondary "No User" client's p12 certificate if the PEM file exists
if create_p12 "$CLIENT_NO_USER_PEM" "$MONGO_X509_CLIENT_NO_USER_P12" "$MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PASSWORD"; then
  MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PATH=$(get_realpath "$MONGO_X509_CLIENT_NO_USER_P12")
  export MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PATH
  export MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PASSWORD
  echo "Secondary ('No User') certificate path: $MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PATH"
  echo "Secondary ('No User') certificate password: $MONGO_X509_CLIENT_NO_USER_CERTIFICATE_PASSWORD"
else
  echo "Skipping secondary ('No User') certificate creation."
fi