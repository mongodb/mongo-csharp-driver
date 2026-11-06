#!/usr/bin/env bash
# ABOUTME: Adds the driver packages downloaded by the download-packages function as a NuGet source.
# ABOUTME: Lets unreleased driver versions be restored from ${PROJECT_DIRECTORY}/artifacts/nuget.

# Environment variables used as input:
# PROJECT_DIRECTORY

LOCAL_PACKAGES_SOURCE_NAME="local-driver-packages"
LOCAL_PACKAGES_PATH="${PROJECT_DIRECTORY}/artifacts/nuget"
if command -v cygpath > /dev/null; then
  LOCAL_PACKAGES_PATH=$(cygpath -w "${LOCAL_PACKAGES_PATH}")
fi
echo "Local driver packages path: ${LOCAL_PACKAGES_PATH}"
ls -la "${LOCAL_PACKAGES_PATH}" || echo "Local driver packages path does not exist"

# add/adjust nuget.config pointing to the local packages so intermediate versions could be restored
if [ -f "./nuget.config" ]; then
  echo "Adding local driver packages into nuget.config"
  NUGET_SOURCES=$(dotnet nuget list source --format short)
  if [[ ${NUGET_SOURCES} != *"${LOCAL_PACKAGES_PATH}"* ]];then
      dotnet nuget add source "${LOCAL_PACKAGES_PATH}" -n "${LOCAL_PACKAGES_SOURCE_NAME}" --configfile ./nuget.config
  fi
else
  echo "Creating custom nuget.config"
  cat > "nuget.config" << EOL
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="${LOCAL_PACKAGES_SOURCE_NAME}" value="${LOCAL_PACKAGES_PATH}" />
  </packageSources>
</configuration>
EOL
fi
