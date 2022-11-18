#addin nuget:?package=Cake.FileHelpers&version=5.0.0
#addin nuget:?package=Cake.Git&version=2.0.0
#addin nuget:?package=Cake.Incubator&version=7.0.0
#tool dotnet:?package=GitVersion.Tool&version=5.10.3
#tool nuget:?package=JunitXml.TestLogger&version=3.0.114

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Cake.Common.Tools.DotNet.DotNetVerbosity;
using Architecture = System.Runtime.InteropServices.Architecture;
using Path = Cake.Core.IO.Path;

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
    .IsDependentOn("Docs")
    .IsDependentOn("Package");

Task("Restore")
    .Does(() =>
    {
        // disable parallel restore to work around apparent bugs in restore
        var restoreSettings = new DotNetRestoreSettings
        {
            DisableParallel = true
        };
        DotNetRestore(solutionFullPath, restoreSettings);
    });

Task("Build")
    .IsDependentOn("Restore")
    .Does<BuildConfig>((buildConfig) =>
    {
       var settings = new DotNetBuildSettings
       {
           NoRestore = true,
           Configuration = configuration,
           EnvironmentVariables = new Dictionary<string, string>
           {
               { "Version", gitVersion.LegacySemVer },
               { "SourceRevisionId", gitVersion.Sha }
           }
        };

        if (buildConfig.IsReleaseMode)
        {
            Console.WriteLine("Build continuousIntegration is enabled");
            settings.MSBuildSettings = new DotNetMSBuildSettings();
            // configure deterministic build for better compatibility with debug symbols (used in Package/Build tasks). Affects: *.nupkg
            settings.MSBuildSettings.SetContinuousIntegrationBuild(continuousIntegrationBuild: true);
        }
        DotNetBuild(solutionFullPath, settings);
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
                    fileNames.Add("AWSSDK.Core.dll");
                    fileNames.Add("DnsClient.dll");
                    fileNames.Add("Microsoft.Extensions.Logging.Abstractions.dll");
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
        items: GetFiles("./**/*.Tests.csproj").Where(name => !name.ToString().Contains("Atlas")),
        action: (BuildConfig buildConfig, Path testProject) =>
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

        RunTests(buildConfig, testProject);
    })
    .DeferOnError();

Task("TestNet472").IsDependentOn("Test");
Task("TestNetStandard20").IsDependentOn("Test");
Task("TestNetStandard21").IsDependentOn("Test");
Task("TestNet60").IsDependentOn("Test");

Task("TestAwsAuthentication")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"AwsMechanism\""));

Task("TestPlainAuthentication")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) => 
            RunTests(buildConfig, testProject, filter: "Category=\"PlainMechanism\""));

// currently we are not running this Task on Evergreen (only locally occassionally)
Task("TestAllGuidRepresentations")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj")
        // .Where(name => name.ToString().Contains("Bson.Tests")) // uncomment to only test Bson
        .Where(name => !name.ToString().Contains("Atlas")),
        action: (BuildConfig buildConfig, Path testProject) =>
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

            RunTests(
                buildConfig, 
                testProject,
                settings =>
                {
                    settings.EnvironmentVariables = new Dictionary<string, string>
                    {
                        { "TEST_WITH_DEFAULT_GUID_REPRESENTATION_MODE", testWithGuidRepresentationMode },
                        { "TEST_WITH_DEFAULT_GUID_REPRESENTATION", testWithGuidRepresentation }
                    };
                });
        }
    });

Task("TestAtlasConnectivity")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/AtlasConnectivity.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) => RunTests(buildConfig, testProject));

Task("TestAtlasDataLake")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
           RunTests(buildConfig, testProject, filter: "Category=\"AtlasDataLake\""));

Task("TestAtlasSearch")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
           RunTests(buildConfig, testProject, filter: "Category=\"AtlasSearch\""));

Task("TestOcsp")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"OCSP\""));

Task("TestGssapi")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>       
           RunTests(buildConfig, testProject, filter: "Category=\"GssapiMechanism\""));

Task("TestGssapiNet472").IsDependentOn("TestGssapi");
Task("TestGssapiNetStandard20").IsDependentOn("TestGssapi");
Task("TestGssapiNetStandard21").IsDependentOn("TestGssapi");
Task("TestGssapiNet60").IsDependentOn("TestGssapi");

Task("TestServerless")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"Serverless\""));

Task("TestServerlessNet472").IsDependentOn("TestServerless");
Task("TestServerlessNetStandard20").IsDependentOn("TestServerless");
Task("TestServerlessNetStandard21").IsDependentOn("TestServerless");
Task("TestServerlessNet60").IsDependentOn("TestServerless");

Task("TestLoadBalanced")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"SupportLoadBalancing\""));

