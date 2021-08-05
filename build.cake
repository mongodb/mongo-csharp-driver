#addin nuget:?package=Cake.FileHelpers&version=4.0.1
#addin nuget:?package=Cake.Git&version=1.0.1
#addin nuget:?package=Cake.Incubator&version=6.0.0
#tool dotnet:?package=GitVersion.Tool&version=5.6.9
#tool nuget:?package=JunitXml.TestLogger&version=3.0.98

using System;
using System.Text.RegularExpressions;
using System.Linq;
using Cake.Common.Tools.DotNetCore.DotNetCoreVerbosity;

const string defaultTarget = "Default";
var target = Argument("target", defaultTarget);
var configuration = Argument("configuration", "Release");

var gitVersion = GitVersion();

var solutionDirectory = MakeAbsolute(Directory("./"));
var artifactsDirectory = solutionDirectory.Combine("artifacts");
var artifactsBinDirectory = artifactsDirectory.Combine("bin");
var artifactsDocsDirectory = artifactsDirectory.Combine("docs");
var artifactsDocsApiDocsDirectory = artifactsDocsDirectory.Combine("ApiDocs-" + gitVersion.LegacySemVer);
var artifactsDocsRefDocsDirectory = artifactsDocsDirectory.Combine("RefDocs-" + gitVersion.LegacySemVer);
var artifactsPackagesDirectory = artifactsDirectory.Combine("packages");
var docsDirectory = solutionDirectory.Combine("Docs");
var docsApiDirectory = docsDirectory.Combine("Api");
var srcDirectory = solutionDirectory.Combine("src");
var testsDirectory = solutionDirectory.Combine("tests");
var outputDirectory = solutionDirectory.Combine("build");
var toolsDirectory = solutionDirectory.Combine("tools");
var toolsHugoDirectory = toolsDirectory.Combine("Hugo");
var artifactsPackagingTestsDirectory = artifactsDirectory.Combine("Packaging.Tests");
var localNugetSourceName = "LocalPackages";
var mongoDbDriverPackageName = "MongoDB.Driver";

var solutionFile = solutionDirectory.CombineWithFilePath("CSharpDriver.sln");
var solutionFullPath = solutionFile.FullPath;
var srcProjectNames = new[]
{
    "MongoDB.Bson",
    "MongoDB.Driver.Core",
    "MongoDB.Driver",
    "MongoDB.Driver.Legacy",
    "MongoDB.Driver.GridFS"
};

Task("Default")
    .IsDependentOn("Test");

Task("Release")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Docs")
    .IsDependentOn("Package");

Task("Restore")
    .Does(() =>
    {
        // disable parallel restore to work around apparent bugs in restore
        var restoreSettings = new DotNetCoreRestoreSettings
        {
            DisableParallel = true
        };
        DotNetCoreRestore(solutionFullPath, restoreSettings);
    });

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
       var settings = new DotNetCoreBuildSettings
       {
           NoRestore = true,
           Configuration = configuration,
           EnvironmentVariables = new Dictionary<string, string>
           {
               { "Version", gitVersion.LegacySemVer },
               { "SourceRevisionId", gitVersion.Sha }
           }
        };
        DotNetCoreBuild(solutionFullPath, settings);
    });

Task("BuildArtifacts")
    .IsDependentOn("Build")
    .Does(() =>
    {
        foreach (var targetFramework in new[] { "net472", "netstandard2.0", "netstandard2.1" })
        {
            var toDirectory = artifactsBinDirectory.Combine(targetFramework);
            CleanDirectory(toDirectory);

            var projects = new[] { "MongoDB.Bson", "MongoDB.Driver.Core", "MongoDB.Driver", "MongoDB.Driver.Legacy", "MongoDB.Driver.GridFS" };
            foreach (var project in projects)
            {
                var fromDirectory = srcDirectory.Combine(project).Combine("bin").Combine(configuration).Combine(targetFramework);

                var fileNames = new List<string>();
                foreach (var extension in new[] { "dll", "pdb", "xml" })
                {
                    var fileName = $"{project}.{extension}";
                    fileNames.Add(fileName);
                }

                // add additional files needed by Sandcastle
                if (targetFramework == "net472" && project == "MongoDB.Driver.Core")
                {
                    fileNames.Add("DnsClient.dll");
                    fileNames.Add("MongoDB.Libmongocrypt.dll");
                    fileNames.Add("SharpCompress.dll");
                }

                foreach (var fileName in fileNames)
                {
                    var fromFile = fromDirectory.CombineWithFilePath(fileName);
                    var toFile = toDirectory.CombineWithFilePath(fileName);
                    CopyFile(fromFile, toFile);
                }
            }
        }
    });

