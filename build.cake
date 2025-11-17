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

var solutionFile = solutionDirectory.CombineWithFilePath("CSharpDriver.sln");
var solutionFullPath = solutionFile.FullPath;

Task("Default")
    .IsDependentOn("Test");

Task("Test")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj").Where(name => !name.ToString().Contains("Atlas")),
        action: (BuildConfig buildConfig, Path testProject) =>
    {
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

        RunTests(buildConfig, testProject, filter: "Category=\"Integration\"");
    })
    .DeferOnError();

Task("TestAwsAuthentication")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"AwsMechanism\""));

Task("TestPlainAuthentication")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"PlainMechanism\""));

Task("TestAtlasConnectivity")
    .DoesForEach(
        items: GetFiles("./**/AtlasConnectivity.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) => RunTests(buildConfig, testProject));

Task("TestAtlasSearch")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
           RunTests(buildConfig, testProject, filter: "Category=\"AtlasSearch\""));

Task("TestAtlasSearchIndexHelpers")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
           RunTests(buildConfig, testProject, filter: "Category=\"AtlasSearchIndexHelpers\""));

Task("TestOcsp")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"OCSP\""));

Task("TestGssapi")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
           RunTests(buildConfig, testProject, filter: "Category=\"GssapiMechanism\""));

Task("TestMongoDbOidc")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"MongoDbOidc\""));

Task("TestLibMongoCrypt")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Encryption.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) => RunTests(buildConfig, testProject));

Task("TestLoadBalanced")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"SupportLoadBalancing\""));

Task("TestCsfleWithMockedKms")
    .IsDependentOn("TestLibMongoCrypt")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"CSFLE\""));

Task("TestCsfleWithMongocryptd")
    .IsDependentOn("TestLibMongoCrypt")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"CSFLE\""));

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

Task("TestX509")
    .DoesForEach(
        items: GetFiles("./**/MongoDB.Driver.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"X509\""));

Task("TestSocks5Proxy")
    .DoesForEach(
        items: GetFiles("./**/*.Tests.csproj"),
        action: (BuildConfig buildConfig, Path testProject) =>
            RunTests(buildConfig, testProject, filter: "Category=\"Socks5Proxy\""));

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

Setup<BuildConfig>(
    setupContext =>
    {
        var targetPlatform = RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X64 => "x64",
            var unknownArchitecture => throw new Exception($"Unknown CPU architecture: {unknownArchitecture}.")
        };

        var framework = Environment.GetEnvironmentVariable("FRAMEWORK");
        if (string.Equals(framework, "netstandard2.1", StringComparison.InvariantCultureIgnoreCase))
        {
            framework = "netcoreapp3.1";
        }

        var packageVersion = target.ToLowerInvariant().StartsWith("smoketests") ? Environment.GetEnvironmentVariable("PACKAGE_VERSION") : gitVersion.LegacySemVer;
        Console.WriteLine($"Framework: {framework ?? "null (not set)"}, TargetPlatform: {targetPlatform}, PackageVersion: {packageVersion}");

        return new BuildConfig(framework, targetPlatform, packageVersion);
    });

RunTarget(target);

public class BuildConfig
{
    public string Framework { get; }
    public string PackageVersion { get; }
    public string TargetPlatform { get; }

    public BuildConfig(string framework, string targetPlatform, string packageVersion)
    {
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
