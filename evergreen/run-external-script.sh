#!/usr/bin/env bash

set -o errexit  # Exit the script with error if any of the commands fail

# clone the repo
rm -rf "${LOCAL_PATH}"
mkdir "${LOCAL_PATH}"
cd "${LOCAL_PATH}" || exit

echo "Cloning the remote repo..."
git clone -b "${GIT_BRANCH:-main}" --single-branch "${GIT_REPO}" .

# add/adjust nuget.config pointing to myget so intermediate versions could be restored
. "${PROJECT_DIRECTORY}/evergreen/append-myget-package-source.sh"

# make files executable
echo "Making files executable"
for i in $(find "." -name \*.sh); do
  chmod +x $i
done

# execute the provided script
echo "Evaluating the script"
eval "$SCRIPT"
