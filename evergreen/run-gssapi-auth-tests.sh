#!/bin/bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit  # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#       AUTH_HOST             Set the hostname of a key distribution center (KDC)
#       AUTH_GSSAPI           Set the GSSAPI credentials, including a user principal/password to use to connect to AUTH_HOST server via GSSAPI authentication mechanism

############################################
#            Main Program                  #
############################################
echo "Running GSSAPI authentication tests"

export GSSAPI_TESTS_ENABLED=true

if [ "Windows_NT" = "$OS" ]; then
  cmd /c "REG ADD HKLM\SYSTEM\ControlSet001\Control\Lsa\Kerberos\Domains\LDAPTEST.10GEN.CC /v KdcNames /d ldaptest.10gen.cc /t REG_MULTI_SZ /f"
  echo "LDAPTEST.10GEN.CC registry has been added"

  cmd /c "REG ADD HKLM\SYSTEM\ControlSet001\Control\Lsa\Kerberos\Domains\LDAPTEST2.10GEN.CC /v KdcNames /d ldaptest.10gen.cc /t REG_MULTI_SZ /f"
  echo "LDAPTEST2.10GEN.CC registry has been added"

  for var in TMP TEMP NUGET_PACKAGES NUGET_HTTP_CACHE_PATH APPDATA; do
    setx $var z:\\data\\tmp
    export $var=z:\\data\\tmp
  done

  powershell.exe .\\build.ps1 -target TestGssapi
else
  echo "Setting krb5 config file"
  touch ${PROJECT_DIRECTORY}/evergreen/krb5.conf.empty
  export KRB5_CONFIG=${PROJECT_DIRECTORY}/evergreen/krb5.conf.empty

  for var in TMP TEMP NUGET_PACKAGES NUGET_HTTP_CACHE_PATH APPDATA; do
    export $var=/data/tmp;
  done

  ./build.sh -target=TestGssapi
fi;

