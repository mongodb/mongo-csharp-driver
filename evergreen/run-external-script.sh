#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with error if any of the commands fail

# clone the repo
rm -rf "${LOCAL_PATH}"
mkdir "${LOCAL_PATH}"
cd "${LOCAL_PATH}" || exit

echo "Cloning the remote repo..."
git clone -b "${GIT_BRANCH:-main}" --single-branch "${GIT_REPO}" .

../evergreen/add-myget-source.sh

# make files executable
echo "Making files executable"
for i in $(find "." -name \*.sh); do
  chmod +x $i
done

# execute the provided script
echo "Evaluating the script"
eval "$SCRIPT"
