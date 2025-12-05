#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

FRAMEWORK=${FRAMEWORK:-net6.0}
CONFIGURATION=${CONFIGURATION:-'Release'}

CONFIGURATION=${CONFIGURATION} ./evergreen/compile-sources.sh
CONFIGURATION=${CONFIGURATION} TEST_CATEGORY="!Integration" ./evergreen/execute-tests.sh
