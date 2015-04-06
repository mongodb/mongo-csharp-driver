#!/bin/bash
mono --runtime=v4.0 Tools/NuGet/NuGet.exe install FAKE -OutputDirectory Tools -ExcludeVersion
mono --runtime=v4.0 Tools/FAKE/tools/FAKE.exe $@ --fsiargs -d:MONO ./build/build.fsx