Task("Test")
    .IsDependentOn("Build")
    .DoesForEach(
        GetFiles("./**/*.Tests.csproj")
        .Where(name => !name.ToString().Contains("Atlas")),
        testProject =>
    {
        if (Environment.GetEnvironmentVariable("MONGODB_API_VERSION") != null &&
            testProject.ToString().Contains("Legacy"))
        {
            return; // Legacy tests are exempt from Version API testing
        }

        var testWithDefaultGuidRepresentationMode = Environment.GetEnvironmentVariable("TEST_WITH_DEFAULT_GUID_REPRESENTATION_MODE");
        if (testWithDefaultGuidRepresentationMode != null)
        {
            Console.WriteLine($"TEST_WITH_DEFAULT_GUID_REPRESENTATION_MODE={testWithDefaultGuidRepresentationMode}");
        }
        var testWithDefaultGuidRepresentation = Environment.GetEnvironmentVariable("TEST_WITH_DEFAULT_GUID_REPRESENTATION");
        if (testWithDefaultGuidRepresentation != null)
        {
            Console.WriteLine($"TEST_WITH_DEFAULT_GUID_REPRESENTATION={testWithDefaultGuidRepresentation}");
        }
        var mongoX509ClientCertificatePath = Environment.GetEnvironmentVariable("MONGO_X509_CLIENT_CERTIFICATE_PATH");
        if (mongoX509ClientCertificatePath != null)
        {
            Console.WriteLine($"MONGO_X509_CLIENT_CERTIFICATE_PATH={mongoX509ClientCertificatePath}");
        }
        var mongoX509ClientCertificatePassword = Environment.GetEnvironmentVariable("MONGO_X509_CLIENT_CERTIFICATE_PASSWORD");
        if (mongoX509ClientCertificatePassword != null)
        {
            Console.WriteLine($"MONGO_X509_CLIENT_CERTIFICATE_PASSWORD={mongoX509ClientCertificatePassword}");
        }

        var testResultsFile = outputDirectory.Combine("test-results").Combine($"TEST-{target.ToLowerInvariant()}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.xml");
        // Evergreen CI server requires JUnit output format to display test results
        var logger = $"junit;LogFilePath={testResultsFile};FailureBodyFormat=Verbose";
        var settings = new DotNetCoreTestSettings
        {
            NoBuild = true,
            NoRestore = true,
            Configuration = configuration,
            Logger = logger,
            ArgumentCustomization = args => args.Append("-- RunConfiguration.TargetPlatform=x64")
        };
        switch (target.ToLowerInvariant())
        {
            case "testnet472": settings.Framework = "net472"; break;
            case "testnetstandard20": settings.Framework = "netcoreapp2.1"; break;
            case "testnetstandard21": settings.Framework = "netcoreapp3.0"; break;
            default: ThrowIfNotDefaultTarget(target); break;
        }
        DotNetCoreTest(
            testProject.FullPath,
            settings
        );
    })
    .DeferOnError();

Task("TestNet472").IsDependentOn("Test");
Task("TestNetStandard20").IsDependentOn("Test");
Task("TestNetStandard21").IsDependentOn("Test");

Task("TestAwsAuthentication")
    .IsDependentOn("Build")
    .DoesForEach(
        GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        testProject =>
        {
            DotNetCoreTest(
                testProject.FullPath,
                new DotNetCoreTestSettings {
                    NoBuild = true,
                    NoRestore = true,
                    Configuration = configuration,
                    ArgumentCustomization = args => args.Append("-- RunConfiguration.TargetPlatform=x64"),
                    Filter = "Category=\"AwsMechanism\""
                }
            );
        });

