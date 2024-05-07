#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

PACKAGE_VERSION=$(git describe --tags)
PACKAGE_VERSION=$(echo $PACKAGE_VERSION | cut -c 2-)
echo "$PACKAGE_VERSION"
