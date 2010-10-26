solution_file = "CSharpDriver-2008.sln"
configuration = "debug"
msbuild_version = "3.5"
nunit_path = "dependencies/NUnit/lib/nunit-console.exe"

desc "unless otherwise stated, it will build a release version and run all tests"
target default, (release, compile, test):
  pass
  
desc "set build configuration to Release"
target release:
  configuration = "release"
 
desc "Build the vs2010 version (using .NET 4.0's msbuild)"
target vs2010:
  msbuild_version = "4"

desc "Build the whole solution, in Debug by default, unless the release target was stated"
target compile:
   msbuild(file: solution_file, configuration: configuration, version: msbuild_version)

desc "Run unit tests"
target test, (compile):
	test_assemblies = ("BsonUnitTests/bin/${configuration}/MongoDB.BsonUnitTests.dll",
		"DriverOnlineTests/bin/${configuration}/MongoDB.DriverOnlineTests.dll",
		"DriverUnitTests/bin/${configuration}/MongoDB.DriverUnitTests.dll",
	)
	nunit(assemblies: test_assemblies, toolPath: nunit_path )
