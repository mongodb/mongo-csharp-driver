#addin "nuget:?package=Cake.FileHelpers"
#addin "nuget:?package=Cake.Git"
#addin "nuget:?package=Cake.Incubator"
#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=xunit.runner.console"
#load buildhelpers.cake

using System.Text.RegularExpressions;
using System.Linq;

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var gitVersion = GitVersion();

var solutionDirectory = MakeAbsolute(Directory("./"));
var artifactsDirectory = solutionDirectory.Combine("artifacts");
var artifactsBinDirectory = artifactsDirectory.Combine("bin");
var artifactsBinNet45Directory = artifactsBinDirectory.Combine("net45");
var artifactsBinNetStandard15Directory = artifactsBinDirectory.Combine("netstandard1.5");
var artifactsDocsDirectory = artifactsDirectory.Combine("docs");
var artifactsDocsApiDocsDirectory = artifactsDocsDirectory.Combine("ApiDocs-" + gitVersion.SemVer);
var artifactsDocsRefDocsDirectory = artifactsDocsDirectory.Combine("RefDocs-" + gitVersion.SemVer);
var artifactsPackagesDirectory = artifactsDirectory.Combine("packages");
var docsDirectory = solutionDirectory.Combine("Docs");
var docsApiDirectory = docsDirectory.Combine("Api");
var srcDirectory = solutionDirectory.Combine("src");
var testsDirectory = solutionDirectory.Combine("tests");
var toolsDirectory = solutionDirectory.Combine("Tools");
var toolsHugoDirectory = toolsDirectory.Combine("Hugo");

var solutionFile = solutionDirectory.CombineWithFilePath("CSharpDriver.sln");
var srcProjectNames = new[]
{
    "MongoDB.Bson",
    "MongoDB.Driver.Core",
    "MongoDB.Driver",
    "MongoDB.Driver.Legacy",
    "MongoDB.Driver.GridFS"
};

Task("Default")
    .IsDependentOn("TestAndPackage");

Task("TestAndPackage")
    .IsDependentOn("Test")
    .IsDependentOn("Package");

Task("Build")
    .IsDependentOn("BuildNet45")
    .IsDependentOn("BuildNetStandard15");

Task("BuildNet45")
    .Does(() =>
    {
        NuGetRestore(solutionFile);
        GlobalAssemblyInfo.OverwriteGlobalAssemblyInfoFile(Context, solutionDirectory, configuration, gitVersion);
        DotNetBuild(solutionFile, settings => settings
            .SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Minimal)
            .WithProperty("TargetFrameworkVersion", "v4.5"));

        EnsureDirectoryExists(artifactsBinNet45Directory);
        foreach (var projectName in srcProjectNames)
        {
            var projectDirectory = srcDirectory.Combine(projectName);
            var outputDirectory = projectDirectory.Combine("bin").Combine(configuration);
            foreach (var extension in new [] { ".dll", ".pdb", ".xml" })
            {
                var outputFileName = projectName + extension;
                var outputFile = outputDirectory.CombineWithFilePath(outputFileName);
                var artifactFile = artifactsBinNet45Directory.CombineWithFilePath(outputFileName);
                CopyFile(outputFile, artifactFile);
            }
        }
    })
    .Finally(() =>
    {
        GlobalAssemblyInfo.RestoreGlobalAssemblyInfoFile(Context, solutionDirectory);
    });

Task("BuildNetStandard15")
    .Does(() =>
    {
        var dotNetProjectDirectories = srcProjectNames.Select(
            projectName=>srcDirectory.Combine(projectName+".Dotnet"));

        foreach (var directory in dotNetProjectDirectories) 
        {
            DotNetCoreRestore(directory.ToString());
        }
        GlobalAssemblyInfo.OverwriteGlobalAssemblyInfoFile(Context, solutionDirectory, configuration, gitVersion);
        var settings= new DotNetCoreBuildSettings { Configuration = configuration };
        foreach (var directory in dotNetProjectDirectories) 
        {
            DotNetCoreBuild(directory.ToString(), settings);
        }
        EnsureDirectoryExists(artifactsBinNetStandard15Directory);
        foreach (var projectName in srcProjectNames)
        {
            var projectDirectory = srcDirectory.Combine(projectName + ".Dotnet");
            var outputDirectory = projectDirectory.Combine("bin").Combine(configuration).Combine("netstandard1.5");
            foreach (var extension in new [] { ".dll", ".pdb", ".xml" })
            {
                var outputFileName = projectName + extension;
                var outputFile = outputDirectory.CombineWithFilePath(outputFileName);
                var artifactFile = artifactsBinNetStandard15Directory.CombineWithFilePath(outputFileName);
                CopyFile(outputFile, artifactFile);
            }
        }
    })
    .Finally(() =>
    {
        GlobalAssemblyInfo.RestoreGlobalAssemblyInfoFile(Context, solutionDirectory);
    });

