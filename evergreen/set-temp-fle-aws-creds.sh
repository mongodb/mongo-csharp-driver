#!/usr/bin/env bash

# Obtains temporary AWS credentials for CSFLE testing.
#
# Run with a . to add environment variables to the current shell:
# . ./set-temp-fle-aws-creds.sh
#
# Requires the python AWS SDK boto3. This can be installed with: pip install boto3
# The path to python in a virtual environment may be passed with the PYTHON
# environment variable.
#
# Environment variables used as input:
#   FLE_AWS_KEY                                      Set to access for global FLE_AWS_KEY
#   FLE_AWS_SECRET                                   Set to access for global FLE_AWS_SECRET
#   FLE_AWS_DEFAULT_REGION                           Set default AWS region for FLE_AWS_KEY
#
# Environment variables produced as output:
#   FLE_AWS_TEMP_ACCESS_KEY_ID                       Temporary AWS_ACCESS_KEY_ID
#   FLE_AWS_TEMP_SECRET_ACCESS_KEY                   Temporary AWS_SECRET_ACCESS_KEY
#   FLE_AWS_TEMP_SESSION_TOKEN                       Temporary AWS_SESSION_TOKEN

set +o xtrace # Disable tracing.

if [ -f "$DRIVERS_TOOLS/.evergreen/csfle/secrets-export.sh" ]; then
  source $DRIVERS_TOOLS/.evergreen/csfle/secrets-export.sh
else
  echo "$DRIVERS_TOOLS/.evergreen/csfle/secrets-export.sh does not exists."
  exit 2
fi

#boto3 expects env variables in a bit different form than we use
export AWS_ACCESS_KEY_ID=$FLE_AWS_KEY
export AWS_SECRET_ACCESS_KEY=$FLE_AWS_SECRET
export AWS_DEFAULT_REGION=$FLE_AWS_DEFAULT_REGION

echo "Triggering temporary CSFLE credentials"

get_creds() {
    $PYTHON - "$@" << 'EOF'
import sys
import boto3
client = boto3.client("sts")
credentials = client.get_session_token()["Credentials"]
sys.stdout.write(credentials["AccessKeyId"] + " " + credentials["SecretAccessKey"] + " " + credentials["SessionToken"])
EOF
}

PYTHON=${PYTHON:-python}
$PYTHON -m pip install boto3

CREDS=$(get_creds)

export FLE_AWS_TEMP_ACCESS_KEY_ID=$(echo $CREDS | awk '{print $1}')
export FLE_AWS_TEMP_SECRET_ACCESS_KEY=$(echo $CREDS | awk '{print $2}')
export FLE_AWS_TEMP_SESSION_TOKEN=$(echo $CREDS | awk '{print $3}')
#enable related tests in the driver
export FLE_AWS_TEMPORARY_CREDS_ENABLED=true
echo "CSFLE credentials have been exported"
