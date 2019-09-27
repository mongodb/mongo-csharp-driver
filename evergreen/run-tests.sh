#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#       AUTH                    Set to enable authentication. Values are: "auth" / "noauth" (default)
#       SSL                     Set to enable SSL. Values are "ssl" / "nossl" (default)
#       MONGODB_URI             Set the suggested connection MONGODB_URI (including credentials and topology info)
#       TOPOLOGY                Allows you to modify variables and the MONGODB_URI based on test topology 
#                               Supported values: "server", "replica_set", "sharded_cluster"

AUTH=${AUTH:-noauth}
SSL=${SSL:-nossl}
MONGODB_URI=${MONGODB_URI:-}
TOPOLOGY=${TOPOLOGY:-server}
COMPRESSOR=${COMPRESSOR:-none}

############################################
#            Functions                     #
############################################

provision_ssl () {
  echo "SSL !"
  uri_environment_variable_name=$1
  # Arguments for auth + SSL
  if [ "$AUTH" != "noauth" ] || [ "$TOPOLOGY" == "replica_set" ]; then
    export $uri_environment_variable_name="${!uri_environment_variable_name}&ssl=true&sslVerifyCertificate=false"
  else
    export $uri_environment_variable_name="${!uri_environment_variable_name}/?ssl=true&sslVerifyCertificate=false"
  fi
}

provision_compressor () {
    uri_environment_variable_name=$1
    if [[ "${!uri_environment_variable_name}" =~ "/?" ]]; then
        export $uri_environment_variable_name="${!uri_environment_variable_name}&compressors=$COMPRESSOR"
    else
        export $uri_environment_variable_name="${!uri_environment_variable_name}/?compressors=$COMPRESSOR"
    fi
}

############################################
#            Main Program                  #
############################################
echo "Initial MongoDB URI:" $MONGODB_URI
# Provision the correct connection string and set up SSL if needed
if [ "$TOPOLOGY" == "sharded_cluster" ]; then
       export MONGODB_URI_WITH_MULTIPLE_MONGOSES="${MONGODB_URI}"
     if [ "$AUTH" = "auth" ]; then
       export MONGODB_URI="mongodb://bob:pwd123@localhost:27017/?authSource=admin"
     else
       export MONGODB_URI="mongodb://localhost:27017"
     fi
fi

if [ "$SSL" != "nossl" ]; then
   provision_ssl MONGODB_URI
   if [ "$TOPOLOGY" == "sharded_cluster" ]; then
     provision_ssl MONGODB_URI_WITH_MULTIPLE_MONGOSES  
   fi
fi

if [ "$COMPRESSOR" != "none" ]; then
    provision_compressor MONGODB_URI
    if [ "$TOPOLOGY" == "sharded_cluster" ]; then
        provision_compressor MONGODB_URI_WITH_MULTIPLE_MONGOSES
    fi
fi

echo "Running $AUTH tests over $SSL for $TOPOLOGY with $COMPRESSOR compressor and connecting to $MONGODB_URI"

if [ "$OS" == "windows-64" ]; then
  export TARGET="Test"
else
  export TARGET="Test"
fi

echo "Final MongoDB_URI: $MONGODB_URI"
if [ "$TOPOLOGY" == "sharded_cluster" ]; then
  echo "Final MongoDB URI with multiple mongoses: $MONGODB_URI_WITH_MULTIPLE_MONGOSES"
fi
for var in TMP TEMP NUGET_PACKAGES NUGET_HTTP_CACHE_PATH APPDATA; do 
  export $var=z:\\data\\tmp; 
done
powershell.exe .\\build.ps1 -target ${TARGET}
