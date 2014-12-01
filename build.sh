#!/bin/bash

mono Tools/NuGet/NuGet.exe install FAKE -OutputDirectory packages -ExcludeVersion

mono packages/FAKE/tools/FAKE.exe $@ --fsiargs -d:MONO ./build.fsx