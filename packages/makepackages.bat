set VER=1.3.1.4349
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
%ZIPEXE% a %ZIPFILE% "..\Release Notes v1.3.1.txt"
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Debug\MongoDB.Bson.dll
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Debug\MongoDB.Bson.pdb
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Debug\MongoDB.Bson.xml
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Debug\MongoDB.Driver.dll
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Debug\MongoDB.Driver.pdb
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Debug\MongoDB.Driver.xml
%ZIPEXE% a %ZIPFILE% ..\Help\CSharpDriverDocs.chm

echo Created %PACKAGES%
pause
