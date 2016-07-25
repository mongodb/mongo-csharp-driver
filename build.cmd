@echo off
Tools\NuGet\NuGet.exe install FAKE -OutputDirectory Tools -ExcludeVersion
Tools\NuGet\NuGet.exe install FAKE.Dotnet -OutputDirectory Tools -ExcludeVersion
Tools\NuGet\NuGet.exe install xunit.runner.console -OutputDirectory Tools -ExcludeVersion
Tools\FAKE\tools\Fake.exe build\build.fsx %*
