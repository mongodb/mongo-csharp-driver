#!/usr/bin/env bash

set -o xtrace
set -o errexit  # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#       MONGODB_URI             Set the URI, including username/password to use to connect to the server via MONGODBAWS authentication mechanism
#       OS                      Current operation system
#       ASSERT_NO_URI_CREDS     Determines whether we need assert existence credentials in connection string or not

############################################
#            Main Program                  #
############################################

echo "Running MONGODB-AWS authentication tests"
echo "OS: $OS"

for var in TMP TEMP NUGET_PACKAGES NUGET_HTTP_CACHE_PATH APPDATA; do
  if [[ "$OS" =~ Windows|windows ]]; then
    export $var=z:\\data\\tmp;
  else
    export $var=/data/tmp;
  fi
done

# ensure no secrets are printed in log files
set +x

# load the script
shopt -s expand_aliases # needed for `urlencode` alias
[ -s "${PROJECT_DIRECTORY}/prepare_mongodb_aws.sh" ] && source "${PROJECT_DIRECTORY}/prepare_mongodb_aws.sh"

# Provision the correct connection string
if [ -z ${MONGODB_URI+x} ]; then
    echo "MONGODB_URI is not set";
    exit 1
fi
MONGODB_URI="${MONGODB_URI}/aws?authMechanism=MONGODB-AWS"
if [[ -n ${SESSION_TOKEN} ]]; then
    MONGODB_URI="${MONGODB_URI}&authMechanismProperties=AWS_SESSION_TOKEN:${SESSION_TOKEN}"
fi
if [ "$ASSERT_NO_URI_CREDS" = "true" ]; then
    if echo "$MONGODB_URI" | grep -q "@"; then
        echo "MONGODB_URI unexpectedly contains user credentials!";
        exit 1
    fi
fi

export MONGODB_URI
export AWS_TESTS_ENABLED=true

# show test output
set -x

if [[ "$OS" =~ Windows|windows ]]; then
  powershell.exe .\\build.ps1 --target=TestAwsAuthentication
else
  ./build.sh --target=TestAwsAuthentication
fi