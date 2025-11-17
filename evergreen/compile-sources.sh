#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

RESTORE_MAX_RETRIES=5
RESTORE_RETRY_DELAY_SECONDS_MULTIPLIER=10

for (( ATTEMPT=1; ATTEMPT<=RESTORE_MAX_RETRIES; ATTEMPT++ ))
do
  echo "Attempt $ATTEMPT of $RESTORE_MAX_RETRIES to run dotnet restore..."
  dotnet restore || exit_status=$?
  if [[ "$exit_status" -eq 0 ]]; then
    echo "dotnet restore succeeded."
    break
  fi

  if [[ $ATTEMPT -eq $RESTORE_MAX_RETRIES ]]; then
    echo "Failed to restore packages after $RESTORE_MAX_RETRIES retries."
    exit 1
  fi

  DELAY=$((ATTEMPT * RESTORE_RETRY_DELAY_SECONDS_MULTIPLIER))
  echo "dotnet restore failed. Retrying in $DELAY seconds..."
  sleep $DELAY
done

dotnet build -c Release --no-restore
