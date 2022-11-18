#!/usr/bin/env bash

set -o xtrace
set -o errexit  # Exit the script with error if any of the commands fail

# Environment variables produced as output
#       ATLAS_SEARCH_TESTS_ENABLED  Enable atlas search tests.

############################################
#            Main Program                  #
############################################

echo "Running Atlas Search driver tests"

export ATLAS_SEARCH_TESTS_ENABLED=true

powershell.exe .\\build.ps1 --target=TestAtlasSearch