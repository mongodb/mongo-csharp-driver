#!/usr/bin/env bash

set -o errexit  # Exit the script with error if any of the commands fail
set +o xtrace # Disable tracing.

cd ./semantic-kernel/dotnet/

# Can't modify packageSourceMapping via command line yet (https://github.com/NuGet/Home/issues/10735), therefore using nuget.custom.config
echo "Creating nuget.custom.config"
cat > nuget.custom.config << EOL
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="myget.org" value="https://www.myget.org/F/mongodb/api/v3/index.json" />
  </packageSources>
  
</configuration>
EOL

# Update mongodb version
echo Update MongoDB Driver version to "$PACKAGE_VERSION"
sed -i -e 's/PackageVersion Include="MongoDB.Driver" Version=".\+"/PackageVersion Include="MongoDB.Driver" Version="'"$PACKAGE_VERSION"'"/g' Directory.Packages.props

echo "MongoDB Driver version updated"

# Set SkipReason to null to enable integration tests
sed -i -e 's/"MongoDB Atlas cluster is required"/null/g' ./src/IntegrationTests/Connectors/Memory/MongoDB/MongoDBMemoryStoreTests.cs

dotnet clean
dotnet restore --configfile nuget.custom.config
echo "restored"

# Run unit tests
dotnet test ./src/Connectors/Connectors.UnitTests/Connectors.UnitTests.csproj --filter SemanticKernel.Connectors.UnitTests.MongoDB.MongoDBMemoryStoreTests --no-restore

# Run integration tests - Currently Fails
#MongoDB__ConnectionString="$ATLAS_SK"
#dotnet test ./src/IntegrationTests/IntegrationTests.csproj --filter SemanticKernel.IntegrationTests.Connectors.MongoDB.MongoDBMemoryStoreTests
