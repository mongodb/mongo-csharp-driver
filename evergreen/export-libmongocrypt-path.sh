#!/usr/bin/env bash

set -o errexit  # Exit the script with error if any of the commands fail

# Environment variables used as input:
#   OS                                               The current operating system

LIBMONGOCRYPT_DIR="$(pwd)/src/MongoDB.Libmongocrypt"

# export the LIBMONGOCRYPT_PATH variable based on OS
if [[ "$OS" =~ Ubuntu|ubuntu ]]; then
  LIBMONGOCRYPT_PATH="$LIBMONGOCRYPT_DIR/linux/libmongocrypt.so"
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

