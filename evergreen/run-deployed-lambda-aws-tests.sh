#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with error if any of the commands fail

LOCALTOOLPATH=$TEST_LAMBDA_DIRECTORY/localtool

echo "Installing aws lambda tool to a custom location"
dotnet tool install "Amazon.Lambda.Tools" --tool-path $LOCALTOOLPATH

# makes the "sam build" command use the local version of the lambda tool
export PATH="$PATH:$LOCALTOOLPATH"

. ${DRIVERS_TOOLS}/.evergreen/aws_lambda/run-deployed-lambda-aws-tests.sh

# uninstall global version installed by the "sam build" command
dotnet tool uninstall -g "Amazon.Lambda.Tools"

# uninstall our local version
dotnet tool uninstall "Amazon.Lambda.Tools" --tool-path $LOCALTOOLPATH
