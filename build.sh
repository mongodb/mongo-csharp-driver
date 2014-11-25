#!/bin/bash

mono tools/nuget/nuget.exe install FAKE -OutputDirectory packages -ExcludeVersion

mono --runtime=v4.0 packages/FAKE/tools/FAKE.exe $@ --fsiargs -d:MONO build.fsx