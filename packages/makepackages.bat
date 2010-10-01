rmdir /s /q installer
mkdir installer
xcopy /f /d /y ..\CSharpDriverSetup\Debug\CSharpDriverSetup.msi installer\

rmdir /s /q zip
mkdir zip
set ZIP="C:\Program Files\7-Zip\7z.exe"
%ZIP% a zip/CSharpDriver.zip ..\CSharpDriver\License.txt
%ZIP% a zip/CSharpDriver.zip ..\CSharpDriver\bin\Debug\MongoDB.BsonLibrary.dll
%ZIP% a zip/CSharpDriver.zip ..\CSharpDriver\bin\Debug\MongoDB.CSharpDriver.dll
%ZIP% a zip/CSharpDriver.zip "..\docs\MongoDB C# Driver Tutorial (Draft 2010-09-30).pdf"

pause
