#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

DOTNET_SDK_PATH="${DOTNET_SDK_PATH:-./.dotnet}"
DOTNET_SDK_VERSION="${DOTNET_SDK_VERSION:-8.0}"

if [[ $OS =~ [Ww]indows.* ]]; then
  echo "Downloading Windows .NET SDK installer into $DOTNET_SDK_PATH folder..."
  curl -Lfo ./dotnet-install.ps1 https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1
  echo "Installing .NET 8.0 SDK..."
  powershell.exe ./dotnet-install.ps1 -Channel "$DOTNET_SDK_VERSION" -InstallDir "$DOTNET_SDK_PATH" -NoPath
else
  echo "Downloading .NET SDK installer into $DOTNET_SDK_PATH folder..."
  curl -Lfo ./dotnet-install.sh https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh
  echo "Installing .NET 8.0 SDK..."
  bash ./dotnet-install.sh --channel "$DOTNET_SDK_VERSION" --install-dir "$DOTNET_SDK_PATH" --no-path
fi