Task("Test")
    .IsDependentOn("TestWindows");

Task("TestWindows")
    .IsDependentOn("TestNet45")
    .IsDependentOn("TestNetStandard15");

Task("TestLinux")
    .IsDependentOn("TestNetStandard15");

Task("TestNet45")
    .IsDependentOn("BuildNet45")
    .Does(() =>
    {
        var testAssemblies = GetFiles("./tests/**/bin/" + configuration + "/*Tests.dll");
        var testSettings = new XUnit2Settings
        {
            Parallelism = ParallelismOption.None,
            ToolTimeout = TimeSpan.FromMinutes(30)
        };
        XUnit2(testAssemblies, testSettings);
    });

Task("TestNetStandard15")
    .IsDependentOn("BuildNetStandard15")
    .Does(() =>
    {
        var testProjectNames = new []
        {
            "MongoDB.Bson.Tests.Dotnet",
            "MongoDB.Driver.Core.Tests.Dotnet",
            "MongoDB.Driver.Tests.Dotnet",
            "MongoDB.Driver.GridFS.Tests.Dotnet",
            "MongoDB.Driver.Legacy.Tests.Dotnet"
        };
        foreach (var testProjectName in testProjectNames)
        {
            var testProjectDirectory = testsDirectory.Combine(testProjectName);
            DotNetCoreRestore(testProjectDirectory.ToString());
            var testProjectFile = testProjectDirectory.CombineWithFilePath($"{testProjectName}.csproj");
            var testSettings = new DotNetCoreTestSettings();
            var xunitSettings = new XUnit2Settings
            {
                Parallelism = ParallelismOption.None,
                ToolTimeout = TimeSpan.FromMinutes(30)
            };
            DotNetCoreTest(testSettings, testProjectFile, xunitSettings);
        }
    });

Task("Docs")
    .IsDependentOn("ApiDocs")
    .IsDependentOn("RefDocs");

Task("ApiDocs")
    .IsDependentOn("BuildNet45")
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
            .WithProperty("HelpFileVersion", gitVersion.MajorMinorPatch)
        );

        var lowerCaseIndexFile = artifactsDocsApiDocsDirectory.CombineWithFilePath("index.html");
        var upperCaseIndexFile = artifactsDocsApiDocsDirectory.CombineWithFilePath("Index.html");
        MoveFile(lowerCaseIndexFile, upperCaseIndexFile);

        var apiDocsZipFileName = artifactsDocsApiDocsDirectory.GetDirectoryName() + "-html.zip";
        var apiDocsZipFile = artifactsDocsDirectory.CombineWithFilePath(apiDocsZipFileName);
        Console.WriteLine(apiDocsZipFile.FullPath);
        Zip(artifactsDocsApiDocsDirectory, apiDocsZipFile);

        var chmFile = artifactsDocsApiDocsDirectory.CombineWithFilePath("CSharpDriverDocs.chm");
        var artifactsDocsChmFile = artifactsDocsDirectory.CombineWithFilePath("CSharpDriverDocs.chm");
        CopyFile(chmFile, artifactsDocsChmFile);

        DeleteDirectory(artifactsDocsApiDocsDirectory, recursive: true);
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
        var processSettings = new ProcessSettings
        {
            WorkingDirectory = landingDirectory
        };
        StartProcess(hugoExe, processSettings);

        var referenceDirectory = docsDirectory.Combine("reference");
        processSettings = new ProcessSettings
        {
            WorkingDirectory = referenceDirectory
        };
        StartProcess(hugoExe, processSettings);

        EnsureDirectoryExists(artifactsDocsRefDocsDirectory);
        CleanDirectory(artifactsDocsRefDocsDirectory);

        var landingPublicDirectory = landingDirectory.Combine("public");
        CopyDirectory(landingPublicDirectory, artifactsDocsRefDocsDirectory);

        var referencePublicDirectory = referenceDirectory.Combine("public");
        var artifactsReferencePublicDirectory = artifactsDocsRefDocsDirectory.Combine(gitVersion.Major + "." + gitVersion.Minor);
        CopyDirectory(referencePublicDirectory, artifactsReferencePublicDirectory);

        var refDocsZipFileName = artifactsDocsRefDocsDirectory.GetDirectoryName() + "-html.zip";
        var refDocsZipFile = artifactsDocsDirectory.CombineWithFilePath(refDocsZipFileName);
        Zip(artifactsDocsRefDocsDirectory, refDocsZipFile);

        DeleteDirectory(artifactsDocsRefDocsDirectory, recursive: true);
    });

