#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with an error if any of the commands fail

# Supported/used environment variables:
#     OCSP_TLS_SHOULD_SUCCEED Set to test OCSP. Values are true/false/nil
#     OCSP_ALGORITHM          Set to test OCSP. Values are rsa/ecdsa/nil
#     OS                      Set to access operating system

OCSP_TLS_SHOULD_SUCCEED=${OCSP_TLS_SHOULD_SUCCEED:-nil}
OCSP_ALGORITHM=${OCSP_ALGORITHM:-nil}

function make_trusted() {
  echo "CA.pem certificate $1"
  if [[ "$OS" =~ Windows|windows ]]; then
    # makes the client.pem trusted
    certutil.exe -addstore "Root" $1
  elif  [[ "$OS" =~ Ubuntu|ubuntu ]]; then
    # makes the client.pem trusted
    # note: .crt is the equivalent format as .pem, but we need to make this renaming because update-ca-certificates supports only .crt
    sudo cp -f $1 /usr/local/share/ca-certificates/ca.crt
    sudo update-ca-certificates
  elif [[ "$OS" =~ macos ]]; then
    # mac OS, the same trick as for above ubuntu step
    sudo cp -f $1 ~/ca.crt
    sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain ~/ca.crt
  else
    echo "Unsupported OS:${OS}" 1>&2 # write to stderr
    exit 1
  fi
}

make_trusted ${DRIVERS_TOOLS}/.evergreen/x509gen/ca.pem

if [[ "$OCSP_TLS_SHOULD_SUCCEED" != "nil" && "$OCSP_ALGORITHM" != "nil" ]]; then
  make_trusted ${DRIVERS_TOOLS}/.evergreen/ocsp/${OCSP_ALGORITHM}/ca.pem
fi
