#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with an error if any of the commands fail

if [[ "$OS" =~ Windows|windows ]]; then
    certutil.exe -delstore "Root" \
    `openssl x509 -noout -text -in ${DRIVERS_TOOLS}/.evergreen/x509gen/ca.pem\
        | grep Serial\
        | sed 's/.*(0x\(.*\))/\1/'`
fi
