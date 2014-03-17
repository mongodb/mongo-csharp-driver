function Get-GitCommit
{
  try {
    $commit = git log -1 --pretty=format:%H
    $git_date = git log -1 --date=iso --pretty=format:%ad
    $commit_date = [DateTime]::Parse($git_date).ToString("yyyy-MM-dd HH:mm:ss")
    return @{Hash=$commit;Date=$commit_date}
  }
  catch {
    return @(Hash="git unavailable";Date=[DateTime]::Now.ToString("yyyy-MM-dd HH:mm:ss"))
  }
}

function Get-BuildNumber
{
  try {
    $version = git describe HEAD^1 --tags --long --match "v[0-9].[0-9].[0-9].[0-9]*"
    $regex = [regex]"-(\d+)-"
    $match = $regex.Match($version)
    return $match.Groups[1].Value
  }
  catch {
    return "0"
  }
}

function Get-ShortenedVersion($version)
{
  if($version.EndsWith(".0")) {
    return $version.SubString(0, $version.Length - 2)
  }

  return $version
}

function Generate-AssemblyInfo
{
param(
  [string]$config,
  [string]$version,
  [string]$sem_version,
  [string]$file = $(throw "file is a required parameter.")
)
  $commit = Get-GitCommit
  $asmInfo = "/* Copyright 2010-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the ""License"");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an ""AS IS"" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

[assembly: AssemblyCompany(""MongoDB Inc."")]
[assembly: AssemblyCopyright(""Copyright © 2010-2014 MongoDB Inc."")]
[assembly: AssemblyVersion(""$version"")]
[assembly: AssemblyInformationalVersion(""{version: '$version', semver: '$sem_version', commit: '$($commit.Hash)', commit_date: '$($commit.Date)'}"")]
[assembly: AssemblyFileVersion(""$version"")]
[assembly: AssemblyConfiguration(""$config"")]
[assembly: AllowPartiallyTrustedCallers()]"

  $dir = [System.IO.Path]::GetDirectoryName($file)
  if ([System.IO.Directory]::Exists($dir) -eq $false)
  {
    Write-Host "Creating directory $dir"
    [System.IO.Directory]::CreateDirectory($dir)
  }
  Write-Host "Generating assembly info file: $file"
  $asmInfo | Out-File -Encoding UTF8 $file
}

function RemoveDirectory($path) {
  if(Test-Path $path) {
    rd -rec -force $path | out-null
  }
}

function Reset-AssemblyInfo 
{
  try {
    git checkout GlobalAssemblyInfo.cs
  }
  catch {
    Write-Host "Unable to reset assembly info"
  } 
}