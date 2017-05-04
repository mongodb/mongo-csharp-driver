#addin "nuget:?package=Cake.Git"
#addin "nuget:?package=Cake.Incubator"
#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=xunit.runner.console"
#load buildhelpers.cake

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var solutionDirectory = Directory("./");
var artifactsDirectory = solutionDirectory + Directory("artifacts");
var toolsDirectory = solutionDirectory + Directory("Tools");

var solutionFile = solutionDirectory + File("CSharpDriver.sln");
var gitVersion = GitVersion();

Task("EchoGitVersion")
    .Does(() =>
    {
        Information("AssemblySemVer = {0}", gitVersion.AssemblySemVer);
        Information("CommitsSinceVersionSource = {0}", gitVersion.CommitsSinceVersionSource);
        Information("FullSemVer = {0}", gitVersion.FullSemVer);
        Information("InformationalVersion = {0}", gitVersion.InformationalVersion);
        Information("LegacySemVer = {0}", gitVersion.LegacySemVer);
        Information("NuGetVersion = {0}", gitVersion.NuGetVersion);
        Information("NuGetVersionV2 = {0}", gitVersion.NuGetVersionV2);
        Information("Patch = {0}", gitVersion.Patch);
        Information("PreReleaseLabel = {0}", gitVersion.PreReleaseLabel);
        Information("PreReleaseNumber = {0}", gitVersion.PreReleaseNumber);
        Information("PreReleaseTag = {0}", gitVersion.PreReleaseTag);
        Information("PreReleaseTagWithDash = {0}", gitVersion.PreReleaseTagWithDash);
        Information("SemVer = {0}", gitVersion.SemVer);
    });

Task("BuildNet45")
    .Does(() =>
    {
        NuGetRestore(solutionFile);
        GlobalAssemblyInfo.OverwriteGlobalAssemblyInfoFile(Context, solutionDirectory, configuration, gitVersion);
        DotNetBuild(solutionFile, settings => settings
            .SetConfiguration(configuration)
            .SetVerbosity(Verbosity.Minimal));
    })
    .Finally(() =>
    {
        GlobalAssemblyInfo.RestoreGlobalAssemblyInfoFile(Context, solutionDirectory);
    });

Task("BuildNetStandard15")
    .Does(() =>
    {
        DotNetCoreRestore();
        GlobalAssemblyInfo.OverwriteGlobalAssemblyInfoFile(Context, solutionDirectory, configuration, gitVersion);
        DotNetCoreBuild("./**/project.json", new DotNetCoreBuildSettings
        {
            Configuration = configuration
        });
    })
    .Finally(() =>
    {
        GlobalAssemblyInfo.RestoreGlobalAssemblyInfoFile(Context, solutionDirectory);
    });

Task("Build")
    .IsDependentOn("BuildNet45")
    .IsDependentOn("BuildNetStandard15");

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
        var testsDirectory = solutionDirectory + Directory("tests");
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
            var testProjectDirectory = testsDirectory + Directory(testProjectName);
            var testProjectFile = testProjectDirectory + File("project.json");
            var testSettings = new DotNetCoreTestSettings();
            var xunitSettings = new XUnit2Settings
            {
                Parallelism = ParallelismOption.None,
                ToolTimeout = TimeSpan.FromMinutes(30)
            };
            DotNetCoreTest(testSettings, testProjectFile, xunitSettings);
        }
    });

Task("TestWindows")
    .IsDependentOn("TestNet45")
    .IsDependentOn("TestNetStandard15");

Task("TestLinux")
    .IsDependentOn("TestNetStandard15");

Task("Test")
    .IsDependentOn("TestWindows");

Task("RefDocs")
    .Does(() =>
    {
        var hugoDirectory = toolsDirectory + Directory("Hugo");
        EnsureDirectoryExists(hugoDirectory);
        CleanDirectory(hugoDirectory);

        var url = "https://github.com/spf13/hugo/releases/download/v0.13/hugo_0.13_windows_amd64.zip";
        var zipFile = hugoDirectory + File("hugo_0.13_windows_amd64.zip");
        DownloadFile(url, zipFile);
        Unzip(zipFile, hugoDirectory);
        var hugoExe = hugoDirectory + File("hugo_0.13_windows_amd64.exe");

        var landingDirectory = solutionDirectory + Directory("docs") + Directory("landing");
        var processSettings = new ProcessSettings
        {
            WorkingDirectory = landingDirectory
        };
        StartProcess(hugoExe, processSettings);

        var referenceDirectory = solutionDirectory + Directory("docs") + Directory("reference");
        processSettings = new ProcessSettings
        {
            WorkingDirectory = referenceDirectory
        };
        StartProcess(hugoExe, processSettings);

        var tempDirectory = artifactsDirectory + Directory("tmp");
        EnsureDirectoryExists(tempDirectory);
        CleanDirectory(tempDirectory);

        var landingPublicDirectory = landingDirectory + Directory("public");
        CopyDirectory(landingPublicDirectory, tempDirectory);

        var referencePublicDirectory = referenceDirectory + Directory("public");
        var referencePublicDestinationDirectory = tempDirectory + Directory(gitVersion.Major + "." + gitVersion.Minor);
        CopyDirectory(referencePublicDirectory, referencePublicDestinationDirectory);

        var referenceDocsZipFile = artifactsDirectory + File("RefDocs-" + gitVersion.SemVer + "-html.zip");
        Zip(tempDirectory, referenceDocsZipFile);

        DeleteDirectory(tempDirectory, recursive: true);
    });

Task("Default")
    .IsDependentOn("Build");

RunTarget(target);
