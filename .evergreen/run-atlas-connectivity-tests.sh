#!/bin/bash

# DO NOT set xtrace
set -o errexit  # Exit the script with error if any of the commands fail

############################################
#            Main Program                  #
############################################

# Provision the correct connection string and set up SSL if needed
for var in TMP TEMP NUGET_PACKAGES NUGET_HTTP_CACHE_PATH APPDATA; do setx $var z:\\data\\tmp; export $var=z:\\data\\tmp; done
powershell.exe .\\build.ps1 -target TestAtlasConnectivity
