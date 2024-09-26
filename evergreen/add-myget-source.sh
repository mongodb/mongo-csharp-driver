#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with error if any of the commands fail

# add/adjust nuget.config pointing to myget so intermediate versions could be restored
if [ -f "./nuget.config" ]; then
  echo "Adding myget into nuget.config"
  dotnet nuget add source https://www.myget.org/F/mongodb/api/v3/index.json -n myget.org --configfile ./nuget.config
else
  echo "Creating custom nuget.config"
  cat > "nuget.config" << EOL
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="myget.org" value="https://www.myget.org/F/mongodb/api/v3/index.json" />
  </packageSources>
</configuration>
EOL
fi
