#!/usr/bin/env bash

set -o xtrace  # Write all commands first to stderr
set -o errexit # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#   SSL                         Set to enable SSL. Values are "ssl" / "nossl" (default)
#   OS                          Set to current operating system
#   FRAMEWORK                   Set to .net framework
#   SINGLE_MONGOS_LB_URI        Set the URI pointing to a load balancer configured with a single mongos server
#   MULTI_MONGOS_LB_URI         Set the URI pointing to a load balancer configured with multiple mongos servers

SSL=${SSL:-nossl}

############################################
#            Main Program                  #
############################################

if [[ ! "$OS" =~ Ubuntu|ubuntu ]]; then
  echo "Unsupported OS:${OS}" 1>&2 # write to stderr
  exit 1
fi


if [ "$SSL" != "nossl" ]; then 
  SINGLE_MONGOS_LB_URI="${SINGLE_MONGOS_LB_URI}&ssl=true&tlsDisableCertificateRevocationCheck=true"
  MULTI_MONGOS_LB_URI="${MULTI_MONGOS_LB_URI}&ssl=true&tlsDisableCertificateRevocationCheck=true" 
fi

echo "Running $AUTH tests (${FRAMEWORK}) over $SSL and connecting to SINGLE_MONGOS_LB_URI: $SINGLE_MONGOS_LB_URI or MULTI_MONGOS_LB_URI: $MULTI_MONGOS_LB_URI" 

export MONGODB_URI=${SINGLE_MONGOS_LB_URI}
export MONGODB_URI_WITH_MULTIPLE_MONGOSES=${MULTI_MONGOS_LB_URI}

# show test output
set -x

./build.sh --target TestLoadBalanced${FRAMEWORK}
