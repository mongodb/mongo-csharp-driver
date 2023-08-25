#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

if [ -z "$PACKAGE_TARGET" ]; then
  # Use production release tag if nothing was passed
  PACKAGE_TARGET="release"
fi

if [ "${PACKAGE_TARGET}" = "dev" ]; then
  PACKAGE_VERSION_MATCH="v[0-9]*.[0-9]*.[0-9]*-dev[0-9]*"
  PACKAGE_VERSION_EXCLUDE=""
elif [ "${PACKAGE_TARGET}" = "release" ]; then
  PACKAGE_VERSION_MATCH="v[0-9]*.[0-9]*.[0-9]*"
  PACKAGE_VERSION_EXCLUDE="*-dev[0-9]*"
else
  echo "Unexpected value of PACKAGE_TARGET: ${PACKAGE_TARGET}"
fi

PACKAGE_VERSION=$(git describe --tags --abbrev=0 --match="${PACKAGE_VERSION_MATCH}" --exclude="${PACKAGE_VERSION_EXCLUDE}")
PACKAGE_VERSION=$(echo $PACKAGE_VERSION | cut -c 2-)
echo "$PACKAGE_VERSION"
