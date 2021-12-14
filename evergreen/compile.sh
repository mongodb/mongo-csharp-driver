#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with error if any of the commands fail

############################################
#            Main Program                  #
############################################

echo "Compiling .NET driver"

if [ "Windows_NT" = "$OS" ]; then # Magic variable in cygwin
  for var in TMP TEMP NUGET_PACKAGES NUGET_HTTP_CACHE_PATH APPDATA; do
    setx $var z:\\data\\tmp
    export $var=z:\\data\\tmp
  done
  powershell.exe .\\build.ps1 --target Build --Verbosity Diagnostic
else
  for var in TMP TEMP NUGET_PACKAGES NUGET_HTTP_CACHE_PATH APPDATA; do
    export $var=/data/tmp;
  done
  ./build.sh --target=Build --Verbosity Diagnostic
fi
