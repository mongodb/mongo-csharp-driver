Properties {
    $base_version = "1.9.0"
    $pre_release = "local"
    $build_number = Get-BuildNumber
    $config = "Release"

    $git_commit = Get-GitCommit

    $base_dir = Split-Path $psake.build_script_file 
    $src_dir = "$base_dir"
    $tools_dir = "$base_dir\tools"
    $artifacts_dir = "$base_dir\artifacts"
    $bin_dir = "$artifacts_dir\bin\"
    $test_results_dir = "$artifacts_dir\test_results"
    $docs_dir = "$artifacts_dir\docs"

    $sln_file = "$base_dir\CSharpDriver.sln"
    $asm_file = "$src_dir\GlobalAssemblyInfo.cs"
    $docs_file = "$base_dir\Docs\Api\CSharpDriverDocs.shfbproj"
    $installer_file = "$base_dir\Installer\CSharpDriverInstaller.wixproj"
    $nuspec_file = "$base_dir\mongocsharpdriver.nuspec"
    $nuspec_build_file = "$base_dir\mongocsharpdriverbuild.nuspec"
    $license_file = "$base_dir\License.txt"
    $version_file = "$artifacts_dir\version.txt"

    $nuget_tool = "$tools_dir\nuget\nuget.exe"
    $nunit_tool = "$tools_dir\nunit\nunit-console.exe"
    $zip_tool = "$tools_dir\7Zip\7za.exe"
}

function IsReleaseBuild {
    if($pre_release -eq "build" -or $pre_release -eq "local") {
        return $false
    }

    return $true
}

TaskSetup {

    $global:version = "$base_version.$build_number"
    $global:sem_version = $base_version
    $global:short_version = Get-ShortenedVersion $sem_version
    if(-not [string]::IsNullOrEmpty($pre_release)) {
        $global:sem_version = "$sem_version-$($pre_release)"
        $global:short_version = "$short_version-$($pre_release)"

        if(-not (IsReleaseBuild)) {
            # These should be + instead of -, but nuget doesn't allow that right now
            # Also padding the build number because nuget sorts lexigraphically
            # meaning that 2 > 10.  So, we make it such that 0002 < 0010.
            # Note: will we ever have > 9999 commits between releases?
            # Note: 0 + $build_number is to coerce the build_number into an integer.
            $bn = "{0:0000}" -f (0 + $build_number)
            $global:sem_version = "$sem_version-$bn"
            $global:short_version = "$short_version-$bn"
        }
    }

    Write-Host "$config Version $sem_version($version)" -ForegroundColor Yellow

    $global:release_notes_version = Get-ShortenedVersion $base_version
    $global:installer_product_id = New-Object System.Guid($git_commit.Hash.SubString(0,32))
    $global:installer_upgrade_code = New-Object System.Guid($git_commit.Hash.SubString(1,32))

    $global:chm_file = "$artifacts_dir\CSharpDriverDocs-$short_version.chm"
    $global:release_notes_file = "$base_dir\Release Notes\Release Notes v$release_notes_version.md"
}

Framework('4.0')

Include tools\psake\psake-ext.ps1

function BuildHasBeenRun {
    $build_exists = (Test-Path $bin_dir)
    Assert $build_exists "Build task has not been run."
    $true
}

function DocsHasBeenRun {
    $build_exists = Test-Path $chm_file
    Assert $build_exists "Docs task has not been run."
    $true
}

function NotLocalPreRelease {
    $is_not_local = $pre_release -ne "local"
    Assert $is_not_local "Cannot run task on a local build. Specify a different (or none) pre-release version."
    $true
}

Task Default -Depends Build

Task OutputVersion {
    if(-not (Test-Path $artifacts_dir)) {
        mkdir -path $artifacts_dir | out-null
    }

    $Utf8NoBomEncoding = New-Object System.Text.UTF8Encoding($False)

    Write-Host "Writing version file to $version_file" -ForegroundColor Green
    $lines = @("BUILD_VERSION=$base_version";"BUILD_PRE_RELEASE=$pre_release";"BUILD_NUMBER=$build_number";"BUILD_SEM_VERSION=$sem_version")
    [System.IO.File]::WriteAllLines($version_file, $lines, $Utf8NoBomEncoding)
}

