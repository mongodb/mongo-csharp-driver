#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with error if any of the commands fail

# SAM CLI installs the "Amazon.Lambda.Tools" tool in this location so we need to add it to our PATH
export PATH="$PATH:$HOME/.dotnet/tools"

# setting this as SAM CLI seems to check DOTNET_ROOT directly instead of the PATH
export DOTNET_ROOT=$DOTNET_SDK_PATH

. ${DRIVERS_TOOLS}/.evergreen/aws_lambda/run-deployed-lambda-aws-tests.sh