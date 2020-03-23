#!/usr/bin/env bash

set -o xtrace
set -o errexit  # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#       MONGODB_URI             Set the URI, including username/password to use to connect to the server via MONGODBAWS authentication mechanism

############################################
#            Main Program                  #
############################################

echo "Running MONGODB-AWS authentication tests"

# Provision the correct connection string and set up SSL if needed
for var in TMP TEMP NUGET_PACKAGES NUGET_HTTP_CACHE_PATH APPDATA; do setx $var z:\\data\\tmp; export $var=z:\\data\\tmp; done

# ensure no secrets are printed in log files
set +x

# load the script
shopt -s expand_aliases # needed for `urlencode` alias
[ -s "${PROJECT_DIRECTORY}/prepare_mongodb_aws.sh" ] && source "${PROJECT_DIRECTORY}/prepare_mongodb_aws.sh"

if [ -z ${MONGODB_URI+x} ]; then
    echo "MONGODB_URI is not set";
    exit 1
fi
MONGODB_URI="${MONGODB_URI}/aws?authMechanism=MONGODB-AWS"
if [[ -n ${SESSION_TOKEN} ]]; then
    MONGODB_URI="${MONGODB_URI}&authMechanismProperties=AWS_SESSION_TOKEN:${SESSION_TOKEN}"
fi
export MONGODB_URI
export AWS_TESTS_ENABLED=true

# show test output
set -x

powershell.exe .\\build.ps1 -target TestAwsAuthentication
