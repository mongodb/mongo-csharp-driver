#r @"../Tools/FAKE/tools/FakeLib.dll"
#r @"../Tools/FAKE.Dotnet/tools/Fake.Dotnet.dll"

open System
open System.IO
open Fake
open Fake.AssemblyInfoFile
open Fake.Dotnet
open Fake.Testing.XUnit2

let config = getBuildParamOrDefault "config" "Release"
let baseVersion = getBuildParamOrDefault "baseVersion" "2.4.2"
let preRelease = getBuildParamOrDefault "preRelease" "local"
let getComputedBuildNumber() = 
    let result = Git.CommandHelper.runSimpleGitCommand currentDirectory "describe HEAD^1 --tags --long --match \"v[0-9].[0-9].[0-9]\""
    let m = System.Text.RegularExpressions.Regex.Match(result, @"-(\d+)-")
    m.Groups.[1].Value

let buildNumber = 
  match getBuildParam "buildNumber" with
  | "" -> getComputedBuildNumber()
  | v -> v

let version = baseVersion + "." + buildNumber
let versionSuffix = 
    match preRelease with
    | "build" | "local" -> preRelease + "-" + buildNumber.PadLeft(4, '0')
    | "#release#" -> "\"\""
    | _ -> preRelease
let semVersion = 
    match preRelease with
    | "#release#" -> baseVersion
    | _ -> baseVersion + "-" + versionSuffix

let shortVersion = semVersion.Substring(0, 3) // this works assuming we don't have double digits

let baseDir = currentDirectory
let buildDir = baseDir @@ "build"
let landingDocsDir = baseDir @@ "docs" @@ "landing"
let refDocsDir = baseDir @@ "docs" @@ "reference"
let srcDir = baseDir @@ "src"
let testsDir = baseDir @@ "tests"
let toolsDir = baseDir @@ "tools"

let artifactsDir = baseDir @@ "artifacts"
let binDir = artifactsDir @@ "bin"
let binDirNet45 = binDir @@ "net45"
let binDirNetStandard15 = binDir @@ "netstandard1.5"
let testResultsDir = artifactsDir @@ "test_results"
let tempDir = artifactsDir @@ "tmp"

let slnFile = baseDir @@ "CSharpDriver.sln"

let asmFile = srcDir @@ "MongoDB.Shared" @@ "GlobalAssemblyInfo.cs"
let apiDocsFile = baseDir @@ "Docs" @@ "Api" @@ "CSharpDriverDocs.shfbproj"
let versionFile = artifactsDir @@ "version.txt"

let dotNetSrcProjects = [
    srcDir @@ "MongoDB.Bson.Dotnet" @@ "project.json"
    srcDir @@ "MongoDB.Driver.Core.Dotnet" @@ "project.json"
    srcDir @@ "MongoDB.Driver.Dotnet" @@ "project.json"
    srcDir @@ "MongoDB.Driver.Legacy.Dotnet" @@ "project.json"
    srcDir @@ "MongoDB.Driver.GridFS.Dotnet" @@ "project.json"
]

let dotNetTestHelpersProjects = [
    testsDir @@ "MongoDB.Bson.TestHelpers.Dotnet" @@ "project.json"
    testsDir @@ "MongoDB.Driver.Core.TestHelpers.Dotnet" @@ "project.json"
    testsDir @@ "MongoDB.Driver.TestHelpers.Dotnet" @@ "project.json"
    testsDir @@ "MongoDB.Driver.Legacy.TestHelpers.Dotnet" @@ "project.json"
]

let dotNetTestProjects = [
    testsDir @@ "MongoDB.Bson.Tests.Dotnet" @@ "project.json"
    testsDir @@ "MongoDB.Driver.Core.Tests.Dotnet" @@ "project.json"
    testsDir @@ "MongoDB.Driver.Tests.Dotnet" @@ "project.json"
    testsDir @@ "MongoDB.Driver.Legacy.Tests.Dotnet" @@ "project.json"
    testsDir @@ "MongoDB.Driver.GridFS.Tests.Dotnet" @@ "project.json"
]

let dotNetProjects = List.concat [ dotNetSrcProjects; dotNetTestHelpersProjects; dotNetTestProjects ] 

