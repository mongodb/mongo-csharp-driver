#!/bin/bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit  # Exit the script with error if any of the commands fail

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
export AWS_ECS_TEST=true

cd mongo-csharp-driver

./build.sh --target=TestAwsAuthentication
