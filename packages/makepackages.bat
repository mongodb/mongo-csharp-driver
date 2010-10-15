set VER=0.5.0.3940
set PACKAGES=packages-%VER%

echo Creating %PACKAGES%
pause

rmdir /s /q %PACKAGES%
mkdir %PACKAGES%
xcopy /f /d /y ..\CSharpDriverSetup\Debug\CSharpDriver.msi %PACKAGES%\
ren %PACKAGES%\CSharpDriver.msi CSharpDriver-%VER%.msi

set ZIPEXE="C:\Program Files\7-Zip\7z.exe"
set ZIPFILE=%PACKAGES%\CSharpDriver-%VER%.zip
%ZIPEXE% a %ZIPFILE% ..\CSharpDriver\License.txt
%ZIPEXE% a %ZIPFILE% ..\CSharpDriver\bin\Debug\MongoDB.BsonLibrary.dll
%ZIPEXE% a %ZIPFILE% ..\CSharpDriver\bin\Debug\MongoDB.CSharpDriver.dll
%ZIPEXE% a %ZIPFILE% "..\docs\MongoDB C# Driver Tutorial (Draft 2010-09-30).pdf"

echo Created %PACKAGES%
pause
