#addin nuget:?package=Cake.FileHelpers&version=5.0.0
#addin nuget:?package=Cake.Git&version=2.0.0
#addin nuget:?package=Cake.Incubator&version=7.0.0
#tool dotnet:?package=GitVersion.Tool&version=5.10.3

using System;
using System.Linq;

var target = Argument("target", "CreatePackage");
var configuration = Argument("configuration", "Release");

var gitVersion = GitVersion();

var buildDirectory = MakeAbsolute(Directory(GetSettingValue("buildDirectory", "c:\\build")));
var libmongocryptAllDirectory=buildDirectory.Combine("libmongocrypt-all");
var downloadedMongocryptDirectory=buildDirectory.Combine("downloadedMongocryptDirectory");
var localReleaseVersion = "0.0.0-local";
var releaseVersion = GetSettingValue("releaseVersion", localReleaseVersion);
var fork = GetSettingValue("fork", "git@github.com:mongodb/libmongocrypt.git");
var branch = GetSettingValue("branch", "master");

// 1.10.0 - latest libmongocrypt release
var libmongocryptAllUrl = GetSettingValue("url", "https://mciuploads.s3.amazonaws.com/libmongocrypt/all/1.10.0/libmongocrypt-all.tar.gz");

var csharpBindingsGitTagName = $"csharp-v{releaseVersion}";
var csharpBindingsDirectory = buildDirectory.Combine(csharpBindingsGitTagName);
var libmongocryptRelWithDebInfoDirectory = csharpBindingsDirectory.Combine("cmake-build").Combine($"{configuration}");
var libmongocryptCsDirectory = csharpBindingsDirectory.Combine("bindings").Combine("cs");
var libmongocryptSolutionDirectory = libmongocryptCsDirectory.Combine("MongoDB.Libmongocrypt");
var libmongocryptSolutionFile = libmongocryptSolutionDirectory.CombineWithFilePath("MongoDB.Libmongocrypt.csproj");
var libmongocryptTestsSolutionDirectory = libmongocryptCsDirectory.Combine("MongoDB.Libmongocrypt.Test");
var artifactsDirectory = buildDirectory.Combine("artifacts");

Task("Prepare")
    .Does(() =>
    {
        if (DirectoryExists(buildDirectory))
        {
            DeleteDirectory(
                buildDirectory, 
                new DeleteDirectorySettings {
                    Recursive = true,
                    Force = true
                });
        }
        CreateDirectory(buildDirectory);

        Information($"Release version: {releaseVersion}");
        Information($"Fork: {fork}");
        Information($"Branch: {branch}");
        Information($"Native libraries url: {libmongocryptAllUrl}");

        Information("Downloading native libs..");
        EnsureDirectoryExists(libmongocryptAllDirectory);
        var nativeLibrariesArchive = libmongocryptAllDirectory.CombineWithFilePath("libmongocrypt-all.tar");
        DownloadFile(libmongocryptAllUrl, nativeLibrariesArchive);
        
        Information("Unzipping..");
        UncompressToTheCurrentDirectory(nativeLibrariesArchive);

        Information("Cloning the libmongocrypt repo..");
        EnsureDirectoryExists(csharpBindingsDirectory);
        Git(csharpBindingsDirectory, $"clone {fork} -b {branch} .");
        Information("Done git clone..");

        EnsureDirectoryExists(libmongocryptRelWithDebInfoDirectory);
        EnsureDirectoryExists(downloadedMongocryptDirectory);
        CopyFile(
            libmongocryptAllDirectory.Combine("windows-test").Combine("bin").CombineWithFilePath("mongocrypt.dll"),
            downloadedMongocryptDirectory.CombineWithFilePath("mongocrypt.dll"));
        CopyFile(
            libmongocryptAllDirectory.Combine("ubuntu1804-64").Combine("nocrypto").Combine("lib").CombineWithFilePath("libmongocrypt.so"),
            downloadedMongocryptDirectory.CombineWithFilePath("libmongocrypt.so"));
        CopyFile(
            libmongocryptAllDirectory.Combine("macos").Combine("lib").CombineWithFilePath("libmongocrypt.dylib"),
            downloadedMongocryptDirectory.CombineWithFilePath("libmongocrypt.dylib"));
        CopyDirectory(downloadedMongocryptDirectory, libmongocryptRelWithDebInfoDirectory);
    });

