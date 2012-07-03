@ECHO OFF
SET FullVersion=1.5.0.4566
SET Version=1.5
SET Configuration=Release
SET SourceBase=..

echo Creating CSharp driver installer v%Version%

echo Cleaning binary directories
rmdir /s /q obj\%Configuration%
rmdir /s /q bin\%Configuration%

echo Building installer

%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe CSharpDriverInstaller.wixproj

echo Done Building installer v%Version%
