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
$PYTHON -u ${DRIVERS_TOOLS}/.evergreen/mongodl.py --component crypt_shared --out ${DRIVERS_TOOLS}/evergreen/csfle --version 6.0.0-rc13

if [[ "$OS" =~ Windows|windows ]]; then
    export CRYPT_SHARED_LIB_PATH="${DRIVERS_TOOLS}/evergreen/csfle/bin/mongo_crypt_v1.dll"
elif [[ "$OS" =~ Mac|mac ]]; then
    export CRYPT_SHARED_LIB_PATH="${DRIVERS_TOOLS}/evergreen/csfle/lib/mongo_crypt_v1.dylib"
else
    export CRYPT_SHARED_LIB_PATH="${DRIVERS_TOOLS}/evergreen/csfle/lib/mongo_crypt_v1.so"
fi

echo "crypt shared library path $CRYPT_SHARED_LIB_PATH"