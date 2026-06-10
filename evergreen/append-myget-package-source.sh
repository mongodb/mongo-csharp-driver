#!/usr/bin/env bash

# add/adjust nuget.config pointing to myget so intermediate versions could be restored
if [ -f "./nuget.config" ]; then
  echo "Adding myget into nuget.config"
  NUGET_SOURCES=$(dotnet nuget list source --format short)
  if [[ ${NUGET_SOURCES} != *"https://www.myget.org/F/mongodb/api/v3/index.json"* ]];then
      dotnet nuget add source https://www.myget.org/F/mongodb/api/v3/index.json -n myget.org --configfile ./nuget.config
  fi
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
