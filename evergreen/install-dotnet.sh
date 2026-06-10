#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

DOTNET_ROOT="${DOTNET_ROOT:-./.dotnet}"
DOTNET_SDK_VERSION="${DOTNET_SDK_VERSION:-10.0}"

echo "runtime: $FRAMEWORK"

if [ -n "$FRAMEWORK" ]; then
  if [ "$FRAMEWORK" = "net5.0" ]; then
    RUNTIME_VERSIONS="5.0"
  elif [ "$FRAMEWORK" = "net6.0" ]; then
    RUNTIME_VERSIONS="6.0"
  elif [ "$FRAMEWORK" = "net8.0" ]; then
    RUNTIME_VERSIONS="8.0"
  elif [ "$FRAMEWORK" = "net10.0" ]; then
    RUNTIME_VERSIONS="10.0"
  elif [ "$FRAMEWORK" = "netstandard2.1" ]; then
    RUNTIME_VERSIONS="3.1"
  elif [ "$FRAMEWORK" = "netcoreapp3.1" ]; then
    RUNTIME_VERSIONS="3.1"
  fi
fi

RUNTIMES=()
if [ -n "$RUNTIME_VERSIONS" ]; then
  # Split RUNTIME_VERSIONS by comma into $RUNTIMES array
  IFS=',' read -r -a RUNTIMES <<< "$RUNTIME_VERSIONS"
fi


if [[ $OS =~ [Ww]indows.* ]]; then
  echo "Downloading Windows .NET SDK installer into $DOTNET_ROOT folder..."
  curl -Lfo ./dotnet-install.ps1 https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1
  echo "Installing .NET ${DOTNET_SDK_VERSION} SDK..."
  powershell.exe ./dotnet-install.ps1 -Channel "$DOTNET_SDK_VERSION" -InstallDir "$DOTNET_ROOT" -NoPath
  for RUNTIME in "${RUNTIMES[@]}"; do
    echo "Installing .NET ${RUNTIME} runtime..."
    powershell.exe ./dotnet-install.ps1 -Channel "$RUNTIME" -Runtime dotnet -InstallDir "$DOTNET_ROOT" -NoPath
  done
else
  echo "Downloading .NET SDK installer into $DOTNET_ROOT folder..."
  curl -Lfo ./dotnet-install.sh https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh
  echo "Installing .NET ${DOTNET_SDK_VERSION} SDK..."
  bash ./dotnet-install.sh --channel "$DOTNET_SDK_VERSION" --install-dir "$DOTNET_ROOT" --no-path
  for RUNTIME in "${RUNTIMES[@]}"; do
    echo "Installing .NET ${RUNTIME} runtime..."
    bash ./dotnet-install.sh --channel "$RUNTIME" --runtime dotnet --install-dir "$DOTNET_ROOT" --no-path
  done
fi

DOTNET_ROOT=$(cd "$DOTNET_ROOT" && pwd) # converts relative path to absolute

export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_ROOT="$DOTNET_ROOT"
