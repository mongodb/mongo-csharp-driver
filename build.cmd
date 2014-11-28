@echo off
if not exist packages\FAKE\tools\Fake.exe ( 
	"tools\nuget\nuget.exe" "install" "FAKE" "-OutputDirectory" "packages" "-ExcludeVersion"
)
"packages\FAKE\tools\Fake.exe" build\build.fsx %*