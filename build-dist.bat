@echo off

msbuild src\directory-content-symlinker-vs2013.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"

del dist\*.* /F /Q

xcopy  src\directory-content-symlinker\bin\Release\*.* dist