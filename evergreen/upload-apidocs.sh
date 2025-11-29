#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

DOCS_REPO="https://${GITHUB_USER}:${GITHUB_APIKEY}@github.com/mongodb/mongo-csharp-driver.git"

echo "Cloning github repo..."
git clone "$DOCS_REPO" ./gh-pages/ --branch gh-pages --single-branch

echo "Adding the generated API-docs site..."
mkdir ./gh-pages/"$PACKAGE_VERSION"/
cp -r ./artifacts/apidocs/"$PACKAGE_VERSION"/. ./gh-pages/"$PACKAGE_VERSION"/

cd ./gh-pages

echo "Generating redirection page..."
# setup simple redirection to the latest version.
rm -f ./api.html
cat > "api.html" << EOL
<!DOCTYPE html>
<html>
  <head>
    <title>Redirecting...</title>
    <meta charset="utf-8">
    <link rel="canonical" href="${PACKAGE_VERSION}/api/index.html" />
    <meta http-equiv="refresh" content="0; url=${PACKAGE_VERSION}/api/index.html" />
  </head>
  <body>
    <p>Redirecting you to the <a href="${PACKAGE_VERSION}/api/index.html">latest API Docs</a>...</p>
  </body>
</html>

EOL

echo "Pushing the changes..."
git add --all
git commit -m "Add $PACKAGE_VERSION Api docs" --author="Build Agent<dbx-csharp-dotnet@mongodb.com>"
git push --repo="$DOCS_REPO"

echo "Done."
