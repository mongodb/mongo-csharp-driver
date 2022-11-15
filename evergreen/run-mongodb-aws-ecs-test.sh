#!/bin/bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit  # Exit the script with error if any of the commands fail

# Environment variables used as input:
#       MONGODB_URI             Set the URI, including username/password to use to connect to the server via MONGODBAWS authentication mechanism
#       ASSERT_NO_URI_CREDS     Determines whether we need assert existence credentials in connection string or not
#
# Environment variables used as output:
#       AWS_TESTS_ENABLED       Allows running AWS tests
#       AWS_ECS_ENABLED         Allows running ECS tests
#
############################################
#            Main Program                  #
############################################

if [[ -z "$1" ]]; then
    echo "usage: $0 <MONGODB_URI>"
    exit 1
fi
export MONGODB_URI="$1"

if echo "$MONGODB_URI" | grep -q "@"; then
  echo "MONGODB_URI unexpectedly contains user credentials in ECS test!";
  exit 1
fi
# Now we can safely enable xtrace
set -o xtrace
export AWS_TESTS_ENABLED=true
export AWS_ECS_ENABLED=true

# EG scripts for ECS assume that a root folder is "src" and all driver side scripts are placed in ".evergreen" folder. 
# So that script is copied into "src/.evergreen" before running
cd src

./build.sh --target=TestAwsAuthentication
