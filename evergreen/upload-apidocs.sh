#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

DOCS_REPO="https://${GITHUB_USER}:${GITHUB_APIKEY}@github.com/mongodb/mongo-csharp-driver.git"

echo "Prepare github docs"
git clone "$DOCS_REPO" ./gh-pages/ --branch gh-pages --single-branch

mkdir ./gh-pages/"$PACKAGE_VERSION"/
cp -r ./artifacts/apidocs/"$PACKAGE_VERSION"/. ./gh-pages/"$PACKAGE_VERSION"/

cd ./gh-pages

git add --all
git commit -m "Add $PACKAGE_VERSION Api docs" --author="Build Agent<dbx-csharp-dotnet@mongodb.com>"
git push --repo="$DOCS_REPO"
