#r @"../Tools/FAKE/tools/FakeLib.dll"
open System
open Fake
open Fake.AssemblyInfoFile

let config = getBuildParamOrDefault "config" "Release"
let baseVersion = getBuildParamOrDefault "baseVersion" "2.2.0"
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
let semVersion = 
    match preRelease with
    | "build" | "local" -> baseVersion + "-" + preRelease + "-" + buildNumber.PadLeft(4, '0')
    | "#release#" -> baseVersion
    | _ -> baseVersion + "-" + preRelease

let shortVersion = semVersion.Substring(0, 3) // this works assuming we don't have double digits

let baseDir = currentDirectory
let buildDir = baseDir @@ "build"
let landingDocsDir = baseDir @@ "docs" @@ "landing"
let refDocsDir = baseDir @@ "docs" @@ "reference"
let srcDir = baseDir @@ "src"
let toolsDir = baseDir @@ "tools"

let artifactsDir = baseDir @@ "artifacts"
let binDir = artifactsDir @@ "bin"
let binDir45 = binDir @@ "net45"
let testResultsDir = artifactsDir @@ "test_results"
let tempDir = artifactsDir @@ "tmp"

let slnFile = 
    match isMono with
    | true -> srcDir @@ "CSharpDriver-Mono.sln"
    | false -> srcDir @@ "CSharpDriver.sln"

let asmFile = srcDir @@ "MongoDB.Shared" @@ "GlobalAssemblyInfo.cs"
let apiDocsFile = baseDir @@ "Docs" @@ "Api" @@ "CSharpDriverDocs.shfbproj"
let installerFile = baseDir @@ "Installer" @@ "CSharpDriverInstaller.wixproj"
let versionFile = artifactsDir @@ "version.txt"

type NuspecFile = { File : string; Dependencies : string list; Symbols : bool; }
let nuspecFiles =
    [ { File = buildDir @@ "MongoDB.Bson.nuspec"; Dependencies = []; Symbols = true; }
      { File = buildDir @@ "MongoDB.Driver.Core.nuspec"; Dependencies = ["MongoDB.Bson"]; Symbols = true; }
      { File = buildDir @@ "MongoDB.Driver.nuspec"; Dependencies = ["MongoDB.Bson"; "MongoDB.Driver.Core"]; Symbols = true; }
      { File = buildDir @@ "MongoDB.Driver.GridFS.nuspec"; Dependencies = ["MongoDB.Bson"; "MongoDB.Driver.Core"; "MongoDB.Driver"]; Symbols = true; }
      { File = buildDir @@ "mongocsharpdriver.nuspec"; Dependencies = ["MongoDB.Bson"; "MongoDB.Driver.Core"; "MongoDB.Driver"]; Symbols = true; }]

let nuspecBuildFile = buildDir @@ "MongoDB.Driver-Build.nuspec"
let licenseFile = baseDir @@ "License.txt"
let releaseNotesFile = baseDir @@ "Release Notes" @@ "Release Notes v" + baseVersion + ".md"

let versionArtifactFile = artifactsDir @@ "version.txt"
let apiDocsArtifactFile = artifactsDir @@ "CSharpDriver-" + semVersion + ".chm"
let apiDocsArtifactZipFile = artifactsDir @@ "ApiDocs-" + semVersion + "-html.zip"
let refDocsArtifactZipFile = artifactsDir @@ "RefDocs-" + semVersion + "-html.zip"
let zipArtifactFile = artifactsDir @@ "CSharpDriver-" + semVersion + ".zip"

MSBuildDefaults <- { MSBuildDefaults with Verbosity = Some(Minimal) }

monoArguments <- "--runtime=v4.0.30319"

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

Target "Build" (fun _ ->
    !! "./**/packages.config"
    |> Seq.iter (RestorePackage (fun x -> { x with OutputPath = srcDir @@ "packages" }))


    let mutable properties = ["Configuration", config
                              "TargetFrameworkVersion", "v4.5"]

    if isMono then
        properties <- properties @ ["DefineConstants", "MONO"]

    [slnFile]
        |> MSBuild binDir45 "Build" properties
        |> Log "Build: "
)

Target "Test" (fun _ ->
    if not <| directoryExists binDir45 then new Exception(sprintf "Directory %s does not exist." binDir45) |> raise
    ensureDirectory testResultsDir

    let framework = ref "net-4.5"
    let mutable testsDir = !! (binDir45 @@ "*Tests*.dll")
    if isMono then
        testsDir <- testsDir -- (binDir45 @@ "*VB.Tests*.dll")
        framework := "mono-4.0"

    testsDir
        |> NUnit (fun p -> 
            { p with 
                OutputFile = testResultsDir @@ getBuildParamOrDefault "testResults" "test-results.xml"
                DisableShadowCopy = true
                ShowLabels = Environment.GetEnvironmentVariable("MONGO_LOGGING") <> null
                Framework = !framework
                IncludeCategory = getBuildParamOrDefault "testInclude" ""
                ExcludeCategory = getBuildParamOrDefault "testExclude" ""
                TimeOut = TimeSpan.FromMinutes 10.0
            })
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
        |> MSBuild binDir45 "" properties
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

    let files =
        [ binDir45 @@ "MongoDB.Bson.dll"
          binDir45 @@ "MongoDB.Bson.pdb"
          binDir45 @@ "MongoDB.Bson.xml"
          binDir45 @@ "MongoDB.Driver.Core.dll"
          binDir45 @@ "MongoDB.Driver.Core.pdb"
          binDir45 @@ "MongoDB.Driver.Core.xml"
          binDir45 @@ "MongoDB.Driver.dll"
          binDir45 @@ "MongoDB.Driver.pdb"
          binDir45 @@ "MongoDB.Driver.xml"
          binDir45 @@ "MongoDB.Driver.GridFS.dll"
          binDir45 @@ "MongoDB.Driver.GridFS.pdb"
          binDir45 @@ "MongoDB.Driver.GridFS.xml"
          binDir45 @@ "MongoDB.Driver.Legacy.dll"
          binDir45 @@ "MongoDB.Driver.Legacy.pdb"
          binDir45 @@ "MongoDB.Driver.Legacy.xml"
          licenseFile
          releaseNotesFile 
          apiDocsArtifactFile ]

    files
        |> CreateZip artifactsDir zipArtifactFile "" DefaultZipLevel true
)

let createNuGetPackage file deps symbols =
    NuGetPack (fun p ->
      { p with
          Dependencies = deps
          Version = semVersion
          OutputPath = artifactsDir
          WorkingDir = baseDir
          SymbolPackage = if symbols then NugetSymbolPackage.Nuspec else NugetSymbolPackage.None })
      file

Target "NuGetPack" (fun _ ->
    !!(artifactsDir @@ "*.nupkg") |> DeleteFiles

    match preRelease with
    | "build" -> createNuGetPackage nuspecBuildFile [] true
    | _ ->
        for nuspecFile in nuspecFiles do
            let deps = nuspecFile.Dependencies |> List.map (fun x -> (x, semVersion))
            createNuGetPackage nuspecFile.File deps nuspecFile.Symbols
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


Target "NoOp" DoNothing
Target "Docs" DoNothing
Target "Package" DoNothing
Target "Publish" DoNothing

"Clean"
    ==> "AssemblyInfo"
    ==> "Build"

"RefDocs"
    ==> "ApiDocs"
    ==> "Docs"

"Docs"
    ==> "Zip"
    ==> "NuGetPack"
    ==> "Package"

"NuGetPush"
    ==> "Publish"

RunTargetOrDefault "Build"