Task("TestPlainAuthentication")
    .IsDependentOn("Build")
    .DoesForEach(
        GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        testProject =>
        {
            DotNetCoreTest(
                testProject.FullPath,
                new DotNetCoreTestSettings {
                    NoBuild = true,
                    NoRestore = true,
                    Configuration = configuration,
                    ArgumentCustomization = args => args.Append("-- RunConfiguration.TargetPlatform=x64"),
                    Filter = "Category=\"PlainMechanism\""
                }
            );
        });

// currently we are not running this Task on Evergreen (only locally occassionally)
Task("TestAllGuidRepresentations")
    .IsDependentOn("Build")
    .DoesForEach(
        GetFiles("./**/*.Tests.csproj")
        // .Where(name => name.ToString().Contains("Bson.Tests")) // uncomment to only test Bson
        .Where(name => !name.ToString().Contains("Atlas")),
        testProject =>
    {
        var modes = new string[][]
        {
            new[] { "V2", "Unspecified" },
            new[] { "V2", "JavaLegacy" },
            new[] { "V2", "Standard" },
            new[] { "V2", "PythonLegacy" },
            new[] { "V2", "CSharpLegacy" },
            new[] { "V3", "Unspecified" }
        };

        foreach (var mode in modes)
        {
            var testWithGuidRepresentationMode = mode[0];
            var testWithGuidRepresentation = mode[1];
            Console.WriteLine($"TEST_WITH_DEFAULT_GUID_REPRESENTATION_MODE={testWithGuidRepresentationMode}");
            Console.WriteLine($"TEST_WITH_DEFAULT_GUID_REPRESENTATION={testWithGuidRepresentation}");

            DotNetCoreTest(
                testProject.FullPath,
                new DotNetCoreTestSettings {
                    NoBuild = true,
                    NoRestore = true,
                    Configuration = configuration,
                    ArgumentCustomization = args => args.Append("-- RunConfiguration.TargetPlatform=x64"),
                    EnvironmentVariables = new Dictionary<string, string>
                    {
                        { "TEST_WITH_DEFAULT_GUID_REPRESENTATION_MODE", testWithGuidRepresentationMode },
                        { "TEST_WITH_DEFAULT_GUID_REPRESENTATION", testWithGuidRepresentation }
                    }
                }
            );
        }
    });

Task("TestAtlasConnectivity")
    .IsDependentOn("Build")
    .DoesForEach(
        GetFiles("./**/AtlasConnectivity.Tests.csproj"),
        testProject =>
{
    DotNetCoreTest(
        testProject.FullPath,
        new DotNetCoreTestSettings {
            NoBuild = true,
            NoRestore = true,
            Configuration = configuration,
            ArgumentCustomization = args => args.Append("-- RunConfiguration.TargetPlatform=x64")
        }
    );
});

Task("TestAtlasDataLake")
    .IsDependentOn("Build")
    .DoesForEach(
        GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        testProject =>
        {
            DotNetCoreTest(
                testProject.FullPath,
                new DotNetCoreTestSettings {
                    NoBuild = true,
                    NoRestore = true,
                    Configuration = configuration,
                    ArgumentCustomization = args => args.Append("-- RunConfiguration.TargetPlatform=x64"),
                    Filter = "Category=\"AtlasDataLake\""
                }
            );
        });

Task("TestOcsp")
    .IsDependentOn("Build")
    .DoesForEach(
        GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        testProject =>
{
    DotNetCoreTest(
        testProject.FullPath,
        new DotNetCoreTestSettings {
            NoBuild = true,
            NoRestore = true,
            Configuration = configuration,

            ArgumentCustomization = args => args
                .Append("--filter FullyQualifiedName~OcspIntegrationTests")
                .Append("-- RunConfiguration.TargetPlatform=x64")
        }
    );
});

