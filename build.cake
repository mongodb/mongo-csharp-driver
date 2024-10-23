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
var artifactsPackagesDirectory = artifactsDirectory.Combine("packages");
var srcDirectory = solutionDirectory.Combine("src");
var testsDirectory = solutionDirectory.Combine("tests");
var outputDirectory = solutionDirectory.Combine("build");
var toolsDirectory = solutionDirectory.Combine("tools");
var toolsHugoDirectory = toolsDirectory.Combine("Hugo");
var mongoDbDriverPackageName = "MongoDB.Driver";

var solutionFile = solutionDirectory.CombineWithFilePath("CSharpDriver.sln");
var solutionFullPath = solutionFile.FullPath;
var srcProjectNames = new[]
{
    "MongoDB.Bson",
    "MongoDB.Driver"
};

Task("Default")
    .IsDependentOn("Test");

Task("Release")
    .IsDependentOn("Build")
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

            var projects = new[] { "MongoDB.Bson", "MongoDB.Driver" };
            foreach (var project in projects)
            {
                var fromDirectory = srcDirectory.Combine(project).Combine("bin").Combine(configuration).Combine(targetFramework);

                var fileNames = new List<string>();
                foreach (var extension in new[] { "dll", "pdb", "xml" })
                {
                    var fileName = $"{project}.{extension}";
                    fileNames.Add(fileName);
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

Task("TestAtlasSearchIndexHelpers")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
           RunTests(buildConfig, testProject, filter: "Category=\"AtlasSearchIndexHelpers\""));

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
Task("TestGssapiNetStandard21").IsDependentOn("TestGssapi");
Task("TestGssapiNet60").IsDependentOn("TestGssapi");

Task("TestMongoDbOidc")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"MongoDbOidc\""));

Task("TestServerless")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"Serverless\""));

Task("TestServerlessNet472").IsDependentOn("TestServerless");
Task("TestServerlessNetStandard21").IsDependentOn("TestServerless");
Task("TestServerlessNet60").IsDependentOn("TestServerless");

Task("TestLibMongoCrypt")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Libmongocrypt.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) => RunTests(buildConfig, testProject));

Task("TestLoadBalanced")
    .IsDependentOn("Build")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"SupportLoadBalancing\""));

Task("TestLoadBalancedNetStandard21").IsDependentOn("TestLoadBalanced");
Task("TestLoadBalancedNet60").IsDependentOn("TestLoadBalanced");

Task("TestCsfleWithMockedKms")
    .IsDependentOn("TestLibMongoCrypt")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"CSFLE\""));

Task("TestCsfleWithMockedKmsNet472").IsDependentOn("TestCsfleWithMockedKms");
Task("TestCsfleWithMockedKmsNetStandard21").IsDependentOn("TestCsfleWithMockedKms");
Task("TestCsfleWithMockedKmsNet60").IsDependentOn("TestCsfleWithMockedKms");

Task("TestCsfleWithMongocryptd")
    .IsDependentOn("TestLibMongoCrypt")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"CSFLE\""));

Task("TestCsfleWithMongocryptdNet472").IsDependentOn("TestCsfleWithMongocryptd");
Task("TestCsfleWithMongocryptdNetStandard21").IsDependentOn("TestCsfleWithMongocryptd");
Task("TestCsfleWithMongocryptdNet60").IsDependentOn("TestCsfleWithMongocryptd");

Task("TestCsfleWithAzureKms")
    .IsDependentOn("TestLibMongoCrypt")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"CsfleAZUREKMS\""));

Task("TestCsfleWithGcpKms")
    .IsDependentOn("TestLibMongoCrypt")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"CsfleGCPKMS\""));

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
            "MongoDB.Driver",
            "MongoDB.Driver.Encryption"
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
            "MongoDB.Driver"
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

Task("SmokeTests")
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
            $"--no-restore --version [{buildConfig.PackageVersion}]",
            toolSettings);

        DotNetTool(
            testProject.FullPath,
            "add package MongoDB.Driver.Encryption",
            $"--no-restore --version [{buildConfig.PackageVersion}]",
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
Task("SmokeTestsNetCoreApp31").IsDependentOn("SmokeTests");
Task("SmokeTestsNet50").IsDependentOn("SmokeTests");
Task("SmokeTestsNet60").IsDependentOn("SmokeTests");
Task("SmokeTestsNet80").IsDependentOn("SmokeTests");

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
            string s when s.EndsWith("netstandard21") || s.EndsWith("netcoreapp31") => "netcoreapp3.1",
            string s when s.EndsWith("net472") => "net472",
            string s when s.EndsWith("net50") => "net5.0",
            string s when s.EndsWith("net60") => "net6.0",
            string s when s.EndsWith("net80") => "net8.0",
            _ => null
        };

        var isReleaseMode = lowerTarget.StartsWith("package") || lowerTarget == "release";
        var packageVersion = lowerTarget.StartsWith("smoketests") ? Environment.GetEnvironmentVariable("PACKAGE_VERSION") : gitVersion.LegacySemVer;

        Console.WriteLine($"Framework: {framework ?? "null (not set)"}, TargetPlatform: {targetPlatform}, IsReleaseMode: {isReleaseMode}, PackageVersion: {packageVersion}");

        return new BuildConfig(isReleaseMode, framework, targetPlatform, packageVersion);
    });

RunTarget(target);

public class BuildConfig
{
    public bool IsReleaseMode { get; }
    public string Framework { get; }
    public string PackageVersion { get; }
    public string TargetPlatform { get; }

    public BuildConfig(bool isReleaseMode, string framework, string targetPlatform, string packageVersion)
    {
        IsReleaseMode = isReleaseMode;
        Framework = framework;
        TargetPlatform = targetPlatform;
        PackageVersion = packageVersion;
    }
}

string[] CreateLoggers(string projectName)
{
    var testResultsFile = outputDirectory.Combine("test-results").Combine($"TEST-{projectName}-{target.ToLowerInvariant()}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.xml");

    // Evergreen CI server requires JUnit output format to display test results
    var junitLogger = $"junit;LogFilePath={testResultsFile};FailureBodyFormat=Verbose";
    var consoleLogger = "console;verbosity=detailed";
    return new[] { junitLogger, consoleLogger };
}

void RunTests(BuildConfig buildConfig, Path path, string filter = null)
{
    RunTests(buildConfig, path, settings => settings.Filter = filter);
}

void RunTests(BuildConfig buildConfig, Path path, Action<DotNetTestSettings> settingsAction)
{
    var projectName = System.IO.Path.GetFileNameWithoutExtension(path.FullPath);

    var settings = new DotNetTestSettings
    {
        NoBuild = true,
        NoRestore = true,
        Configuration = configuration,
        Loggers = CreateLoggers(projectName),
        ArgumentCustomization = args => args.Append($"-- RunConfiguration.TargetPlatform={buildConfig.TargetPlatform}"),
        Framework = buildConfig.Framework
    };

    settingsAction?.Invoke(settings);

    DotNetTest(path.FullPath, settings);
}
