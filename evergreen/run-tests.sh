#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with error if any of the commands fail

# Environment variables used as input:
#   AUTH                            Set to enable authentication. Values are: "auth" / "noauth" (default)
#   SSL                             Set to enable SSL. Values are "ssl" / "nossl" (default)
#   MONGODB_URI                     Set the suggested connection MONGODB_URI (including credentials and topology info)
#   TOPOLOGY                        Allows you to modify variables and the MONGODB_URI based on test topology
#                                   Supported values: "server", "replica_set", "sharded_cluster"
#   REQUIRE_API_VERSION             Flag to require API version. Values: "true" / nil (default)
#   CLIENT_PEM                      Path to mongo-orchestration's client.pem
#   OCSP_TLS_SHOULD_SUCCEED         Set to test OCSP. Values are true/false/nil
#   MONGODB_X509_CLIENT_P12_PATH    Absolute path to client certificate in p12 format
#   MONGO_X509_CLIENT_CERTIFICATE_PASSWORD  password for client certificate
#   FRAMEWORK                       Set to specify .NET framework to test against. Values: "Net472", "NetStandard20", "NetStandard21",
#   TARGET                          Set to specify a custom test target. Default: "nil"
#
# Environment variables produced as output:
#   MONGODB_X509_CLIENT_P12_PATH            Absolute path to client certificate in p12 format
#   MONGO_X509_CLIENT_CERTIFICATE_PASSWORD  Password for client certificate
#   MONGODB_API_VERSION                     Server API version to use in every client

AUTH=${AUTH:-noauth}
SSL=${SSL:-nossl}
MONGODB_URI=${MONGODB_URI:-}
TOPOLOGY=${TOPOLOGY:-server}
COMPRESSOR=${COMPRESSOR:-none}
OCSP_TLS_SHOULD_SUCCEED=${OCSP_TLS_SHOULD_SUCCEED:-nil}
CLIENT_PEM=${CLIENT_PEM:-nil}
PLATFORM=${PLATFORM:-nil}
TARGET=${TARGET:-Test}
FRAMEWORK=${FRAMEWORK:-nil}

############################################
#            Functions                     #
############################################

provision_ssl () {
  echo "SSL !"
  uri_environment_variable_name=$1
  # Arguments for auth + SSL
  if [ "$AUTH" != "noauth" ] || [ "$TOPOLOGY" == "replica_set" ]; then
    if [ "$OCSP_TLS_SHOULD_SUCCEED" != "nil" ]; then
      export $uri_environment_variable_name="${!uri_environment_variable_name}&ssl=true"
    else
      export $uri_environment_variable_name="${!uri_environment_variable_name}&ssl=true&tlsDisableCertificateRevocationCheck=true"
    fi
  else
    if [ "$OCSP_TLS_SHOULD_SUCCEED" != "nil" ]; then
      export $uri_environment_variable_name="${!uri_environment_variable_name}/?ssl=true"
    else
      export $uri_environment_variable_name="${!uri_environment_variable_name}/?ssl=true&tlsDisableCertificateRevocationCheck=true"
    fi
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
echo "CRYPT_SHARED_LIB_PATH:" $CRYPT_SHARED_LIB_PATH
echo "Initial MongoDB URI:" $MONGODB_URI
echo "Framework: " $FRAMEWORK

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

if [ ! -z "$REQUIRE_API_VERSION" ]; then
  export MONGODB_API_VERSION="1"
  echo "Server API version is set to $MONGODB_API_VERSION"
fi

if [[ $FRAMEWORK != "nil" ]] && [[ $TARGET != *${FRAMEWORK} ]]; then
  TARGET="${TARGET}${FRAMEWORK}"
fi

export TARGET
if [[ "$OS" =~ Windows|windows ]]; then
  if [ "$OCSP_TLS_SHOULD_SUCCEED" != "nil" ]; then
    export TARGET="TestOcsp"
    certutil.exe -urlcache localhost delete # clear the OS-level cache of all entries with the URL "localhost"
  fi
fi

echo "Test target: $TARGET"

echo "Final MongoDB_URI: $MONGODB_URI"
if [ "$TOPOLOGY" == "sharded_cluster" ]; then
  echo "Final MongoDB URI with multiple mongoses: $MONGODB_URI_WITH_MULTIPLE_MONGOSES"
fi
for var in TMP TEMP NUGET_PACKAGES NUGET_HTTP_CACHE_PATH APPDATA; do
  if [[ "$OS" =~ Windows|windows ]]; then
    export $var=z:\\data\\tmp;
  else
    export $var=/data/tmp;
  fi
done

if [[ "$CLIENT_PEM" != "nil" ]]; then
  CLIENT_PEM=${CLIENT_PEM} source evergreen/convert-client-cert-to-pkcs12.sh
fi

if [[ -z "$MONGO_X509_CLIENT_CERTIFICATE_PATH" && -z "$MONGO_X509_CLIENT_CERTIFICATE_PASSWORD" ]]; then
    # technically the above condiion will be always true since CLIENT_PEM is always set and 
    # convert-client-cert-to-pkcs12 always assigns these env variables, but leaving this condition in case 
    # if we make CLIENT_PEM input parameter conditional
    export MONGO_X509_CLIENT_CERTIFICATE_PATH=${MONGO_X509_CLIENT_CERTIFICATE_PATH}
    export MONGO_X509_CLIENT_CERTIFICATE_PASSWORD="${MONGO_X509_CLIENT_CERTIFICATE_PASSWORD}"
fi

if [[ "$OS" =~ Windows|windows ]]; then
  powershell.exe .\\build.ps1 --target=$TARGET
else
  ./build.sh --target=$TARGET
fi
