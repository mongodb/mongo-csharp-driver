#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

DOTNET_ROOT="${DOTNET_ROOT:-./.dotnet}"
DOTNET_SDK_VERSION="${DOTNET_SDK_VERSION:-}"

echo "runtime: $FRAMEWORK"

if [ -n "$FRAMEWORK" ]; then
  if [ "$FRAMEWORK" = "net6.0" ]; then
    RUNTIME_VERSIONS="6.0"
  elif [ "$FRAMEWORK" = "net8.0" ]; then
    RUNTIME_VERSIONS="8.0"
  elif [ "$FRAMEWORK" = "net10.0" ]; then
    RUNTIME_VERSIONS="10.0"
  elif [ "$FRAMEWORK" = "net11.0" ]; then
    RUNTIME_VERSIONS="11.0"
    # .NET 11 is not GA yet: use the latest preview/RC build and an SDK that can build net11.0.
    DOTNET_SDK_VERSION="${DOTNET_SDK_VERSION:-11.0}"
    DOTNET_QUALITY="preview"
  fi
fi

DOTNET_SDK_VERSION="${DOTNET_SDK_VERSION:-10.0}"

RUNTIMES=()
if [ -n "$RUNTIME_VERSIONS" ]; then
  # Split RUNTIME_VERSIONS by comma into $RUNTIMES array
  IFS=',' read -r -a RUNTIMES <<< "$RUNTIME_VERSIONS"
fi


if [[ $OS =~ [Ww]indows.* ]]; then
  # From .NET 11 the installer prefers .tar.gz on Windows whenever a `tar` is on PATH. On Evergreen that is
  # GNU tar, which treats `C:\...` as a remote host ("Cannot connect to C: resolve failed"), so force .zip.
  export DOTNET_INSTALL_SKIP_TAR=1
  echo "Downloading Windows .NET SDK installer into $DOTNET_ROOT folder..."
  curl -Lfo ./dotnet-install.ps1 https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.ps1
  echo "Installing .NET ${DOTNET_SDK_VERSION} SDK..."
  powershell.exe ./dotnet-install.ps1 -Channel "$DOTNET_SDK_VERSION" -InstallDir "$DOTNET_ROOT" -NoPath ${DOTNET_QUALITY:+-Quality "$DOTNET_QUALITY"}
  for RUNTIME in "${RUNTIMES[@]}"; do
    echo "Installing .NET ${RUNTIME} runtime..."
    powershell.exe ./dotnet-install.ps1 -Channel "$RUNTIME" -Runtime dotnet -InstallDir "$DOTNET_ROOT" -NoPath ${DOTNET_QUALITY:+-Quality "$DOTNET_QUALITY"}
  done
else
  echo "Downloading .NET SDK installer into $DOTNET_ROOT folder..."
  curl -Lfo ./dotnet-install.sh https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh
  echo "Installing .NET ${DOTNET_SDK_VERSION} SDK..."
  bash ./dotnet-install.sh --channel "$DOTNET_SDK_VERSION" --install-dir "$DOTNET_ROOT" --no-path ${DOTNET_QUALITY:+--quality "$DOTNET_QUALITY"}
  for RUNTIME in "${RUNTIMES[@]}"; do
    echo "Installing .NET ${RUNTIME} runtime..."
    bash ./dotnet-install.sh --channel "$RUNTIME" --runtime dotnet --install-dir "$DOTNET_ROOT" --no-path ${DOTNET_QUALITY:+--quality "$DOTNET_QUALITY"}
  done
fi

DOTNET_ROOT=$(cd "$DOTNET_ROOT" && pwd) # converts relative path to absolute

export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_ROOT="$DOTNET_ROOT"
