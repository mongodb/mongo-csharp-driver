@ECHO OFF

SET Version=1.4.1
SET SourceBase = ..

echo Creating CSharp driver installer v%Version%
pause

echo Cleaning binary directories
rmdir /s /q obj
rmdir /s /q bin

echo Building installer

%windir%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe

echo Done Building installer v%Version%