Task("TestGssapi")
    .IsDependentOn("Build")
    .DoesForEach(
        GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        testProject =>
    {
        var settings = new DotNetCoreTestSettings
        {
            NoBuild = true,
            NoRestore = true,
            Configuration = configuration,
            ArgumentCustomization = args => args.Append("-- RunConfiguration.TargetPlatform=x64"),
            Filter = "Category=\"GssapiMechanism\""
        };
        switch (target.ToLowerInvariant())
        {
            case "testgssapinet472": settings.Framework = "net472"; break;
            case "testgssapinetstandard20": settings.Framework = "netcoreapp2.1"; break;
            case "testgssapinetstandard21": settings.Framework = "netcoreapp3.0"; break;
            default: ThrowIfNotDefaultTarget(target); break;
        }
        DotNetCoreTest(
            testProject.FullPath,
            settings
        );
    });

Task("TestGssapiNet472").IsDependentOn("TestGssapi");
Task("TestGssapiNetStandard20").IsDependentOn("TestGssapi");
Task("TestGssapiNetStandard21").IsDependentOn("TestGssapi");

Task("TestServerless")
    .IsDependentOn("Build")
    .DoesForEach(
        GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        testProject =>
        {
            var settings = new DotNetCoreTestSettings
            {
                NoBuild = true,
                NoRestore = true,
                Configuration = configuration,
                ArgumentCustomization = args => args.Append("-- RunConfiguration.TargetPlatform=x64"),
                Filter = "Category=\"Serverless\""
            };
            switch (target.ToLowerInvariant())
            {
                case "testserverlessnet472": settings.Framework = "net472"; break;
                case "testserverlessnetstandard20": settings.Framework = "netcoreapp2.1"; break;
                case "testserverlessnetstandard21": settings.Framework = "netcoreapp3.0"; break;
                default: ThrowIfNotDefaultTarget(target); break;
            }
            DotNetCoreTest(
                testProject.FullPath,
                settings
            );
        });

Task("TestServerlessNet472").IsDependentOn("TestServerless");
Task("TestServerlessNetStandard20").IsDependentOn("TestServerless");
Task("TestServerlessNetStandard21").IsDependentOn("TestServerless");

Task("TestLoadBalanced")
    .IsDependentOn("Build")
    .DoesForEach(
        GetFiles("./**/*.Tests.csproj"),
        testProject =>
     {
        var settings = new DotNetCoreTestSettings
        {
            NoBuild = true,
            NoRestore = true,
            Configuration = configuration,
            ArgumentCustomization = args => args.Append("-- RunConfiguration.TargetPlatform=x64"),
            Filter = "Category=\"SupportLoadBalancing\""
        };

        switch (target.ToLowerInvariant())
        {
            case "testloadbalancednetstandard20": settings.Framework = "netcoreapp2.1"; break;
            case "testloadbalancednetstandard21": settings.Framework = "netcoreapp3.0"; break;
        }

        DotNetCoreTest(
            testProject.FullPath,
            settings
        );
     });

Task("TestLoadBalancedNetStandard20").IsDependentOn("TestLoadBalanced");
Task("TestLoadBalancedNetStandard21").IsDependentOn("TestLoadBalanced");

Task("Docs")
    .IsDependentOn("ApiDocs")
    .IsDependentOn("RefDocs");

Task("ApiDocs")
    .IsDependentOn("BuildArtifacts")
    .Does(() =>
    {
        EnsureDirectoryExists(artifactsDocsApiDocsDirectory);
        CleanDirectory(artifactsDocsApiDocsDirectory);

        var shfbprojFile = docsApiDirectory.CombineWithFilePath("CSharpDriverDocs.shfbproj");
        var preliminary = false; // TODO: compute
        MSBuild(shfbprojFile, new MSBuildSettings
            {
                Configuration = "Release"
            }
            .WithProperty("OutputPath", artifactsDocsApiDocsDirectory.ToString())
            .WithProperty("CleanIntermediate", "True")
            .WithProperty("Preliminary", preliminary ? "True" : "False")
            .WithProperty("HelpFileVersion", gitVersion.LegacySemVer)
        );

        var lowerCaseIndexFile = artifactsDocsApiDocsDirectory.CombineWithFilePath("index.html");
        var upperCaseIndexFile = artifactsDocsApiDocsDirectory.CombineWithFilePath("Index.html");
        MoveFile(upperCaseIndexFile, lowerCaseIndexFile);

        var chmFile = artifactsDocsApiDocsDirectory.CombineWithFilePath("CSharpDriverDocs.chm");
        var artifactsDocsChmFile = artifactsDocsDirectory.CombineWithFilePath("CSharpDriverDocs.chm");
        CopyFile(chmFile, artifactsDocsChmFile);
    });

