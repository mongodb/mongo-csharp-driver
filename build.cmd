@echo off
Tools\NuGet\NuGet.exe install FAKE -OutputDirectory Tools -ExcludeVersion
Tools\FAKE\tools\Fake.exe build\build.fsx %*
