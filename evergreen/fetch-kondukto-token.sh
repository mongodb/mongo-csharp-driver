#!/usr/bin/env bash

set -o errexit  # Exit the script with error if any of the commands fail

# Environment variables used as input:
# AWS_ACCESS_KEY_ID
# AWS_SECRET_ACCESS_KEY
# AWS_SESSION_TOKEN

kondukto_token=$(aws secretsmanager get-secret-value --secret-id "kondukto-token" --region "us-east-1" --query 'SecretString' --output text)
echo "KONDUKTO_TOKEN=$kondukto_token" > ${PWD}/kondukto_credentials.env
