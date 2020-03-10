#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with an error if any of the commands fail

OCSP_TLS_SHOULD_SUCCEED=${OCSP_TLS_SHOULD_SUCCEED:-nil}
OCSP_ALGORITHM=${OCSP_ALGORITHM:-nil}

if [[ "$OS" =~ Windows|windows ]]; then
    certutil.exe -addstore "Root" ${DRIVERS_TOOLS}/.evergreen/x509gen/ca.pem

  if [ "$OCSP_TLS_SHOULD_SUCCEED" != "nil" ] && [ "$OCSP_ALGORITHM" != "nil" ]; then
    certutil.exe -addstore "Root" ${DRIVERS_TOOLS}/.evergreen/ocsp/${OCSP_ALGORITHM}/ca.pem
  fi
fi