Task("RefDocs")
    .Does(() =>
    {
        EnsureDirectoryExists(toolsHugoDirectory);
        CleanDirectory(toolsHugoDirectory);

        var url = "https://github.com/spf13/hugo/releases/download/v0.13/hugo_0.13_windows_amd64.zip";
        var hugoZipFile = toolsHugoDirectory.CombineWithFilePath("hugo_0.13_windows_amd64.zip");
        DownloadFile(url, hugoZipFile);
        Unzip(hugoZipFile, toolsHugoDirectory);
        var hugoExe = toolsHugoDirectory.CombineWithFilePath("hugo_0.13_windows_amd64.exe");

        var landingDirectory = docsDirectory.Combine("landing");
        var landingPublicDirectory = landingDirectory.Combine("public");
        CleanDirectory(landingPublicDirectory);

        var processSettings = new ProcessSettings
        {
            WorkingDirectory = landingDirectory
        };
        StartProcess(hugoExe, processSettings);

        var referenceDirectory = docsDirectory.Combine("reference");
        var referencePublicDirectory = referenceDirectory.Combine("public");
        CleanDirectory(referencePublicDirectory);

        processSettings = new ProcessSettings
        {
            WorkingDirectory = referenceDirectory
        };
        StartProcess(hugoExe, processSettings);

        EnsureDirectoryExists(artifactsDocsRefDocsDirectory);
        CleanDirectory(artifactsDocsRefDocsDirectory);

        CopyDirectory(landingPublicDirectory, artifactsDocsRefDocsDirectory);

        var artifactsReferencePublicDirectory = artifactsDocsRefDocsDirectory.Combine(gitVersion.Major + "." + gitVersion.Minor);
        CopyDirectory(referencePublicDirectory, artifactsReferencePublicDirectory);
    });

Task("Package")
    .IsDependentOn("PackageNugetPackages");

Task("PackageNugetPackages")
    .IsDependentOn("Build")
    .Does(() =>
    {
        EnsureDirectoryExists(artifactsPackagesDirectory);
        CleanDirectory(artifactsPackagesDirectory);

        var projects = new[]
        {
            "MongoDB.Bson",
            "MongoDB.Driver.Core",
            "MongoDB.Driver",
            "MongoDB.Driver.GridFS",
            "MongoDB.Driver.Legacy"
        };

        foreach (var project in projects)
        {
            var projectPath = $"{srcDirectory}\\{project}\\{project}.csproj";
            var settings = new DotNetCorePackSettings
            {
                Configuration = configuration,
                OutputDirectory = artifactsPackagesDirectory,
                NoBuild = true,
                IncludeSymbols = true,
                MSBuildSettings = new DotNetCoreMSBuildSettings()
                    .WithProperty("PackageVersion", gitVersion.LegacySemVer)
            };
            DotNetCorePack(projectPath, settings);
        }
    });

Task("PushToNuGet")
    .Does(() =>
    {
        var nugetApiKey = EnvironmentVariable("NUGETAPIKEY");
        if (nugetApiKey == null)
        {
            throw new Exception("NUGETAPIKEY environment variable missing");
        }

        var packageFiles = new List<FilePath>();

        var projects = new[]
        {
            "MongoDB.Bson",
            "MongoDB.Driver.Core",
            "MongoDB.Driver",
            "MongoDB.Driver.GridFS",
            "mongocsharpdriver" // the Nuget package name for MongoDB.Driver.Legacy
        };

        foreach (var project in projects)
        {
            var packageFileName = $"{project}.{gitVersion.LegacySemVer}.nupkg";
            var packageFile = artifactsPackagesDirectory.CombineWithFilePath(packageFileName);
            packageFiles.Add(packageFile);
        }

        NuGetPush(packageFiles, new NuGetPushSettings
        {
            ApiKey = nugetApiKey,
            Source = "https://api.nuget.org/v3/index.json"
        });
    });

