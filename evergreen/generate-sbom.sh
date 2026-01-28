#!/usr/bin/env bash
set -o errexit # Exit the script with error if any of the commands fail

# Accommodate Git Bash or MSYS2 on Windows
export MSYS_NO_PATHCONV=1

echo -e "\n************************************************"

# Get Packages Version if variable is empty
if [[ -z "$PACKAGE_VERSION" ]]; then
  PACKAGE_VERSION=$(bash ./evergreen/get-version.sh)
fi

echo "Package Version: ${PACKAGE_VERSION}"

# Get array of Package Names
source ./evergreen/packages.sh
echo "Packages: ${PACKAGES[*]}"

# Run a restore that will set the package version in the Directory.Build.props file, otherwise it is set to "0.0.0-local"
# This will also cause the Choose...When conditions in the .csproj files to use PackageReference instead of ProjectReference
echo "Restoring solution with version set to ${PACKAGE_VERSION}"
dotnet restore /p:Version=${PACKAGE_VERSION}

# Install cyclonedx-dotnet using latest version tested with this script
echo "Installing cyclonedx-dotnet"
dotnet tool install --global CycloneDX --version 5.3.1 --allow-downgrade

echo -e "\nGenerating SBOMs"
echo "************************************************"

# Track SBOM file paths for merging
SBOM_FILES=""

for package in ${PACKAGES[*]}; do
  echo -e "\n+++++++++++++++++++++++++++++++++++++"
  echo "Processing: ${package}"

  SBOM_FILE="sbom.${package}.cdx.json"
  SBOM_FILES="${SBOM_FILES} /pwd/${SBOM_FILE}"

  echo "SBOM file: ${SBOM_FILE}"

  # There are nuances to how cyclonedx-dotnet handles <PackageReference> items in Directory.Build.props that lead to private packages being included in SBOM
  # results even when PrivateAssets is set to "All". As a safeguard, this command lists the PackageReferences and adds the references with PrivateAssets="All"
  # to an exclusion filter variable to be fed into cyclonedx-dotnet
  EXCLUDE_FILTER=$(dotnet msbuild ./src/${package}/${package}.csproj -getItem:PackageReference | jq -r '[.Items.PackageReference[] | select(.PrivateAssets != null) | select(.PrivateAssets | test ("All"; "i")) | .Identity + "@" + .Version] | join(",")')
  echo "Excluded Private Package References: ${EXCLUDE_FILTER}"

  # The ProjectReference items do not resolve as the Nuget packages they represent. This causes duplicate components when the SBOMs are merged. To address this
  # we add the Nuget PURL to the JSON. This command lists the ProjectReferences for processing.
  PURL_PATCHES=$(dotnet msbuild ./src/${package}/${package}.csproj -getItem:ProjectReference | jq -r '[.Items.ProjectReference[] | .Filename] | join(",")')
  echo "Project References requiring added PURL: ${PURL_PATCHES}"

  echo "+++++++++++++++++++++++++++++++++++++"

  ## Run cyclonedx-dotnet
  # Attempt GitHub license resolution only if GITHUB_USER and GITHUB_APIKEY are both set
  if [[ -v GITHUB_USER && -v GITHUB_APIKEY ]]; then
    github_options=(--enable-github-licenses --github-username ${GITHUB_USER} --github-token ${GITHUB_APIKEY})
  fi

  echo "dotnet-CycloneDX src/${package}/${package}.csproj --disable-package-restore --set-type library --set-nuget-purl --exclude-dev --include-project-references --set-name ${package} --set-version ${PACKAGE_VERSION} --filename ${SBOM_FILE} --exclude-filter ${EXCLUDE_FILTER} ${github_options[@]}"
  dotnet-CycloneDX src/${package}/${package}.csproj \
    --disable-package-restore --set-type library --set-nuget-purl --exclude-dev --include-project-references \
    --set-name ${package} --set-version ${PACKAGE_VERSION} --filename ${SBOM_FILE} \
    --exclude-filter "${EXCLUDE_FILTER}" \
    "${github_options[@]}"

  # Patch JSON file with PURLs, as needed
  for patch in $PURL_PATCHES; do
    echo "Patching ${patch} with Nuget PURL"
    contents=$(jq --arg package "$patch" --arg version "$PACKAGE_VERSION" '.components |= map(if [.name | startswith("MongoDB.")] and has("purl") == false then .purl = "pkg:nuget/\($package)@\($version)" else . end)' ${SBOM_FILE})
    echo -E "${contents}" >${SBOM_FILE}
  done

done

echo -e "\n================================="
echo "Merging SBOMs using cyclonedx-cli"
echo "================================="

# Use cyclonedx-cli to merge the SBOMs into 1 hierarchical SBOM
docker run --platform="linux/amd64" --rm -v ${PWD}:/pwd \
  cyclonedx/cyclonedx-cli:0.28.2 \
  merge --input-files ${SBOM_FILES} --output-file /pwd/sbom.cdx.json \
  --hierarchical --group mongodb --name mongo-csharp-driver --version ${PACKAGE_VERSION}