type NuspecFile = { File : string; Symbols : bool; }
let nuspecFiles =
    [ { File = buildDir @@ "MongoDB.Bson.nuspec"; Symbols = true; }
      { File = buildDir @@ "MongoDB.Driver.Core.nuspec"; Symbols = true; }
      { File = buildDir @@ "MongoDB.Driver.nuspec"; Symbols = true; }
      { File = buildDir @@ "MongoDB.Driver.GridFS.nuspec"; Symbols = true; }
      { File = buildDir @@ "mongocsharpdriver.nuspec"; Symbols = true; }]

let nuspecBuildFile = buildDir @@ "MongoDB.Driver-Build.nuspec"
let licenseFile = baseDir @@ "License.txt"
let releaseNotesFile = baseDir @@ "Release Notes" @@ "Release Notes v" + baseVersion + ".md"

let versionArtifactFile = artifactsDir @@ "version.txt"
let apiDocsArtifactFile = artifactsDir @@ "CSharpDriver-" + semVersion + ".chm"
let apiDocsArtifactZipFile = artifactsDir @@ "ApiDocs-" + semVersion + "-html.zip"
let refDocsArtifactZipFile = artifactsDir @@ "RefDocs-" + semVersion + "-html.zip"
let zipArtifactFile = artifactsDir @@ "CSharpDriver-" + semVersion + ".zip"

MSBuildDefaults <- { MSBuildDefaults with Verbosity = Some(MSBuildVerbosity.Minimal) }

// Targets
Target "Clean" (fun _ ->
    CleanDir artifactsDir
    DeleteDir artifactsDir
)

Target "OutputVersion" (fun _ ->
    ensureDirectory artifactsDir

    let lines = 
        [ sprintf "baseVersion=%s" baseVersion 
          sprintf "preRelease=%s" preRelease 
          sprintf "buildNumber=%s" buildNumber 
          sprintf "semVersion=%s" semVersion ]

    WriteFile versionFile lines
)

Target "AssemblyInfo" (fun _ ->
    let githash = Git.Information.getCurrentSHA1 baseDir
    
    ActivateFinalTarget "Teardown"
    ReplaceAssemblyInfoVersions (fun p ->
        { p with
            OutputFileName = asmFile
            AssemblyVersion = version
            AssemblyInformationalVersion = semVersion
            AssemblyFileVersion = version
            AssemblyConfiguration = config
            AssemblyMetadata = ["githash", githash]})
)

Target "BuildNet45" (fun _ ->
    !! "./**/packages.config"
    |> Seq.iter (RestorePackage (fun x -> { x with OutputPath = baseDir @@ "packages" }))


    let properties = [
        ("Configuration", config);
        ("TargetFrameworkVersion", "v4.5")
    ]

    [slnFile]
        |> MSBuild binDirNet45 "Build" properties
        |> Log "Build: "
)

Target "InstallDotnet" (fun _ ->
    DotnetCliInstall Preview2ToolingOptions
)

Target "BuildNetStandard15" (fun _ ->
    for project in dotNetProjects do
        DotnetRestore id project
        DotnetCompile (fun c ->
            { c with
                Configuration = BuildConfiguration.Release
                Common = {DotnetOptions.Default with CustomParams = Some ("--version-suffix " + versionSuffix)}
            })
            project

    ensureDirectory binDirNetStandard15
    for projectName in [ "MongoDB.Bson"; "MongoDB.Driver.Core"; "MongoDB.Driver"; "MongoDB.Driver.Legacy"; "MongoDB.Driver.GridFS"] do
        let projectDirectory = baseDir @@ "src" @@ (projectName + ".Dotnet")
        let outputDirectory = projectDirectory @@ "bin" @@ "Release" @@ "netstandard1.5"
        for extension in [".dll"; ".pdb"; ".xml"] do
            CopyFile binDirNetStandard15 (outputDirectory @@ (projectName + extension))

)

Target "TestNet45" (fun _ ->
    if not <| directoryExists binDirNet45 then new Exception(sprintf "Directory %s does not exist." binDirNet45) |> raise
    ensureDirectory testResultsDir

    let testDlls = !! (binDirNet45 @@ "*Tests.dll")

    let resultsOutputPath = testResultsDir @@ (getBuildParamOrDefault "testResults" "test-results.xml")
    let includeTraits =
        match getBuildParamOrDefault "Category" "" with
        | "" -> []
        | category -> [("Category", category)]

    testDlls
        |> xUnit2 (fun p ->
            { p with
                ErrorLevel = TestRunnerErrorLevel.Error
                NUnitXmlOutputPath = Some resultsOutputPath
                Parallel = ParallelMode.NoParallelization
                TimeOut = TimeSpan.FromDays(1.0)
                IncludeTraits = includeTraits
            })
)

