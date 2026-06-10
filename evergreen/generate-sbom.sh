#!/usr/bin/env bash
# Environment Variables:
#   PACKAGE_VERSION (optional) - Version to use for SBOM generation
#                                Defaults to output of get-version.sh
#   GITHUB_USER (optional)     - GitHub username for license resolution
#   GITHUB_APIKEY (optional)   - GitHub API token for license resolution
set -eo pipefail

# Accommodate Git Bash or MSYS2 on Windows
export MSYS_NO_PATHCONV=1

SERIAL_NUMBER="urn:uuid:a24dfa6a-26a8-44d8-94bd-a2488e01185b"
SBOM_SLNF="MongoDB.Driver.sbom.slnf"

echo -e "\n************************************************"

# Get package version if not set
if [[ -z "$PACKAGE_VERSION" ]]; then
  PACKAGE_VERSION=$(bash ./evergreen/get-version.sh)
fi

echo "Package Version: ${PACKAGE_VERSION}"

echo "Restoring solution with version set to ${PACKAGE_VERSION}"
dotnet restore "${SBOM_SLNF}" /p:Version="${PACKAGE_VERSION}" /p:TreatWarningsAsErrors=false

# Install cyclonedx-dotnet
echo "Installing cyclonedx-dotnet"
dotnet tool install --global CycloneDX --version 6.2.0 --allow-downgrade

echo -e "\nGenerating SBOM"
echo "************************************************"

# Attempt GitHub license resolution only if GITHUB_USER and GITHUB_APIKEY are both non-empty
if [[ -n "${GITHUB_USER:-}" && -n "${GITHUB_APIKEY:-}" ]]; then
  echo "GitHub license resolution enabled for user: ${GITHUB_USER}"
  github_options=(--enable-github-licenses --github-username "${GITHUB_USER}" --github-token "${GITHUB_APIKEY}")
else
  echo "GitHub license resolution disabled (GITHUB_USER or GITHUB_APIKEY not set)"
  github_options=()
fi

# Packages with PrivateAssets="All" in Directory.Build.props that --exclude-dev does not
# cover reliably. Version specifiers are not required in cyclonedx-dotnet 6.2.0+.
EXCLUDE_FILTER="Microsoft.CodeAnalysis.FxCopAnalyzers,Microsoft.NETFramework.ReferenceAssemblies,Microsoft.SourceLink.GitHub"

dotnet-CycloneDX "${SBOM_SLNF}" \
  --disable-package-restore \
  --configuration Release \
  --set-type library \
  --set-nuget-purl \
  --exclude-dev \
  --set-name mongo-csharp-driver \
  --set-version "${PACKAGE_VERSION}" \
  --exclude-filter "${EXCLUDE_FILTER}" \
  --spec-version 1.5 \
  --filename sbom.cdx.json \
  "${github_options[@]+"${github_options[@]}"}"

echo -e "\n================================="
echo "Resolving libmongocrypt version"
echo "================================="

MONGOCRYPT_COMMIT=$(sed -n 's/.*<LibMongoCryptCommit>\([^<]*\)<\/LibMongoCryptCommit>.*/\1/p' \
  src/MongoDB.Driver.Encryption/MongoDB.Driver.Encryption.csproj | head -1)

if [[ -z "$MONGOCRYPT_COMMIT" ]]; then
  echo "ERROR: Could not extract LibMongoCryptCommit from MongoDB.Driver.Encryption.csproj" >&2
  exit 1
fi

echo "Looking up tag for libmongocrypt commit ${MONGOCRYPT_COMMIT}"
github_auth_args=()
[[ -n "${GITHUB_APIKEY:-}" ]] && github_auth_args=(-H "Authorization: Bearer ${GITHUB_APIKEY}")

# The tags API returns newest-first; bare semver tags (1.x.y) may not appear until page 2+
# because pymongocrypt-* and node-v* tags dominate the first pages. Stop early when found.
LIBMONGOCRYPT_VERSION=""
for page in 1 2 3 4 5; do
  tags_json=$(curl -sSL \
    -H "Accept: application/vnd.github+json" \
    "${github_auth_args[@]+"${github_auth_args[@]}"}" \
    "https://api.github.com/repos/mongodb/libmongocrypt/tags?per_page=100&page=${page}" \
    2>/dev/null || echo '[]')
  [[ $(jq -r 'type' <<< "$tags_json") != "array" ]] && { echo "Unexpected GitHub API response (not an array), stopping tag lookup" >&2; break; }
  [[ $(jq 'length' <<< "$tags_json") -eq 0 ]] && break
  # Prefer bare semver tags (e.g. 1.15.1) over language-prefixed tags for the same commit
  LIBMONGOCRYPT_VERSION=$(jq -r --arg sha "$MONGOCRYPT_COMMIT" \
    '[.[] | select(.commit.sha == $sha) | .name] |
     (map(select(test("^[0-9]"))) | first) // (first // empty)' \
    <<< "$tags_json")
  [[ -n "$LIBMONGOCRYPT_VERSION" ]] && break
done

if [[ -z "$LIBMONGOCRYPT_VERSION" ]]; then
  echo "No tag found for commit ${MONGOCRYPT_COMMIT}, using SHA as version"
  LIBMONGOCRYPT_VERSION="$MONGOCRYPT_COMMIT"
else
  echo "Resolved commit ${MONGOCRYPT_COMMIT} to tag ${LIBMONGOCRYPT_VERSION}"
fi

LIBMONGOCRYPT_PURL="pkg:github/mongodb/libmongocrypt@${LIBMONGOCRYPT_VERSION}"
ENCRYPTION_PURL="pkg:nuget/MongoDB.Driver.Encryption@${PACKAGE_VERSION}"

tmp=$(mktemp)
jq \
  --arg purl  "$LIBMONGOCRYPT_PURL" \
  --arg ver   "$LIBMONGOCRYPT_VERSION" \
  --arg encpurl "$ENCRYPTION_PURL" \
  '.components += [{
    "type": "library",
    "bom-ref": $purl,
    "name": "libmongocrypt",
    "version": $ver,
    "purl": $purl
  }] |
  .dependencies += [{
    "ref": $encpurl,
    "dependsOn": [$purl]
  }]' sbom.cdx.json > "$tmp" && mv "$tmp" sbom.cdx.json

echo -e "\n================================="
echo "Updating sbom.json with version tracking"
echo "================================="

CURRENT_VERSION=$(jq -r '.version // 0' sbom.json 2>/dev/null || echo 0)
NEW_CONTENT=$(jq -S 'del(.version, .metadata.timestamp)' sbom.cdx.json)
OLD_CONTENT=$(jq -S 'del(.version, .metadata.timestamp)' sbom.json 2>/dev/null || echo '{}')

if [ "$NEW_CONTENT" = "$OLD_CONTENT" ]; then
  NEW_VERSION=$CURRENT_VERSION
  echo "SBOM content unchanged, keeping version ${NEW_VERSION}"
else
  NEW_VERSION=$((CURRENT_VERSION + 1))
  echo "SBOM content changed, incrementing version to ${NEW_VERSION}"
fi

jq --argjson v "$NEW_VERSION" --arg serial "$SERIAL_NUMBER" \
  '.version = $v | .serialNumber = $serial' sbom.cdx.json > sbom.json
rm sbom.cdx.json

echo "Generated sbom.json (version ${NEW_VERSION})"