Task("Package")
    .IsDependentOn("PackageReleaseZipFile")
    .IsDependentOn("PackageNugetPackages");

Task("PackageReleaseZipFile")
    .IsDependentOn("BuildNet45")
    .IsDependentOn("BuildNetStandard15")
    .IsDependentOn("ApiDocs")
    .Does(() =>
    {
        var assemblySemVer = gitVersion.AssemblySemVer; // e.g. 2.4.4.0
        var majorMinorBuild = Regex.Replace(assemblySemVer, @"\.\d+$", ""); // e.g. 2.4.4

        var stagingDirectoryName = "CSharpDriver-" + majorMinorBuild;
        var stagingDirectory = artifactsDirectory.Combine(stagingDirectoryName);
        EnsureDirectoryExists(stagingDirectory);
        CleanDirectory(stagingDirectory);

        var stagingNet45Directory = stagingDirectory.Combine("net45");
        CopyDirectory(artifactsBinNet45Directory, stagingNet45Directory);

        var stagingNetStandard15Directory = stagingDirectory.Combine("netstandard1.5");
        CopyDirectory(artifactsBinNetStandard15Directory, stagingNetStandard15Directory);

        var chmFile = artifactsDocsDirectory.CombineWithFilePath("CSharpDriverDocs.chm");
        var stagingChmFileName = stagingDirectoryName + ".chm";
        var stagingChmFile = stagingDirectory.CombineWithFilePath(stagingChmFileName);
        CopyFile(chmFile, stagingChmFile);

        var licenseFile = solutionDirectory.CombineWithFilePath("license.txt");
        var stagingLicenseFile = stagingDirectory.CombineWithFilePath("license.txt");
        CopyFile(licenseFile, stagingLicenseFile);

        var releaseNotesFileName = "Release Notes v" + majorMinorBuild + ".md";
        var releaseNotesDirectory = solutionDirectory.Combine("Release Notes");
        var releaseNotesFile =  releaseNotesDirectory.CombineWithFilePath(releaseNotesFileName);
        var stagingDirectoryReleaseNotesFile = stagingDirectory.CombineWithFilePath(releaseNotesFileName);
        CopyFile(releaseNotesFile, stagingDirectoryReleaseNotesFile);

        var zipFileName = stagingDirectoryName + ".zip";
        var zipFile = artifactsDirectory.CombineWithFilePath(zipFileName);
        Zip(stagingDirectory, zipFile);

        DeleteDirectory(stagingDirectory, recursive: true);
    });

Task("PackageNugetPackages")
    .IsDependentOn("BuildNet45")
    .IsDependentOn("BuildNetStandard15")
    .Does(() =>
    {
        EnsureDirectoryExists(artifactsPackagesDirectory);
        CleanDirectory(artifactsPackagesDirectory);

        var packageVersion = gitVersion.MajorMinorPatch;

        var nuspecFiles = GetFiles("./Build/*.nuspec");
        foreach (var nuspecFile in nuspecFiles)
        {
            var tempNuspecFilename = nuspecFile.GetFilenameWithoutExtension().ToString() + "." + packageVersion + ".nuspec";
            var tempNuspecFile = artifactsPackagesDirectory.CombineWithFilePath(tempNuspecFilename);

            CopyFile(nuspecFile, tempNuspecFile);
            ReplaceTextInFiles(tempNuspecFile.ToString(), "@driverPackageVersion@", packageVersion);
            ReplaceTextInFiles(tempNuspecFile.ToString(), "@solutionDirectory@", solutionDirectory.FullPath);

            NuGetPack(tempNuspecFile, new NuGetPackSettings
            {
                OutputDirectory = artifactsPackagesDirectory,
                Symbols = true
            });

            // DeleteFile(tempNuspecFile);
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

        var nuspecFiles = GetFiles("./artifacts/packages/*.nuspec");
        foreach (var nuspecFile in nuspecFiles)
        {
            var packageFileName = nuspecFile.GetFilenameWithoutExtension() + ".nupkg";
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
