#! /bin/bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit  # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#       MONGODB_URI             Set the URI, including username/password to use to connect to the server

############################################
#            Main Program                  #
############################################

export MONGO_URI="${MONGO_URI}"

# Download the data to be used in the performance tests
./download-data.sh .

dotnet run -c Release -- --filter *
