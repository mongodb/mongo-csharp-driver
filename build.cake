#addin "nuget:?package=Cake.FileHelpers&version=3.1.0"
#addin "nuget:?package=Cake.Git&version=0.19.0"
#addin "nuget:?package=Cake.Incubator&version=3.1.0"
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0"
#tool "nuget:?package=xunit.runner.console"

using System.Text.RegularExpressions;
using System.Linq;

var target = Argument("target", "Default");
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
var toolsDirectory = solutionDirectory.Combine("Tools");
var toolsHugoDirectory = toolsDirectory.Combine("Hugo");

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
        DotNetCoreRestore(solutionFullPath);
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
        foreach (var targetFramework in new[] { "net452", "netstandard1.5", "netstandard2.0", "netstandard2.1" })
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
                if (targetFramework == "net452" && project == "MongoDB.Driver.Core")
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

        var settings = new DotNetCoreTestSettings
        {
            NoBuild = true,
            NoRestore = true,
            Configuration = configuration,
            ArgumentCustomization = args => args.Append("-- RunConfiguration.TargetPlatform=x64")
        };
        switch (target.ToLowerInvariant())
        {
            case "testnet452": settings.Framework = "net452"; break;
            case "testnetstandard15": settings.Framework = "netcoreapp1.1"; break;
            case "testnetstandard20": settings.Framework = "netcoreapp2.1"; break;
            case "testnetstandard21": settings.Framework = "netcoreapp3.0"; break;
        }
        DotNetCoreTest(
            testProject.FullPath,
            settings
        );
    });

Task("TestNet452").IsDependentOn("Test");
Task("TestNetStandard15").IsDependentOn("Test");
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

Task("PushToMyget")
    .Does(() =>
    {
        var mygetApiKey = EnvironmentVariable("MYGETAPIKEY");
        if (mygetApiKey == null)
        {
            throw new Exception("MYGETAPIKEY environment variable missing");
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
            ApiKey = mygetApiKey,
            Source = "https://www.myget.org/F/mongodb/api/v2/package"
        });
    });

Task("DumpGitVersion")
    .Does(() =>
    {
        Information(gitVersion.Dump());
    });

RunTarget(target);
