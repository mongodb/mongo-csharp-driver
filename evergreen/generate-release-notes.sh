#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

version=$1
version_tag=$2
previous_commit_sha=$(git rev-list ${version_tag} --skip=1 --max-count=1)
previous_tag=$(git describe ${previous_commit_sha} --tags --abbrev=0)

if [[ "$version" == *.0 ]]; then
  template_file="./release-notes.yml"
else
    template_file="./patch-notes.yml"
fi

python ./release-notes.py ${version} mongodb/mongo-csharp-driver ${version_tag} ${previous_tag} ${template_file}
