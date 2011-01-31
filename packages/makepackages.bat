set VER=0.11.0.4042
set PACKAGES=packages-%VER%

echo Creating %PACKAGES%
pause

rmdir /s /q %PACKAGES%
mkdir %PACKAGES%
xcopy /f /d /y ..\DriverSetup\Debug\CSharpDriver.msi %PACKAGES%\
ren %PACKAGES%\CSharpDriver.msi CSharpDriver-%VER%.msi

set ZIPEXE="C:\Program Files\7-Zip\7z.exe"
set ZIPFILE=%PACKAGES%\CSharpDriver-%VER%.zip
%ZIPEXE% a %ZIPFILE% ..\License.txt
%ZIPEXE% a %ZIPFILE% "..\Release Notes v0.11.txt"
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Debug\MongoDB.Bson.dll
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Debug\MongoDB.Bson.pdb
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Debug\MongoDB.Driver.dll
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Debug\MongoDB.Driver.pdb

echo Created %PACKAGES%
pause
