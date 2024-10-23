#!/usr/bin/env bash

set -o errexit  # Exit the script with error if any of the commands fail

# Environment variables used as input:
#   OS                                               The current operating system

LIBMONGOCRYPT_DIR="$(pwd)/src/MongoDB.Driver.Encryption"

# export the LIBMONGOCRYPT_PATH variable based on OS
if [[ "$OS" =~ Ubuntu|ubuntu ]]; then
  arch=$(uname -m)
  if [[ "$arch" == "x86_64" ]]; then
      arch_path="x64"
  elif [[ "$arch" == "aarch64" || "$arch" == "arm64" ]]; then
      arch_path="arm64"
  else
      echo "Unsupported architecture: $arch"
      exit 1
  fi
  LIBMONGOCRYPT_PATH="$LIBMONGOCRYPT_DIR/linux/$arch_path/libmongocrypt.so"
elif [[ "$OS" =~ macos|macOS ]]; then
  LIBMONGOCRYPT_PATH="$LIBMONGOCRYPT_DIR/macos/libmongocrypt.dylib"
elif [[ "$OS" =~ Windows|windows ]]; then
  LIBMONGOCRYPT_PATH=$(cygpath -w "$LIBMONGOCRYPT_DIR/windows/mongocrypt.dll")
else
  echo "Unsupported OS: $OS"
  exit 1
fi

export LIBMONGOCRYPT_PATH
echo "LIBMONGOCRYPT_PATH set to $LIBMONGOCRYPT_PATH"
