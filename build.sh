#!/bin/bash

mono --runtime=v4.0 Tools/NuGet/NuGet.exe install FAKE -OutputDirectory packages -ExcludeVersion

mono --runtime=v4.0 packages/FAKE/tools/FAKE.exe $@ --fsiargs -d:MONO ./build/build.fsx