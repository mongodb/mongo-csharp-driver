#!/usr/bin/env bash

set -o errexit  # Exit the script with error if any of the commands fail

export ENABLE_CSOT_PENDING_RESPONSE_TESTS=1
export MONGODB_URI="mongodb://127.0.0.1:28017/?directConnection=true"

./build.sh --target=CSOTPendingResponseNet60