Task("Tests")
    .IsDependentOn("Prepare")
    .DoesForEach(
    () => 
    {
        var monikersDetails = new List<(string Moniker, string Bitness)>
        {
            { ("net472", "x64") },
            { ("netcoreapp2.1", "x64") },
            { ("netcoreapp3.0", "x64") },
            { ("net50", "x64") }
        };
        return monikersDetails;
    },
    (monikerInfo) =>
    {
        Information($"Test running {monikerInfo.Moniker}..");
        var settings = new DotNetTestSettings
        {
            Configuration = configuration,
            Framework = monikerInfo.Moniker,
            ArgumentCustomization = args => args.Append($"-- RunConfiguration.TargetPlatform={monikerInfo.Bitness}")
        };
        var projectFullPath = libmongocryptTestsSolutionDirectory.CombineWithFilePath("MongoDB.Libmongocrypt.Test.csproj").FullPath;
        Information(projectFullPath);
        DotNetTest(
            projectFullPath,
            settings
        );
    })
    .DeferOnError();
    
Task("CreatePackage")
    .IsDependentOn("Tests")
    .Does(() =>
    {
        var projectFullPath = libmongocryptSolutionFile.FullPath;
        Information($"Project path: {projectFullPath}. ReleaseVersion: {releaseVersion}");
        var settings = new DotNetPackSettings
        {
            Configuration = configuration,
            OutputDirectory = artifactsDirectory,
            EnvironmentVariables = new Dictionary<string, string>
            {
               { "Version", releaseVersion },
            }
        };
        DotNetPack(
            projectFullPath,
            settings);
    });
    
Task("NugetPush")
    .Does(() =>
    {
        ThrowIfLocalRelease();

        var nugetApi = GetSettingValue("NugetApiKey", null);
        var packageFilePath = artifactsDirectory.CombineWithFilePath($"{libmongocryptSolutionFile.GetFilenameWithoutExtension().ToString()}.{releaseVersion}.nupkg");
        Information(packageFilePath);
        DotNetNuGetPush(
            packageFilePath,
            new DotNetNuGetPushSettings 
            {
                ApiKey = nugetApi,
                Source = "https://api.nuget.org/v3/index.json"
            });
    });
    
Task("CreateGitTag")
    .Does(() =>
    {
        ThrowIfLocalRelease();

        Information($"Directory: {libmongocryptSolutionDirectory}");
        Information("Show origin:");
        Git(libmongocryptSolutionDirectory, "remote -v");
        Git(libmongocryptSolutionDirectory, $"tag -a {csharpBindingsGitTagName} -m {csharpBindingsGitTagName}"); 
        Git(libmongocryptSolutionDirectory, $"push origin {csharpBindingsGitTagName}"); 
    });

RunTarget(target);

string GetSettingValue(string commandArgumentName, string defaultValue)
{
    var optionValue = Argument(commandArgumentName, (string)null);
    if (optionValue != null)
    {
        return optionValue;
    }

    var environmentVariableName = $"LIBMONGOCRYPT_PACKAGING_{commandArgumentName.ToUpper()}";
    var environmentVariable = Environment.GetEnvironmentVariable(environmentVariableName);
    if (environmentVariable == null)
    {
        if (defaultValue == null)
        {
            throw new Exception($"Neither {commandArgumentName} command argument nor {environmentVariableName} environmentVariable have been configured.");
        }
        else
        {
            return defaultValue;
        }
    }
    
    return environmentVariable;
}

void Git(DirectoryPath workingDirectory, string command)
{
    CustomToolCall(workingDirectory, "git", command);
}

void UncompressToTheCurrentDirectory(FilePath archiveFilePath)
{
    CustomToolCall(archiveFilePath.GetDirectory(), "tar", "xzvf", archiveFilePath.GetFilename().ToString());
}

void CustomToolCall(DirectoryPath workingDirectory, string tool, params string[] arguments)
{
    var argumentsBuilder = new ProcessArgumentBuilder();
    foreach (var argument in arguments)
    {
        argumentsBuilder.Append(argument);
    }
    Information($"{tool} {string.Join(" ", arguments)}");
    StartProcess(tool, new ProcessSettings { Arguments = argumentsBuilder, WorkingDirectory = workingDirectory });
}

void ThrowIfLocalRelease()
{
    if (releaseVersion == localReleaseVersion)
    {
        throw new Exception("Attempt to publish a local nuget.");
    }
}
