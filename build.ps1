Properties {
	$base_version = "1.9"
	$version_status = "alpha"
	$build_number = Get-BuildNumber
	$git_commit = Get-GitCommit

	$version = "$base_version.$build_number"
	$sem_version = $base_version
	$short_version = Get-ShortenedVersion $sem_version
	if(-not [string]::IsNullOrEmpty($version_status)) {
		$sem_version = "$sem_version-$($version_status)-$build_number"
		$short_version = "$short_version-$($version_status)-$build_number"
	}
	$release_notes_version = Get-ShortenedVersion $base_version
	$config = 'Release'
	$installer_product_id = New-Object System.Guid($git_commit.Hash.SubString(0,32))
	$installer_upgrade_code = New-Object System.Guid($git_commit.Hash.SubString(1,32))

	Write-Host "$config Version $sem_version($version)" -ForegroundColor Yellow
	
	$base_dir = Split-Path $psake.build_script_file	
	$src_dir = "$base_dir"
	$tools_dir = "$base_dir\tools"
	$artifacts_dir = "$base_dir\artifacts"
	$35_build_dir = "$artifacts_dir\net35\build\"
	$35_test_results_dir = "$artifacts_dir\net35\test_results"
	$40_build_dir = "$artifacts_dir\net40\build\"
	$40_test_results_dir = "$artifacts_dir\net40\test_results"
	$docs_dir = "$artifacts_dir\docs"

	$sln_file = "$base_dir\CSharpDriver.sln"
	$asm_file = "$src_dir\GlobalAssemblyInfo.cs"
	$docs_file = "$base_dir\Docs\Api\CSharpDriverDocs.shfbproj"
	$installer_file = "$base_dir\Installer\CSharpDriverInstaller.wixproj"
	$nuspec_file = "$base_dir\mongocsharpdriver.nuspec"
	$chm_file = "$artifacts_dir\CSharpDriverDocs-$short_version.chm"
	$release_notes_file = "$base_dir\Release Notes\Release Notes v$release_notes_version.md"
	$license_file = "$base_dir\License.txt"

	$nuget_tool = "$tools_dir\nuget\nuget.exe"
	$nunit_tool = "$tools_dir\nunit\nunit-console.exe"
	$zip_tool = "$tools_dir\7Zip\7za.exe"
}

Framework('4.0')

Include tools\psake\psake-ext.ps1

function BuildHasBeenRun {
	$build_exists = (Test-Path $35_build_dir) -and (Test-Path $40_build_dir)
	Assert $build_exists "Build task has not been run"
	$true
}

function DocsHasBeenRun {
	$build_exists = Test-Path $chm_file
	Assert $build_exists "Docs task has not been run"
	$true
}

Task Default -Depends Build

Task Release -Depends Build, Docs, Zip, Installer, NugetPack

Task Clean {
	RemoveDirectory $artifacts_dir
	
	Write-Host "Cleaning $sln_file" -ForegroundColor Green
	Exec { msbuild "$sln_file" /t:Clean /p:Configuration=$config /v:quiet } 
}

Task Init -Depends Clean {
	Generate-AssemblyInfo `
		-file $asm_file `
		-version $version `
		-config $config `
		-sem_version $sem_version `
}

Task Build -Depends Init {	
	mkdir -p $35_build_dir | out-null
	Write-Host "Building $sln_file for .NET 3.5" -ForegroundColor Green
	Exec { msbuild "$sln_file" /t:Rebuild /p:Configuration=$config /p:TargetFrameworkVersion=v3.5 /v:quiet /p:OutDir=$35_build_dir } 

	mkdir -p $40_build_dir | out-null
	Write-Host "Building $sln_file for .NET 4.0" -ForegroundColor Green
	Exec { msbuild "$sln_file" /t:Rebuild /p:Configuration=$config /p:TargetFrameworkVersion=v4.0 /v:quiet /p:OutDir=$40_build_dir } 

	Reset-AssemblyInfo
}

Task Test -precondition { BuildHasBeenRun } {
	mkdir -p $35_test_results_dir | out-null
	$test_assemblies = ls -rec artifacts\net35\build\*Tests*.dll
	Write-Host "Testing $test_assemblies for .NET 3.5" -ForegroundColor Green
	Exec { &$nunit_tool $test_assemblies /xml=$35_test_results_dir\net35-test-results.xml /framework=net-3.5 /nologo /noshadow }

	mkdir -p $40_test_results_dir | out-null
	$test_assemblies = ls -rec artifacts\net40\build\*Tests*.dll
	Write-Host "Testing $test_assemblies for .NET 4.0" -ForegroundColor Green
	Exec { &$nunit_tool $test_assemblies /xml=$40_test_results_dir\net40-test-results.xml /framework=net-4.0 /nologo /noshadow }
}

Task Docs -precondition { BuildHasBeenRun } {
	RemoveDirectory $docs_dir

	mkdir -p $docs_dir | out-null
	Exec { msbuild "$docs_file" /p:Configuration=$config /p:CleanIntermediate=True /p:HelpFileVersion=$version /p:OutputPath=$docs_dir } 

	mv "$docs_dir\CSharpDriverDocs.chm" $chm_file
	mv "$docs_dir\Index.html" "$docs_dir\index.html"
	Exec { &$zip_tool a "$artifacts_dir\CSharpDriverDocs-$short_version-html.zip" "$docs_dir\*" }
	RemoveDirectory $docs_dir
}

task Zip -precondition { (BuildHasBeenRun) -and (DocsHasBeenRun) }{
	$zip_dir = "$artifacts_dir\ziptemp"
	
	RemoveDirectory $zip_dir

	mkdir -p $zip_dir | out-null
	
	$35_items = @("$35_build_dir\MongoDB.Bson.dll", `
		"$35_build_dir\MongoDB.Bson.pdb", `
		"$35_build_dir\MongoDB.Bson.xml", `
		"$35_build_dir\MongoDB.Driver.dll", `
		"$35_build_dir\MongoDB.Driver.pdb", `
		"$35_build_dir\MongoDB.Driver.xml")
	cp $35_items "$zip_dir"

	cp $license_file $zip_dir
	cp "Release Notes\Release Notes v$release_notes_version.md" "$zip_dir\Release Notes.txt"
	cp $chm_file "$zip_dir\CSharpDriverDocs.chm"

	Exec { &$zip_tool a "$artifacts_dir\CSharpDriver-$short_version.zip" "$zip_dir\*" }

	rd $zip_dir -rec -force | out-null
}

Task Installer -precondition { (BuildHasBeenRun) -and (DocsHasBeenRun) } {
	$release_notes_relative_path = Get-Item $release_notes_file | Resolve-Path -Relative
	$doc_relative_path = Get-Item $chm_file | Resolve-Path -Relative

	Exec { msbuild "$installer_file" /t:Rebuild /p:Configuration=$config /p:Version=$version /p:SemVersion=$short_version /p:ProductId=$installer_product_id /p:UpgradeCode=$installer_upgrade_code /p:ReleaseNotes=$release_notes_relative_path /p:License="License.rtf" /p:Documentation=$doc_relative_path /p:OutputPath=$artifacts_dir /p:BinDir=$35_build_dir}
	
	rm -force $artifacts_dir\*.wixpdb
}

task NugetPack -precondition { (BuildHasBeenRun) -and (DocsHasBeenRun) }{
	Exec { &$nuget_tool pack $nuspec_file -o $artifacts_dir -Version $sem_version -Symbols -BasePath $base_dir }
}