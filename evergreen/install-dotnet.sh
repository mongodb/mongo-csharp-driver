#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

DOTNET_ROOT="${DOTNET_ROOT:-./.dotnet}"
DOTNET_SDK_VERSION="${DOTNET_SDK_VERSION:-10.0}"

echo "runtime: $FRAMEWORK"

if [ -n "$FRAMEWORK" ]; then
  if [ "$FRAMEWORK" = "net5.0" ]; then
    RUNTIME_VERSION="5.0"
  elif [ "$FRAMEWORK" = "net6.0" ]; then
    RUNTIME_VERSION="6.0"
  elif [ "$FRAMEWORK" = "net8.0" ]; then
    RUNTIME_VERSION="8.0"
  elif [ "$FRAMEWORK" = "net10.0" ]; then
    RUNTIME_VERSION="10.0"
  elif [ "$FRAMEWORK" = "netstandard2.1" ]; then
    RUNTIME_VERSION="3.1"
  elif [ "$FRAMEWORK" = "netcoreapp3.1" ]; then
    RUNTIME_VERSION="3.1"
  fi
fi

if [[ $OS =~ [Ww]indows.* ]]; then
  echo "Downloading Windows .NET SDK installer into $DOTNET_ROOT folder..."
  curl -Lfo ./dotnet-install.ps1 https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1
  echo "Installing .NET ${DOTNET_SDK_VERSION} SDK..."
  powershell.exe ./dotnet-install.ps1 -Channel "$DOTNET_SDK_VERSION" -InstallDir "$DOTNET_ROOT" -NoPath
  if [ -n  "$RUNTIME_VERSION" ]; then
    echo "Installing .NET ${RUNTIME_VERSION} runtime..."
    powershell.exe ./dotnet-install.ps1 -Channel "$RUNTIME_VERSION" -Runtime dotnet -InstallDir "$DOTNET_ROOT" -NoPath
  fi
else
  echo "Downloading .NET SDK installer into $DOTNET_ROOT folder..."
  curl -Lfo ./dotnet-install.sh https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh
  echo "Installing .NET ${DOTNET_SDK_VERSION} SDK..."
  bash ./dotnet-install.sh --channel "$DOTNET_SDK_VERSION" --install-dir "$DOTNET_ROOT" --no-path
  if [ -n  "$RUNTIME_VERSION" ]; then
    echo "Installing .NET ${RUNTIME_VERSION} runtime..."
    bash ./dotnet-install.sh --channel "$RUNTIME_VERSION" --runtime dotnet --install-dir "$DOTNET_ROOT" --no-path
  fi
fi

DOTNET_ROOT=$(cd "$DOTNET_ROOT" && pwd) # converts relative path to absolute

export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_ROOT="$DOTNET_ROOT"
