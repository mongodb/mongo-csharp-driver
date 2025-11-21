#!/usr/bin/env bash

set -o errexit  # Exit the script with error if any of the commands fail

# Environment variables produced as output
#       ATLAS_SEARCH_INDEX_HELPERS_TESTS_ENABLED  Enable atlas search index helpers tests.

############################################
#            Main Program                  #
############################################

echo "Running Atlas Search Index Helpers driver tests"

export ATLAS_SEARCH_INDEX_HELPERS_TESTS_ENABLED=true

./evergreen/compile-sources.sh
./build.sh --target=TestAtlasSearchIndexHelpers