Task Clean {
    RemoveDirectory $artifacts_dir
    
    Write-Host "Cleaning $sln_file" -ForegroundColor Green
    Exec { msbuild "$sln_file" /t:Clean /p:Configuration=$config /v:quiet } 
}

Task Build -Depends Clean, OutputVersion {  
    try {
        Generate-AssemblyInfo `
            -file $asm_file `
            -version $version `
            -config $config `
            -sem_version $sem_version `

        mkdir -path $bin_dir | out-null
        Write-Host "Building $sln_file for .NET 3.5" -ForegroundColor Green
        Exec { msbuild "$sln_file" /t:Rebuild /p:Configuration=$config /p:TargetFrameworkVersion=v3.5 /v:quiet /p:OutDir=$bin_dir } 
    }
    finally {
        Reset-AssemblyInfo
    }
}

Task Test -precondition { BuildHasBeenRun } {
    mkdir -path $test_results_dir | out-null
    $test_assemblies = ls -rec $bin_dir/*Tests*.dll
    Write-Host "Testing $test_assemblies for .NET 3.5" -ForegroundColor Green
    Exec { &$nunit_tool $test_assemblies /xml=$test_results_dir\test-results.xml /framework=net-3.5 /nologo /noshadow }
}

Task Docs -precondition { BuildHasBeenRun } {
    RemoveDirectory $docs_dir

    mkdir -path $docs_dir | out-null

    $preliminary = "False"
    if(-not [string]::IsNullOrEmpty($pre_release)) {
        $preliminary = "True"
    }

    Exec { msbuild "$docs_file" /p:Configuration=$config /p:CleanIntermediate=True /p:Preliminary=$preliminary /p:HelpFileVersion=$version /p:OutputPath=$docs_dir } 

    mv "$docs_dir\CSharpDriverDocs.chm" $chm_file
    mv "$docs_dir\Index.html" "$docs_dir\index.html"
    Exec { &$zip_tool a "$artifacts_dir\CSharpDriverDocs-$short_version-html.zip" "$docs_dir\*" }
    RemoveDirectory $docs_dir
}

task Zip -precondition { (BuildHasBeenRun) -and (DocsHasBeenRun) }{
    $zip_dir = "$artifacts_dir\ziptemp"
    
    RemoveDirectory $zip_dir

    mkdir -path $zip_dir | out-null
    
    $items = @("$bin_dir\MongoDB.Bson.dll", `
        "$bin_dir\MongoDB.Bson.pdb", `
        "$bin_dir\MongoDB.Bson.xml", `
        "$bin_dir\MongoDB.Driver.dll", `
        "$bin_dir\MongoDB.Driver.pdb", `
        "$bin_dir\MongoDB.Driver.xml")
    cp $items "$zip_dir"

    cp $license_file $zip_dir
    cp "Release Notes\Release Notes v$release_notes_version.md" "$zip_dir\Release Notes.txt"
    cp $chm_file "$zip_dir\CSharpDriverDocs.chm"

    Exec { &$zip_tool a "$artifacts_dir\CSharpDriver-$short_version.zip" "$zip_dir\*" }

    rd $zip_dir -rec -force | out-null
}

Task Installer -precondition { (BuildHasBeenRun) -and (DocsHasBeenRun) } {
    $release_notes_relative_path = Get-Item $release_notes_file | Resolve-Path -Relative
    $doc_relative_path = Get-Item $chm_file | Resolve-Path -Relative

    Exec { msbuild "$installer_file" /t:Rebuild /p:Configuration=$config /p:Version=$version /p:SemVersion=$short_version /p:ProductId=$installer_product_id /p:UpgradeCode=$installer_upgrade_code /p:ReleaseNotes=$release_notes_relative_path /p:License="License.rtf" /p:Documentation=$doc_relative_path /p:OutputPath=$artifacts_dir /p:BinDir=$bin_dir}
    
    rm -force $artifacts_dir\*.wixpdb
}

task NugetPack -precondition { (NotLocalPreRelease) -and (BuildHasBeenRun) -and (DocsHasBeenRun) } {

    $nf = $nuspec_file
    if($pre_release -eq "build") {
        $nf = $nuspec_build_file
    }


    Exec { &$nuget_tool pack $nf -o $artifacts_dir -Version $sem_version -Symbols -BasePath $base_dir }
}