Task("DumpGitVersion")
    .Does(() =>
    {
        Information(gitVersion.Dump());
    });

Task("PreparePackagingTests")
    .Does(() =>
    {
        var nugetConfigPath = artifactsDirectory.CombineWithFilePath("nuget.config");
        if (FileExists(nugetConfigPath)) DeleteFile(nugetConfigPath);

        CreateNugetConfig(nugetConfigPath);

        void CreateNugetConfig(FilePath filePath)    
        {
            DotNetCoreTool(filePath, "new nugetconfig"); // create a default nuget.config

            // <packageSources>
            //     <add key="{localNugetSourceName}" value="packages" />
            // </packageSources>
            XmlPoke(filePath, "/configuration/packageSources/add/@key", $"{localNugetSourceName}");
            XmlPoke(filePath, $"/configuration/packageSources/add[@key = '{localNugetSourceName}']/@value", "packages");
        }
    });
    
Task("PackagingProjectReferenceTests")
    .IsDependentOn("Build")
    .DoesForEach(
        GetFiles("./**/*.Tests.csproj"),
        testProject =>
     {
        var settings = new DotNetCoreTestSettings
        {
            NoBuild = true,
            NoRestore = true,
            Configuration = configuration,
            ArgumentCustomization = args => args.Append("-- RunConfiguration.TargetPlatform=x64"),
            Filter = "Category=\"Packaging\""
        };

        DotNetCoreTest(
            testProject.FullPath,
            settings
        );
     });

