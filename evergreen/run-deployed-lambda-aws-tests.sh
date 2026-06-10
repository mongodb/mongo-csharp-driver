#!/usr/bin/env bash

set -o errexit  # Exit the script with error if any of the commands fail

# SAM CLI installs the "Amazon.Lambda.Tools" tool in this location so we need to add it to our PATH
export PATH="$PATH:$HOME/.dotnet/tools"

. ${DRIVERS_TOOLS}/.evergreen/aws_lambda/run-deployed-lambda-aws-tests.sh
