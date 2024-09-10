#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

DOTNET_SDK_PATH="${DOTNET_SDK_PATH:-./.dotnet}"

if [[ $OS =~ [Ww]indows.* ]]; then
  echo "Downloading Windows .NET SDK installer into $DOTNET_SDK_PATH folder..."
  curl -Lfo ./dotnet-install.ps1 https://dot.net/v1/dotnet-install.ps1
  echo "Installing .NET 8.0 SDK..."
  powershell.exe ./dotnet-install.ps1 -Channel 8.0 -InstallDir "$DOTNET_SDK_PATH" -NoPath
else
  echo "Downloading .NET SDK installer into $DOTNET_SDK_PATH folder..."
  curl -Lfo ./dotnet-install.sh https://dot.net/v1/dotnet-install.sh
  echo "Installing .NET 8.0 SDK..."
  bash ./dotnet-install.sh --channel 8.0 --install-dir "$DOTNET_SDK_PATH" --no-path
fi