Task("TestLoadBalancedNetStandard20").IsDependentOn("TestLoadBalanced");
Task("TestLoadBalancedNetStandard21").IsDependentOn("TestLoadBalanced");
Task("TestLoadBalancedNet60").IsDependentOn("TestLoadBalanced");

Task("TestCsfleWithMockedKms")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"CSFLE\""));

Task("TestCsfleWithMockedKmsNet472").IsDependentOn("TestCsfleWithMockedKms");
Task("TestCsfleWithMockedKmsNetStandard20").IsDependentOn("TestCsfleWithMockedKms");
Task("TestCsfleWithMockedKmsNetStandard21").IsDependentOn("TestCsfleWithMockedKms");
Task("TestCsfleWithMockedKmsNet60").IsDependentOn("TestCsfleWithMockedKms");

Task("TestCsfleWithMongocryptd")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"CSFLE\""));

Task("TestCsfleWithMongocryptdNet472").IsDependentOn("TestCsfleWithMongocryptd");
Task("TestCsfleWithMongocryptdNetStandard20").IsDependentOn("TestCsfleWithMongocryptd");
Task("TestCsfleWithMongocryptdNetStandard21").IsDependentOn("TestCsfleWithMongocryptd");
Task("TestCsfleWithMongocryptdNet60").IsDependentOn("TestCsfleWithMongocryptd");

Task("TestCsfleWithAzureKms")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"CsfleAZUREKMS\""));

Task("TestCsfleWithGcpKms")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"CsfleGCPKMS\""));

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
    .Does<BuildConfig>((buildConfig) =>
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
            var settings = new DotNetPackSettings
            {
                Configuration = configuration,
                OutputDirectory = artifactsPackagesDirectory,
                NoBuild = true, // SetContinuousIntegrationBuild is enabled for nupkg on the Build step
                IncludeSymbols = true,
                MSBuildSettings = new DotNetMSBuildSettings()
                    // configure deterministic build for better compatibility with debug symbols (used in Package/Build tasks). Affects: *.snupkg
                    .SetContinuousIntegrationBuild(continuousIntegrationBuild: true)
                    .WithProperty("PackageVersion", buildConfig.PackageVersion)
            };
            DotNetPack(projectPath, settings);
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

Task("TestsPackagingProjectReference")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) => 
            RunTests(buildConfig, testProject, filter: "Category=\"Packaging\""));

