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

# EG scripts for ECS assume that a root folder is "src" and all driver side scripts are placed in ".evergreen" folder.
# So that script is copied into "src/.evergreen" before running
cd src

if [[ -z "$1" ]]; then
    echo "usage: $0 <MONGODB_URI>"
    exit 1
fi

export MONGODB_URI="$1"
# The container does not inherit the Evergreen task environment, so the framework is passed in a file next to this script
export FRAMEWORK=$(<.evergreen/framework)
. ./evergreen/install-dotnet.sh

if echo "$MONGODB_URI" | grep -q "@"; then
  echo "MONGODB_URI unexpectedly contains user credentials in ECS test!";
  exit 1
fi
export AWS_TESTS_ENABLED=true
export AWS_ECS_ENABLED=true

# dotnet test console output does not reach the Evergreen log from the ECS container, so report the junit results here
test_status=0
TEST_CATEGORY=AwsMechanism ./evergreen/execute-tests.sh || test_status=$?

results=$(grep -ho '<testcase [^>]*>\|<failure [^>]*>\|<skipped[^>]*>' build/test-results/*.xml | awk '
  function report() { if (name != "") print status ": " name detail }
  /^<testcase / { report(); match($0, / name="[^"]*"/); name = substr($0, RSTART + 7, RLENGTH - 8); status = "Passed"; detail = "" }
  /^<failure /  { status = "Failed"; match($0, /message="[^"]*"/); detail = " (" substr($0, RSTART + 9, RLENGTH - 10) ")" }
  /^<skipped/   { status = "Skipped" }
  END           { report() }')

echo "ECS test results:"
[ -n "$results" ] && echo "$results" | sed 's/^/  /'
tests_run=$(echo "$results" | grep -c . || true)
tests_failed=$(echo "$results" | grep -c '^Failed' || true)
echo "  Total: $tests_run, failed: $tests_failed"
if [ "$tests_run" -eq 0 ]; then
  echo "No tests ran in the ECS container"
  exit 1
fi

exit $test_status
