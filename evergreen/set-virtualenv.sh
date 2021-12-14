#!/usr/bin/env bash

# Find the version of python on the system.
# If the directory "venv" exists, start the virtual environment.
# Otherwise, install a new virtual environment.
#
# Environment variables used as input:
#   OS                                               The current operating system
#
# Environment variables produced as output:
#   PYTHON                                           The venv python path

echo "Initialize PYTHON"

if [ -e "/cygdrive/c/python/Python36/python" ]; then
    export SYSTEM_PYTHON="/cygdrive/c/python/Python36/python"
elif [ -e "/opt/mongodbtoolchain/v3/bin/python3" ]; then
    export SYSTEM_PYTHON="/opt/mongodbtoolchain/v3/bin/python3"
elif python3 --version >/dev/null 2>&1; then
    export SYSTEM_PYTHON=python3
else
    export SYSTEM_PYTHON=python
fi

if [ ! -e venv ]; then
    $SYSTEM_PYTHON -m venv ./venv
fi

if [ "Windows_NT" = "$OS" ]; then
    export PYTHON="$(pwd)/venv/Scripts/python"
else
    export PYTHON="$(pwd)/venv/bin/python"
fi

echo "PYTHON has been initialized"
