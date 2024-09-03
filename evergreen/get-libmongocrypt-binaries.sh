#!/usr/bin/env bash

set -o errexit  # Exit the script with error if any of the commands fail

# Environment variables used as input:
#   OS                                               The current operating system

TAR_URL="https://mciuploads.s3.amazonaws.com/libmongocrypt/all/1.10.0/libmongocrypt-all.tar.gz"

# Directory where the tar file will be extracted
EXTRACT_DIR="${PROJECT_DIRECTORY}/libmongocrypt-all-binaries"

# Output directory to collect binaries if the 'packaging' argument is used
COLLECT_DIR="${PROJECT_DIRECTORY}/binaries-to-package"

# Function to download and extract binaries
download_and_extract() {
    echo "Downloading binaries..."
    curl -L -o libmongocrypt-all.tar.gz "$TAR_URL"

    echo "Extracting binaries..."
    mkdir -p "$EXTRACT_DIR"
    tar -xzf libmongocrypt-all.tar.gz -C "$EXTRACT_DIR"
}

# Function to export the LIBMONGOCRYPT_PATH variable based on OS
export_libmongocrypt_path() {
    if [[ "$OS" =~ Ubuntu|ubuntu ]]; then
      LIBMONGOCRYPT_PATH="$EXTRACT_DIR/ubuntu1804-64/nocrypto/lib/libmongocrypt.so"
    elif [[ "$OS" =~ macos|macOS ]]; then
      LIBMONGOCRYPT_PATH="$EXTRACT_DIR/macos/lib/libmongocrypt.dylib"
    elif [[ "$OS" =~ Windows|windows ]]; then
      LIBMONGOCRYPT_PATH=$(cygpath -m "$EXTRACT_DIR/windows-test/bin/mongocrypt.dll")
    else
      echo "Unsupported OS: $OS"
      exit 1
    fi

    export LIBMONGOCRYPT_PATH
    echo "LIBMONGOCRYPT_PATH set to $LIBMONGOCRYPT_PATH"
}

# Function to collect binaries into a folder that'll be used later for packaging
collect_binaries() {
    echo "Collecting binaries..."
    mkdir -p "$COLLECT_DIR"/{linux,linux-alpine,macos,windows}

    # Copy binaries for Linux, Mac, and Windows to the collect folder
    cp "$EXTRACT_DIR/ubuntu1804-64/nocrypto/lib/libmongocrypt.so" "$COLLECT_DIR/linux/"
    cp "$EXTRACT_DIR/alpine-arm64-earthly/nocrypto/lib/libmongocrypt.so" "$COLLECT_DIR/linux-alpine/"
    cp "$EXTRACT_DIR/macos/lib/libmongocrypt.dylib" "$COLLECT_DIR/macos/"
    cp "$EXTRACT_DIR/windows-test/bin/mongocrypt.dll" "$COLLECT_DIR/windows/"

    echo "Binaries collected in $COLLECT_DIR"
}

# Main script logic
if [ "$1" == "testing" ]; then
    download_and_extract
    export_libmongocrypt_path
elif [ "$1" == "packaging" ]; then
    download_and_extract
    collect_binaries
else
    echo "Invalid argument. Use 'testing' to export LIBMONGOCRYPT_PATH or 'packaging' to gather binaries."
    exit 1
fi
