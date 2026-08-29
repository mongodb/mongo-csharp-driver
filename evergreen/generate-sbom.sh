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

# Packages with PrivateAssets="All" in Directory.Build.props that --exclude-dev correctly
# drops at the top level, but leaves their transitive-only children behind as unreferenced
# orphans (see https://github.com/CycloneDX/cyclonedx-dotnet/issues/445). Applied via jq
# below rather than --exclude-filter: cyclonedx-dotnet 6.2.0's --exclude-filter runs its
# orphan-removal pass before project-reference packages are marked as direct references, so
# it wipes out the entire component list regardless of the filter value (see
# https://github.com/CycloneDX/cyclonedx-dotnet/blob/v6.2.0/CycloneDX/Runner.cs, no fix
# released as of v6.2.0). The list below includes both the top-level PrivateAssets="All"
# packages (in case --exclude-dev's behavior changes) and the orphaned children actually
# observed in output today.
EXCLUDE_FILTER="Microsoft.CodeAnalysis.FxCopAnalyzers,Microsoft.NETFramework.ReferenceAssemblies,Microsoft.NETFramework.ReferenceAssemblies.net472,Microsoft.SourceLink.GitHub,Microsoft.SourceLink.Common,Microsoft.Build.Tasks.Git"

dotnet-CycloneDX "${SBOM_SLNF}" \
  --disable-package-restore \
  --configuration Release \
  --set-type library \
  --set-nuget-purl \
  --exclude-dev \
  --set-name mongo-csharp-driver \
  --set-version "${PACKAGE_VERSION}" \
  --spec-version 1.5 \
  --filename sbom.cdx.json \
  "${github_options[@]+"${github_options[@]}"}"

echo -e "\n================================="
echo "Removing excluded packages: ${EXCLUDE_FILTER}"
echo "================================="

# Drops components matching EXCLUDE_FILTER by exact name, then repeatedly drops any remaining
# component whose only referrers were themselves excluded (a component with no referrers at
# all, i.e. a real direct/top-level dependency, is never touched).
tmp=$(mktemp)
jq --arg names "$EXCLUDE_FILTER" '
  . as $root
  | ($names | split(",")) as $ex0
  | (reduce $root.dependencies[] as $d ({};
       reduce ($d.dependsOn // [])[] as $child (.; .[$child] = ((.[$child] // []) + [$d.ref]))
     )) as $parentsOf
  | (reduce range(0;10) as $i (
       [$root.components[] | select(.name as $n | $ex0 | index($n)) | ."bom-ref"];
       . as $excluded
       | ($excluded + [
           $root.components[]
           | select(."bom-ref" as $r | ($excluded | index($r)) | not)
           | select((($parentsOf[."bom-ref"] // [])|length) > 0)
           | select(($parentsOf[."bom-ref"] // []) | all(. as $p | $excluded | index($p)))
           | ."bom-ref"
         ]) | unique
     )) as $excludedRefs
  | $root
  | .components |= map(select((."bom-ref" as $r | $excludedRefs | index($r)) | not))
  | .dependencies |= (
      map(select((.ref as $r | $excludedRefs | index($r)) | not))
      | map(if .dependsOn then .dependsOn |= map(select(. as $r | ($excludedRefs | index($r)) | not)) else . end)
    )
' sbom.cdx.json > "$tmp" && mv "$tmp" sbom.cdx.json

echo -e "\n================================="
echo "Resolving libmongocrypt version"
echo "================================="

LIBMONGOCRYPT_VERSION=$(dotnet msbuild src/MongoDB.Driver.Encryption/MongoDB.Driver.Encryption.csproj \
  -getProperty:LibMongoCryptVersion 2>/dev/null)

if [[ -z "$LIBMONGOCRYPT_VERSION" ]]; then
  echo "ERROR: Could not extract LibMongoCryptVersion from MongoDB.Driver.Encryption.csproj" >&2
  exit 1
fi

echo "libmongocrypt version: ${LIBMONGOCRYPT_VERSION}"

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