Target "TestNetStandard15" (fun _ ->
    for project in dotNetTestProjects do
        DotnetRestore id project
        let traitArg =
            match getBuildParamOrDefault "Category" "" with
            | "" -> ""
            | category -> sprintf "-trait \"Category=%s\"" category
        let args = sprintf "test %s %s" project traitArg
        let result = Dotnet DotnetOptions.Default args
        if not result.OK then failwithf "dotnet test failed with code %i" result.ExitCode
)

Target "RefDocs" (fun _ ->
  DeleteFile refDocsArtifactZipFile
  ensureDirectory tempDir
  CleanDir tempDir

  let landingResult =
    ExecProcess (fun info ->
      info.FileName <- "hugo.exe"
      info.WorkingDirectory <- landingDocsDir
    )(TimeSpan.FromMinutes 1.0)

  let refResult = 
    ExecProcess (fun info ->
      info.FileName <- "hugo.exe"
      info.WorkingDirectory <- refDocsDir
    )(TimeSpan.FromMinutes 1.0)

  CopyDir tempDir (landingDocsDir @@ "public") (fun _ -> true)
  CopyDir (tempDir @@ shortVersion) (refDocsDir @@ "public") (fun _ -> true)

  !! (tempDir @@ "**/**.*")
    |> CreateZip tempDir refDocsArtifactZipFile "" DefaultZipLevel false

  DeleteDir tempDir
)

Target "ApiDocs" (fun _ ->
    DeleteFile apiDocsArtifactFile
    DeleteFile apiDocsArtifactZipFile
    ensureDirectory tempDir
    CleanDir tempDir

    let preliminary =
        match preRelease with
        | "#release#" -> "False"
        | _ -> "True"

    let properties = ["Configuration", config
                      "OutputPath", tempDir
                      "CleanIntermediate", "True"
                      "Preliminary", preliminary
                      "HelpFileVersion", version]

    [apiDocsFile]
        |> MSBuild binDirNet45 "" properties
        |> Log "Docs: "

    Rename apiDocsArtifactFile (tempDir @@ "CSharpDriverDocs.chm")
    Rename (tempDir @@ "index.html") (tempDir @@ "Index.html")

    !! (tempDir @@ "**/**.*")
        |> CreateZip tempDir apiDocsArtifactZipFile "" DefaultZipLevel false

    DeleteDir tempDir
)

Target "Zip" (fun _ ->
    DeleteFile zipArtifactFile

    checkFileExists apiDocsArtifactFile
    checkFileExists licenseFile
    checkFileExists releaseNotesFile

    let zipStagingDirectory = artifactsDir @@ "zip-staging"
    CleanDir zipStagingDirectory
    DeleteDir zipStagingDirectory

    let sharedFiles = [
        licenseFile
        releaseNotesFile 
        apiDocsArtifactFile
    ]

    let net45Files = [
        binDirNet45 @@ "MongoDB.Bson.dll"
        binDirNet45 @@ "MongoDB.Bson.pdb"
        binDirNet45 @@ "MongoDB.Bson.xml"
        binDirNet45 @@ "MongoDB.Driver.Core.dll"
        binDirNet45 @@ "MongoDB.Driver.Core.pdb"
        binDirNet45 @@ "MongoDB.Driver.Core.xml"
        binDirNet45 @@ "MongoDB.Driver.dll"
        binDirNet45 @@ "MongoDB.Driver.pdb"
        binDirNet45 @@ "MongoDB.Driver.xml"
        binDirNet45 @@ "MongoDB.Driver.GridFS.dll"
        binDirNet45 @@ "MongoDB.Driver.GridFS.pdb"
        binDirNet45 @@ "MongoDB.Driver.GridFS.xml"
        binDirNet45 @@ "MongoDB.Driver.Legacy.dll"
        binDirNet45 @@ "MongoDB.Driver.Legacy.pdb"
        binDirNet45 @@ "MongoDB.Driver.Legacy.xml"
    ]

    let netStandard16Files = [
        binDirNetStandard15 @@ "MongoDB.Bson.dll"
        binDirNetStandard15 @@ "MongoDB.Bson.pdb"
        binDirNetStandard15 @@ "MongoDB.Bson.xml"
        binDirNetStandard15 @@ "MongoDB.Driver.Core.dll"
        binDirNetStandard15 @@ "MongoDB.Driver.Core.pdb"
        binDirNetStandard15 @@ "MongoDB.Driver.Core.xml"
        binDirNetStandard15 @@ "MongoDB.Driver.dll"
        binDirNetStandard15 @@ "MongoDB.Driver.pdb"
        binDirNetStandard15 @@ "MongoDB.Driver.xml"
        binDirNetStandard15 @@ "MongoDB.Driver.GridFS.dll"
        binDirNetStandard15 @@ "MongoDB.Driver.GridFS.pdb"
        binDirNetStandard15 @@ "MongoDB.Driver.GridFS.xml"
        binDirNetStandard15 @@ "MongoDB.Driver.Legacy.dll"
        binDirNetStandard15 @@ "MongoDB.Driver.Legacy.pdb"
        binDirNetStandard15 @@ "MongoDB.Driver.Legacy.xml"
    ]

    CopyFiles zipStagingDirectory sharedFiles
    CopyFiles (zipStagingDirectory @@ "net45") net45Files
    CopyFiles (zipStagingDirectory @@ "netstandard1.5") netStandard16Files

    !! (zipStagingDirectory @@ "**/*.*")
        |> CreateZip zipStagingDirectory zipArtifactFile "" DefaultZipLevel false
)

