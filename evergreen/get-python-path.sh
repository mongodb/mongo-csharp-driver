#!/usr/bin/env bash

# Find the version of python on the system.
#
# Environment variables used as input:
#   Venv                                             Venv path
#   OS                                               The current operating system
#
# Environment variables produced as output:
#   PYTHON                                           The python path

# Don't write anything to output
if [ -z "$Venv" ]; then
    if [ -e "/cygdrive/c/python/Python39/python" ]; then
        echo "/cygdrive/c/python/Python39/python"
    elif [ -e "/opt/mongodbtoolchain/v3/bin/python3" ]; then
        echo "/opt/mongodbtoolchain/v3/bin/python3"
    elif python3 --version >/dev/null 2>&1; then
        echo python3
    else
        echo python
    fi
else
    if [[ "$OS" =~ Windows|windows ]]; then
        echo "${Venv}/Scripts/python"
    else
        echo "${Venv}/bin/python"
    fi
fi    
