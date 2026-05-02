#!/usr/bin/env bash
set -o errexit # Exit the script with error if any of the commands fail
set +o xtrace  # Disable tracing.

# Environment variables used as inpu
# NUGET_SIGN_CERTIFICATE_FINGERPRINT
# PACKAGES_SOURCE
# PACKAGES_SOURCE_KEY
# PACKAGE_VERSION

# querying nuget source to find search base url
packages_search_url=$(curl -X GET -s "${PACKAGES_SOURCE}" | jq -r 'first(.resources[] | select(."@type"=="SearchQueryService") | ."@id")')

execute_with_retry() {
  max_attempts=$1
  shift  # Shift removes the first argument ($1), so $@ becomes the command
  attempt=1

  until "$@"; do
    ((attempt++))

    if [ $attempt -gt $max_attempts ]; then
      echo "Command failed after $max_attempts attempts." >&2
      exit 1
    fi

    delay=$((attempt * 10))
    echo "Command failed. Sleeping for ${delay} seconds..."
    sleep $delay
  done
}

check_package_version_available() {
  package=$1
  version=$2
  response=$(curl -X GET -s "$packages_search_url?prerelease=true&take=1&q=PackageId:$package")
  echo "$response" | jq -e --arg v "$version" 'any(.data[0].versions[]?; .version == $v)' > /dev/null 2>&1
}

wait_until_package_is_available() {
  package=$1
  version=$2
  echo "Checking package availability: ${package}:${version} at ${packages_search_url}"
  execute_with_retry 10 check_package_version_available "$package" "$version"
  echo "Package ${package} is available, version: ${version}"
}

if [ -z "$PACKAGES_SOURCE" ]; then
  echo "PACKAGES_SOURCE variable should be set"
  exit 1
fi

if [ -z "$PACKAGES_SOURCE_KEY" ]; then
  echo "PACKAGES_SOURCE_KEY variable should be set"
  exit 1
fi

if [ -z "$PACKAGE_VERSION" ]; then
  echo "PACKAGE_VERSION variable should be set"
  exit 1
fi

clear_version_rx='^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9]+)?$'
if [ "$PACKAGES_SOURCE" = "https://api.nuget.org/v3/index.json" ] && [[ ! "$PACKAGE_VERSION" =~ $clear_version_rx ]]; then
  echo "Cannot push dev version to nuget.org: '$PACKAGE_VERSION'"
  exit 1
fi

source ./evergreen/packages.sh

for package in ${PACKAGES[*]}; do
  dotnet nuget verify ./artifacts/nuget/"$package"."$PACKAGE_VERSION".nupkg --certificate-fingerprint "$NUGET_SIGN_CERTIFICATE_FINGERPRINT"
done

for package in ${PACKAGES[*]}; do
  echo "Pushing package: ${package}:${PACKAGE_VERSION}"
  execute_with_retry 5 dotnet nuget push --source "$PACKAGES_SOURCE" --api-key "$PACKAGES_SOURCE_KEY" ./artifacts/nuget/"$package"."$PACKAGE_VERSION".nupkg
done

for package in ${PACKAGES[*]}; do
  wait_until_package_is_available "$package" "$PACKAGE_VERSION"
done
