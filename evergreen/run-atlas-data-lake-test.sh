#!/bin/bash

set -o xtrace
set -o errexit  # Exit the script with error if any of the commands fail

############################################
#            Main Program                  #
############################################

echo "Running Atlas Data Lake driver tests"

export MONGODB_URI="mongodb://mhuser:pencil@localhost"
export ATLAS_DATA_LAKE_TESTS_ENABLED=true

powershell.exe .\\build.ps1 -target TestAtlasDataLake
