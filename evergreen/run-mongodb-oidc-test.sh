#!/bin/bash

set -o xtrace
set -o errexit  # Exit the script with error if any of the commands fail

############################################
#            Main Program                  #
############################################

# Supported/used environment variables:
#  OIDC_TOKEN_DIR                    The path for generated tokens
#  PROJECT_DIRECTORY                 The path to the root of driver source
# Environment variables used as output:
#  OIDC_TESTS_ENABLED                Allows running OIDC tests

echo "Running MONGODB-OIDC authentication tests"

# load the script
shopt -s expand_aliases # needed for `urlencode` alias
[ -s "${PROJECT_DIRECTORY}/prepare_mongodb_oidc.sh" ] && source "${PROJECT_DIRECTORY}/prepare_mongodb_oidc.sh"

# use aws device workflow by default
MONGODB_URI="mongodb://localhost/test?authMechanism=MONGODB-OIDC&authMechanismProperties=PROVIDER_NAME:aws"

export MONGODB_URI="$MONGODB_URI"
export OIDC_TOKEN_DIR="$OIDC_TOKEN_DIR"

if [ -z "${OIDC_TOKEN_DIR}" ]; then
    echo "Must specify OIDC_TOKEN_DIR"
    exit 1
fi
export AWS_WEB_IDENTITY_TOKEN_FILE=${AWS_WEB_IDENTITY_TOKEN_FILE:-"${OIDC_TOKEN_DIR}/test_user1"}
export OIDC_TESTS_ENABLED=true

echo "Assigned environment variables:"
echo "MONGODB_URI: $MONGODB_URI"
echo "OIDC_TOKEN_DIR: $OIDC_TOKEN_DIR"
echo "AWS_WEB_IDENTITY_TOKEN_FILE: $AWS_WEB_IDENTITY_TOKEN_FILE"

if [[ "$OS" =~ Windows|windows ]]; then
  powershell.exe .\\build.ps1 --target=TestOidcAuthentication
else
  ./build.sh --target=TestOidcAuthentication
fi
