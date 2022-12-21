#!/bin/bash

set -o xtrace
set -o errexit  # Exit the script with error if any of the commands fail

############################################
#            Main Program                  #
############################################

# Supported/used environment variables:
#       DRIVERS_TOOLS                                          The path to evergreeen tools
#       OIDC_AWS_*                                             Required OIDC_AWS_* env variables must be configured
#
# Environment variables used as output:
#       OIDC_TESTS_ENABLED                                     Allows running OIDC tests
#       OIDC_TOKEN_DIR                                         The path to generated OIDC AWS tokens
#       AWS_WEB_IDENTITY_TOKEN_FILE                            The path to AWS token for device workflow

if [ -z ${DRIVERS_TOOLS+x} ]; then
    echo "DRIVERS_TOOLS. is not set";
    exit 1
fi

if [ -z ${OIDC_AWS_ROLE_ARN+x} ]; then
    echo "OIDC_AWS_ROLE_ARN. is not set";
    exit 1
fi

if [ -z ${OIDC_AWS_SECRET_ACCESS_KEY+x} ]; then
    echo "OIDC_AWS_SECRET_ACCESS_KEY. is not set";
    exit 1
fi

if [ -z ${OIDC_AWS_ACCESS_KEY_ID+x} ]; then
    echo "OIDC_AWS_ACCESS_KEY_ID. is not set";
    exit 1
fi

export AWS_ROLE_ARN=${OIDC_AWS_ROLE_ARN}
export AWS_SECRET_ACCESS_KEY=${OIDC_AWS_SECRET_ACCESS_KEY}
export AWS_ACCESS_KEY_ID=${OIDC_AWS_ACCESS_KEY_ID}
export OIDC_FOLDER=${DRIVERS_TOOLS}/.evergreen/auth_oidc
export OIDC_TOKEN_DIR=${OIDC_FOLDER}/test_tokens
export AWS_WEB_IDENTITY_TOKEN_FILE=${OIDC_TOKEN_DIR}/test1
export OIDC_TESTS_ENABLED=true

echo "Configuring OIDC server for local authentication tests"

cd ${OIDC_FOLDER}
DRIVERS_TOOLS=${DRIVERS_TOOLS} ./oidc_get_tokens.sh
