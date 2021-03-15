#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with an error if any of the commands fail

# Supported/used environment variables:
#     SSL                     Set to enable SSL. Values are "ssl" / "nossl" (default)
#     OCSP_TLS_SHOULD_SUCCEED Set to test OCSP. Values are true/false/nil
#     OCSP_ALGORITHM          Set to test OCSP. Values are rsa/ecdsa/nil

SSL=${SSL:-nossl}
OCSP_TLS_SHOULD_SUCCEED=${OCSP_TLS_SHOULD_SUCCEED:-nil}
OCSP_ALGORITHM=${OCSP_ALGORITHM:-nil}

if [[ "$SSL" != "ssl" ]]; then
  exit 0
fi

if [[ "$OS" =~ Windows|windows ]]; then
    certutil.exe -addstore "Root" ${DRIVERS_TOOLS}/.evergreen/x509gen/ca.pem

  if [[ "$OCSP_TLS_SHOULD_SUCCEED" != "nil" && "$OCSP_ALGORITHM" != "nil" ]]; then
    certutil.exe -addstore "Root" ${DRIVERS_TOOLS}/.evergreen/ocsp/${OCSP_ALGORITHM}/ca.pem
  fi
fi
