#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with an error if any of the commands fail

if [[ "$OS" =~ Windows|windows ]]; then
    certutil.exe -addstore "Root" ${DRIVERS_TOOLS}/.evergreen/x509gen/ca.pem
fi