Task("PackagingTests")
    .IsDependentOn("PackagingProjectReferenceTests")
    .IsDependentOn("Package")
    .IsDependentOn("PreparePackagingTests")
    .DoesForEach(
    () => 
    {
        var packagesList = NuGetList(
            new NuGetListSettings {
                AllVersions = true,
                Prerelease = true,
                Source = new [] { $"{localNugetSourceName}" }, // corresponds to artifacts Nuget.config
                WorkingDirectory = artifactsDirectory
            });

        foreach(var package in packagesList)
        {
            Information("Found packages {0}, version {1}", package.Name, package.Version);
        }
        if (packagesList.Count(p => p.Name == mongoDbDriverPackageName) != 1)
        {
            throw new Exception($"Package {mongoDbDriverPackageName} must be presented and unique.");
        }
        var mongoDriverPackageVersion = packagesList.Single(p => p.Name == mongoDbDriverPackageName).Version;
        Information($"Package version {mongoDriverPackageVersion}");

        var testDetails = new List<(string Moniker, string CsprojType, string Version, string Bitness, string Command)>
        {
            /* Should be considered in https://jira.mongodb.org/browse/CSHARP-3805
            { ("v4.7.2", "NonSdk", mongoDriverPackageVersion) }, */

            { ("net472", "SDK", mongoDriverPackageVersion, "x64", Command: "xunit") }, // TODO: add x32
            { ("netcoreapp21", "SDK", mongoDriverPackageVersion, "x64", Command: "xunit") },
            { ("netcoreapp30", "SDK", mongoDriverPackageVersion, "x64", Command: "xunit") },
            { ("net50", "SDK", mongoDriverPackageVersion, "x64", Command: "xunit") }
        };

        // run the same tests on console app
        testDetails.AddRange(testDetails.ToArray().Select(i => (i.Moniker, i.CsprojType, i.Version, i.Bitness, Command: "console")));
        
        return testDetails;
    },
    (testDetails) => 
    {
        var moniker = testDetails.Moniker;
        var csprojFormat = testDetails.CsprojType;
        var mongoDriverPackageVersion = testDetails.Version;
        var command = testDetails.Command;

        Information($"Moniker: {moniker}, csproj style: {csprojFormat}");

        var monikerTestFolder = artifactsPackagingTestsDirectory.Combine($"{moniker}_{csprojFormat}_{command}");
        Information($"Moniker test folder: {monikerTestFolder}");
        EnsureDirectoryExists(monikerTestFolder);
        CleanDirectory(monikerTestFolder);

        var csprojFileName = $"{monikerTestFolder.GetDirectoryName()}.csproj"; 
        var csprojFullPath = monikerTestFolder.CombineWithFilePath(csprojFileName);

        switch (command)
        {
            case "xunit":
                {
                    if (moniker == "net472")
                    {
                        // CSHARP-3806
                        return;
                    }

                    Information("Creating test project...");
                    DotNetCoreTool(csprojFullPath, "new xunit", $"--target-framework-override {moniker} --language C# ");
                    Information("Created test project");

                    Information($"Adding FluentAssertions...");
                    DotNetCoreTool(
                        csprojFullPath,
                        "add package FluentAssertions",
                        $"--framework {moniker} --version 4.12.0"
                    );
                    Information($"Added FluentAssertions");

                    Information($"Adding test package...");
                    DotNetCoreTool(
                        csprojFullPath,
                        $"add package {mongoDbDriverPackageName}",
                        $"--framework {moniker} --version {mongoDriverPackageVersion}"
                    );
                    Information("Added tested package");

                    DeleteFile(monikerTestFolder.CombineWithFilePath("UnitTest1.cs")); // Remove a default unit test
                    var packagingTestsDirectory	= testsDirectory.Combine("MongoDB.Driver.Tests").Combine("Packaging");
                    Console.WriteLine($"Original test file {packagingTestsDirectory}");
                    var files = GetFiles($"{packagingTestsDirectory}/*.cs").ToList();
                    CopyFiles(files, monikerTestFolder); // copy tests content

                    Information("Running tests...");  
                    DotNetCoreTest(
                        csprojFullPath.ToString(),
                        new DotNetCoreTestSettings
                        {
                            Framework = moniker,
                            Configuration = configuration,
                            ArgumentCustomization = args => args.Append($"-- RunConfiguration.TargetPlatform={testDetails.Bitness}")
                        }
                    );
                } 
                break;
            case "console":
                {
                    Information("Creating console project...");
                    DotNetCoreTool(csprojFullPath, "new console", $"--target-framework-override {moniker} --language C# ");
                    Information("Created test project");

                    Information($"Adding tested package...");
                    DotNetCoreTool(
                        csprojFullPath,
                        $"add package {mongoDbDriverPackageName}",
                        $"--framework {moniker} --version {mongoDriverPackageVersion}"
                    );
                    Information("Added test package");

                    // the below two packages are added just to allow using the same code as in xunit
                    Information($"Adding FluentAssertions...");
                    DotNetCoreTool(
                        csprojFullPath,
                        "add package FluentAssertions",
                        $"--framework {moniker} --version 4.12.0"
                    );
                    Information($"Added FluentAssertions");

                    Information($"Adding xunit...");
                    DotNetCoreTool(
                        csprojFullPath,
                        "add package xunit",
                        $"--framework {moniker} --version 2.4.0"
                    );
                    Information($"Added xunit");

                    DeleteFile(monikerTestFolder.CombineWithFilePath("Program.cs")); // Remove a default .cs file
                    var packagingTestsDirectory	= testsDirectory.Combine("MongoDB.Driver.Tests").Combine("Packaging");
                    Console.WriteLine($"Original test file {packagingTestsDirectory}");
                    var files = GetFiles($"{packagingTestsDirectory}/*.cs").ToList();
                    CopyFiles(files, monikerTestFolder); // copy tests content

                    Information("Running console app...");  
                    DotNetCoreRun(
                        csprojFullPath.ToString(),
                        new DotNetCoreRunSettings
                        {
                            EnvironmentVariables = new Dictionary<string, string>() 
                            { 
                                { "DefineConstants", "CONSOLE_TEST" }
                            },
                            Framework = moniker,
                            Configuration = configuration,
                            ArgumentCustomization = args => args.Append($"-- RunConfiguration.TargetPlatform={testDetails.Bitness}")
                        }
                    );
                } 
                break;
            default: throw new NotSupportedException($"Packaging tests for {testDetails.Command} is not supported.");
        }
    })
    .DeferOnError();

RunTarget(target);

void ThrowIfNotDefaultTarget(string @value)
{
    if (@value != defaultTarget)
    {
       throw new ArgumentException($"Unexpected target: \"{target}\".");
    }
}