let createNuGetPackage file symbols =
    NuGetPack (fun p ->
      { p with
          Version = semVersion
          OutputPath = artifactsDir
          WorkingDir = baseDir
          SymbolPackage = if symbols then NugetSymbolPackage.Nuspec else NugetSymbolPackage.None })
      file

Target "NuGetPack" (fun _ ->
    !!(artifactsDir @@ "*.nupkg") |> DeleteFiles

    match preRelease with
    | "build" -> createNuGetPackage nuspecBuildFile true
    | _ ->
        for nuspecFile in nuspecFiles do
            createNuGetPackage nuspecFile.File nuspecFile.Symbols
)

let pushNugetPackage project =
    NuGetPublish (fun x -> 
      { x with 
          PublishUrl = getBuildParamOrDefault "nugetSource" "https://www.myget.org/F/mongodb/api/v2/package"
          AccessKey = getBuildParam "nugetApiKey"
          OutputPath = artifactsDir
          WorkingDir = baseDir
          Project = project
          Version = semVersion })

Target "NuGetPush" (fun _ ->
    if not <| hasBuildParam "nugetApiKey" then new Exception("nugetApiKey must be specified to push nuget files.") |> raise

    match preRelease with
    | "build" -> pushNugetPackage (fileNameWithoutExt nuspecBuildFile)
    | _ ->
        for nuspecFile in nuspecFiles do
            pushNugetPackage (fileNameWithoutExt nuspecFile.File)
)

FinalTarget "Teardown" (fun _ ->
    let cmd = sprintf "checkout %s" asmFile
    let result = Git.CommandHelper.runSimpleGitCommand baseDir cmd
    ()
)


Target "Docs" DoNothing
Target "Package" DoNothing
Target "Publish" DoNothing
Target "Build" DoNothing
Target "Test" DoNothing

// Build dependencies
"AssemblyInfo" ==> "BuildNet45"
"AssemblyInfo" ==> "BuildNetStandard15"
"BuildNet45" ==> "Build"
"BuildNetStandard15" ==> "Build"
"Clean" ==> "AssemblyInfo"
"InstallDotnet" ==> "BuildNetStandard15"

// Test dependencies (assumes Build has already been run)
"InstallDotnet" ==> "TestNetStandard15"
"TestNet45" ==> "Test"
"TestNetStandard15" ==> "Test"

// Package dependencies (assumes Build has already been run)
"ApiDocs" ==> "Docs"
"ApiDocs" ==> "Zip"
"Docs" ==> "Package"
"NuGetPack" ==> "Package"
"RefDocs" ==> "Docs"
"Zip" ==> "Package"

// Publish dependencies (assumes Package has already been run)
"NuGetPush" ==> "Publish"

RunTargetOrDefault "Build"
