#!/usr/bin/env bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit  # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#       SASL_USER               The username to use to connect to the server via PLAIN authentication mechanism
#       SASL_PASS               The password to use to connect to the server via PLAIN authentication mechanism
#       SASL_HOST_BUILD         The host of the server to connect to
#       SASL_PORT               The port of the server to connect to
#       SASL_DB                 The authentication source (authSource) to use for PLAIN authentication

############################################
#            Main Program                  #
############################################

echo "Running PLAIN authentication tests"

if [ -z ${SASL_USER+x} ]; then
    echo "SASL_USER is not set";
    exit 1
fi
if [ -z ${SASL_PASS+x} ]; then
    echo "SASL_PASS is not set";
    exit 1
fi
if [ -z ${SASL_HOST_BUILD+x} ]; then
    echo "SASL_HOST_BUILD is not set";
    exit 1
fi
if [ -z ${SASL_PORT+x} ]; then
    echo "SASL_PORT is not set";
    exit 1
fi
if [ -z ${SASL_DB+x} ]; then
    echo "SASL_DB is not set";
    exit 1
fi
export MONGODB_URI="mongodb://${SASL_USER}:${SASL_PASS}@${SASL_HOST_BUILD}:${SASL_PORT}/ldap?authMechanism=PLAIN&authSource=${SASL_DB}"
export PLAIN_AUTH_TESTS_ENABLED=true

./evergreen/compile-sources.sh
TEST_CATEGORY=PlainMechanism ./evergreen/execute-tests.sh
