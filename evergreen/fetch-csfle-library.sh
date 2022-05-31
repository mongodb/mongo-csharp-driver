#!/usr/bin/env bash

# Fetch csfle shared library.
#
# Environment variables used as input:
#   OS                                               The current operating system
#   DRIVERS_TOOLS
#
# Environment variables produced as output:
#   MONGODB_CSFLE_PATH                               The MONGODB_CSFLE_PATH path

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with an error if any of the commands fail


PYTHON=$(OS=${OS} ${PROJECT_DIRECTORY}/evergreen/get-python-path.sh)
$PYTHON -u ${DRIVERS_TOOLS}/.evergreen/mongodl.py --component csfle --out ${DRIVERS_TOOLS}/evergreen/csfle --version latest

if [[ "$OS" =~ Windows|windows ]]; then
    export MONGODB_CSFLE_PATH="${DRIVERS_TOOLS}/evergreen/csfle/bin/mongo_csfle_v1.dll"
elif [[ "$OS" =~ Mac|mac ]]; then
    export MONGODB_CSFLE_PATH="${DRIVERS_TOOLS}/evergreen/csfle/lib/mongo_csfle_v1.dylib"
else
    export MONGODB_CSFLE_PATH="${DRIVERS_TOOLS}/evergreen/csfle/lib/mongo_csfle_v1.so"
fi

echo "csfle lib path $MONGODB_CSFLE_PATH"