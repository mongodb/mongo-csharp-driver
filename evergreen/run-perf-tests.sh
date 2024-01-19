#! /bin/bash

set -o errexit  # Exit the script with error if any of the commands fail

############################################
#            Main Program                  #
############################################

# Download the data to be used in the performance tests
./scripts/download-data.sh .

dotnet run -c Release -- --driverBenchmarks --evergreen --anyCategories "DriverBench" "BsonBench" "RunBench" --a ./Benchmark.Artifacts
