set VER=1.6.0.4624
set PACKAGES=packages-%VER%

echo Creating %PACKAGES%
pause

rmdir /s /q %PACKAGES%
mkdir %PACKAGES%
xcopy /f /d /y ..\Installer\bin\Release\CSharpDriver-%VER%.msi %PACKAGES%\

del "Release Notes v.1.6.txt"
xcopy /f /y "..\Release Notes\Release Notes v1.6.md"
ren "Release Notes v1.6.md" "Release Notes v1.6.txt"

set ZIPEXE="C:\Program Files\7-Zip\7z.exe"
set ZIPFILE=%PACKAGES%\CSharpDriver-%VER%.zip
%ZIPEXE% a %ZIPFILE% ..\License.txt
%ZIPEXE% a %ZIPFILE% "Release Notes v1.6.txt"
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Release\MongoDB.Bson.dll
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Release\MongoDB.Bson.pdb
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Release\MongoDB.Bson.xml
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Release\MongoDB.Driver.dll
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Release\MongoDB.Driver.pdb
%ZIPEXE% a %ZIPFILE% ..\Driver\bin\Release\MongoDB.Driver.xml
%ZIPEXE% a %ZIPFILE% ..\Help\CSharpDriverDocs.chm

del "Release Notes v1.6.txt"

echo Created %PACKAGES%
pause