Task("SmokeTests")
    .IsDependentOn("PackageNugetPackages")
    .DoesForEach(
        GetFiles("./**/SmokeTests/**/*.SmokeTests*.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
     {
        var environmentVariables = new Dictionary<string, string>
        {
           { "SmokeTestsPackageSha", gitVersion.Sha }
        };

        var toolSettings = new DotNetToolSettings { EnvironmentVariables = environmentVariables };

        Information($"Updating MongoDB package: {buildConfig.PackageVersion} sha: {gitVersion.Sha}");

        DotNetTool(
            testProject.FullPath,
            "add package MongoDB.Driver",
            $"--version [{buildConfig.PackageVersion}]",
            toolSettings);

        RunTests(
            buildConfig, 
            testProject, 
            settings =>
            {
                settings.NoBuild = false;
                settings.NoRestore = false;
                settings.EnvironmentVariables = environmentVariables;
            });
     });

Task("SmokeTestsNet472").IsDependentOn("SmokeTests");
Task("SmokeTestsNetCoreApp21").IsDependentOn("SmokeTests");
Task("SmokeTestsNetCoreApp31").IsDependentOn("SmokeTests");
Task("SmokeTestsNet50").IsDependentOn("SmokeTests");
Task("SmokeTestsNet60").IsDependentOn("SmokeTests");

Task("TestsPackaging")
    .IsDependentOn("TestsPackagingProjectReference")
    .IsDependentOn("Package")
    .DoesForEach(
    () =>
    {
        var monikers = new[] { "net472", "netcoreapp21", "netcoreapp30", "net50", "net60" };
        var csprojTypes = new[] { "SDK" };
        var processorArchitectures = new[] { "x64", "arm64" };
        var projectTypes = new[] { "xunit", "console" };

        return
            from moniker in monikers
            from csprojType in csprojTypes
            from processorArchitecture in processorArchitectures
            from projectType in projectTypes
            select new { Moniker = moniker, CsprojType = csprojType, ProcessorArchitecture = processorArchitecture, ProjectType = projectType };
    },
    (testDetails) =>
    {
        var moniker = testDetails.Moniker;
        var csprojFormat = testDetails.CsprojType;
        var projectType = testDetails.ProjectType;
        var processorArchitecture = testDetails.ProcessorArchitecture;
        var localNugetSourceName = "LocalPackages";

        Information($"Moniker: {moniker}, csproj style: {csprojFormat}");

        var monikerTestFolder = artifactsPackagingTestsDirectory.Combine($"{moniker}_{csprojFormat}_{projectType}_{processorArchitecture}");
        Information($"Moniker test folder: {monikerTestFolder}");
        EnsureDirectoryExists(monikerTestFolder);
        CleanDirectory(monikerTestFolder);

        var csprojFileName = $"{monikerTestFolder.GetDirectoryName()}.csproj";
        var csprojFullPath = monikerTestFolder.CombineWithFilePath(csprojFileName);

        switch (projectType)
        {
            case "xunit":
                {
                    if (moniker == "net472")
                    {
                        // CSHARP-3806
                        return;
                    }

                    Information("Creating test project...");
                    DotNetTool(csprojFullPath, "new xunit", $"--target-framework-override {moniker} --language C# ");
                    Information("Created test project");

                    // the below two packages are added just to allow using the same code as in xunit
                    Information($"Adding FluentAssertions...");
                    DotNetTool(
                        csprojFullPath,
                        "add package FluentAssertions",
                        $"--framework {moniker} --version 4.12.0"
                    );
                    Information($"Added FluentAssertions");

                    var mongoDriverPackageVersion = ConfigureAndGetTestedDriverVersion(monikerTestFolder, localNugetSourceName);

                    Information($"Adding test package...");
                    DotNetTool(
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
                    DotNetTest(
                        csprojFullPath.ToString(),
                        new DotNetTestSettings
                        {
                            Framework = moniker,
                            Configuration = configuration,
                            ArgumentCustomization = args => 
                                args
                                .Append("/p:LangVersion=9")
                                .Append($"-- RunConfiguration.TargetPlatform={processorArchitecture}")
                        }
                    );
                }
                break;
            case "console":
                {
                    if (moniker == "netcoreapp21")
                    {
                        // https://github.com/dotnet/sdk/issues/8662
                        // The described solution works but it's tricky to implement it via scripts
                        return;
                    }

                    Information("Creating console project...");
                    DotNetTool(csprojFullPath, "new console", $"--target-framework-override {moniker} --language C# --langVersion 9");
                    Information("Created test project");

                    // the below two packages are added just to allow using the same code as in xunit
                    Information($"Adding FluentAssertions...");
                    DotNetTool(
                        csprojFullPath,
                        "add package FluentAssertions",
                        $"--framework {moniker} --version 4.12.0"
                    );
                    Information($"Added FluentAssertions");

                    Information($"Adding xunit...");
                    DotNetTool(
                        csprojFullPath,
                        "add package xunit",
                        $"--framework {moniker} --version 2.4.0"
                    );
                    Information($"Added xunit");

                    var mongoDriverPackageVersion = ConfigureAndGetTestedDriverVersion(monikerTestFolder, localNugetSourceName);

                    Information($"Adding tested package...");
                    DotNetTool(
                        csprojFullPath,
                        $"add package {mongoDbDriverPackageName}",
                        $"--framework {moniker} --version {mongoDriverPackageVersion}"
                    );
                    Information("Added test package");

                    DeleteFile(monikerTestFolder.CombineWithFilePath("Program.cs")); // Remove a default .cs file
                    var packagingTestsDirectory	= testsDirectory.Combine("MongoDB.Driver.Tests").Combine("Packaging");
                    Console.WriteLine($"Original test file {packagingTestsDirectory}");
                    var files = GetFiles($"{packagingTestsDirectory}/*.cs").ToList();
                    CopyFiles(files, monikerTestFolder); // copy tests content

                    Information("Running console app...");
                    DotNetRun(
                        csprojFullPath.ToString(),
                        new DotNetRunSettings
                        {
                            EnvironmentVariables = new Dictionary<string, string>()
                            {
                                { "DefineConstants", "CONSOLE_TEST" },
                                { "PlatformTarget", processorArchitecture }
                            },
                            Framework = moniker,
                            Configuration = configuration
                        }
                    );
                }
                break;
            default: throw new NotSupportedException($"Packaging tests for {projectType} is not supported.");
        }

        string ConfigureAndGetTestedDriverVersion(DirectoryPath directoryPath, string localNugetSourceName)
        {
            CreateNugetConfig(directoryPath, localNugetSourceName);

            var packagesList = NuGetList(
            new NuGetListSettings {
                AllVersions = true,
                Prerelease = true,
                Source = new [] { $"{localNugetSourceName}" }, // corresponds to artifacts Nuget.config
                WorkingDirectory = directoryPath
            });

            foreach(var package in packagesList)
            {
                Information("Found package {0}, version {1}", package.Name, package.Version);
            }
            if (packagesList.Count(p => p.Name == mongoDbDriverPackageName) != 1)
            {
                throw new Exception($"Package {mongoDbDriverPackageName} must be presented and unique.");
            }
            var mongoDriverPackageVersion = packagesList.Single(p => p.Name == mongoDbDriverPackageName).Version;
            Information($"Package version {mongoDriverPackageVersion}");
            return mongoDriverPackageVersion;

            void CreateNugetConfig(DirectoryPath directoryPath, string localNugetSourceName)
            {
                var nugetConfigPath = directoryPath.CombineWithFilePath("nuget.config");
                if (FileExists(nugetConfigPath)) DeleteFile(nugetConfigPath);

                DotNetTool(nugetConfigPath, "new nugetconfig"); // create a default nuget.config

                // <packageSources>
                //     <add key="{localNugetSourceName}" value="..\..\packages" />
                // </packageSources>
                XmlPoke(nugetConfigPath, "/configuration/packageSources/add/@key", $"{localNugetSourceName}");
                XmlPoke(nugetConfigPath, $"/configuration/packageSources/add[@key = '{localNugetSourceName}']/@value", @"..\..\packages");
            }
        }
    })
    .DeferOnError();

Setup<BuildConfig>(
    setupContext =>
    {
        var targetPlatform = RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x64",
            var unknownArchitecture => throw new Exception($"Unknown CPU architecture: {unknownArchitecture}.")
        };

        var lowerTarget = target.ToLowerInvariant();
        // Apple M1 (arm64) must run on .NET 6 as the hosting process is arm64 and cannot load the previous netcoreapp2.1/3.1 runtimes.
        // While Rosetta 2 can cross-compile x64->arm64 to run x64 code, it requires a completely separate install of the .NET runtimes
        // in a different directory with a x64 dotnet host process. This would further complicate our testing for little additional gain.
        var framework = targetPlatform == "arm64" ? "net6.0" : lowerTarget switch
        {
            string s when s.EndsWith("net472") => "net472",
            string s when s.EndsWith("netstandard20") || s.EndsWith("netcoreapp21") => "netcoreapp2.1",
            string s when s.EndsWith("netstandard21") || s.EndsWith("netcoreapp31") => "netcoreapp3.1",
            string s when s.EndsWith("net472") => "net472",
            string s when s.EndsWith("net50") => "net5.0",
            string s when s.EndsWith("net60") => "net6.0",
            _ => null
        };

        var isReleaseMode = lowerTarget.StartsWith("package") || lowerTarget == "release";
        var packageVersion = lowerTarget.StartsWith("smoketests") ? gitVersion.FullSemVer.Replace('+', '-') : gitVersion.LegacySemVer;

        Console.WriteLine($"Framework: {framework ?? "null (not set)"}, TargetPlatform: {targetPlatform}, IsReleaseMode: {isReleaseMode}, PackageVersion: {packageVersion}");
        var loggers = CreateLoggers();
        return new BuildConfig(isReleaseMode, framework, targetPlatform, packageVersion, loggers);
    });

RunTarget(target);

public class BuildConfig
{
    public bool IsReleaseMode { get; }
    public string Framework { get; }
    public string PackageVersion { get; }
    public string TargetPlatform { get; }
    public string[] Loggers { get; }

    public BuildConfig(bool isReleaseMode, string framework, string targetPlatform, string packageVersion, string[] loggers)
    {
        IsReleaseMode = isReleaseMode;
        Framework = framework;
        TargetPlatform = targetPlatform;
        PackageVersion = packageVersion;
        Loggers = loggers;
    }
}

string[] CreateLoggers()
{
    var testResultsFile = outputDirectory.Combine("test-results").Combine($"TEST-{target.ToLowerInvariant()}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.xml");
    // Evergreen CI server requires JUnit output format to display test results
    var junitLogger = $"junit;LogFilePath={testResultsFile};FailureBodyFormat=Verbose";
    var consoleLogger = "console;verbosity=detailed";
    return new []{ junitLogger, consoleLogger };
}

void RunTests(BuildConfig buildConfig, Path path, string filter = null)
{
    RunTests(buildConfig, path, settings => settings.Filter = filter);
}

void RunTests(BuildConfig buildConfig, Path path, Action<DotNetTestSettings> settingsAction)
{
    var settings = new DotNetTestSettings
    {
        NoBuild = true,
        NoRestore = true,
        Configuration = configuration,
        Loggers = buildConfig.Loggers,
        ArgumentCustomization = args => args.Append($"-- RunConfiguration.TargetPlatform={buildConfig.TargetPlatform}"),
        Framework = buildConfig.Framework
    };
    
    settingsAction?.Invoke(settings);

    DotNetTest(path.FullPath, settings);
}