#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

DOTNET_SDK_PATH=./.dotnet
mkdir -p "$DOTNET_SDK_PATH"

if [[ $OS =~ [Ww]indows.* ]]; then
  echo "Downloading Windows .NET SDK installer into $DOTNET_SDK_PATH folder..."
  curl -Lfo "$DOTNET_SDK_PATH"/dotnet-install.ps1 https://dot.net/v1/dotnet-install.ps1
  echo "Installing .NET LTS SDK..."
  powershell.exe "$DOTNET_SDK_PATH"/dotnet-install.ps1 -Channel 6.0 -InstallDir "$DOTNET_SDK_PATH" -NoPath
else
  echo "Downloading .NET SDK installer into $DOTNET_SDK_PATH folder..."
  curl -Lfo "$DOTNET_SDK_PATH"/dotnet-install.sh https://dot.net/v1/dotnet-install.sh
  echo "Installing .NET LTS SDK..."
  bash "$DOTNET_SDK_PATH"/dotnet-install.sh --channel 6.0 --install-dir "$DOTNET_SDK_PATH" --no-path
fi
