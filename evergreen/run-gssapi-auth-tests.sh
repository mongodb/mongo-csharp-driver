#!/usr/bin/env bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit  # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#       AUTH_HOST             Set the hostname of a key distribution center (KDC)
#       AUTH_GSSAPI           Set the GSSAPI credentials, including a user principal/password to use to connect to AUTH_HOST server via GSSAPI authentication mechanism
#       FRAMEWORK             Set target framework to test against
#       OS                    Set whether the current operating system is Windows or not

############################################
#            Main Program                  #
############################################
echo "Running GSSAPI authentication tests"

export GSSAPI_TESTS_ENABLED=true

if [ "windows-64" = "$OS" ]; then
    cmd /c "REG ADD HKLM\SYSTEM\ControlSet001\Control\Lsa\Kerberos\Domains\LDAPTEST.BUILD.10GEN.CC /v KdcNames /d ldaptest.build.10gen.cc /t REG_MULTI_SZ /f"
    echo "LDAPTEST.BUILD.10GEN.CC registry has been added"

    export AUTH_GSSAPI="${PRINCIPAL_BUILD}:${SASL_PASS}"
else
  echo "Setting krb5 config file"
  touch ./evergreen/krb5.conf.empty
  export KRB5_CONFIG=./evergreen/krb5.conf.empty

  echo -n "${KEYTAB_BASE64_BUILD}" | base64 -d > ./evergreen/drivers.keytab
  kinit -k -t ./evergreen/drivers.keytab ${PRINCIPAL_BUILD}

  export AUTH_GSSAPI=${PRINCIPAL_BUILD}
fi;

export AUTH_HOST=${SASL_HOST_BUILD}

./evergreen/compile-sources.sh
TEST_CATEGORY=GssapiMechanism ./evergreen/execute-tests.sh
