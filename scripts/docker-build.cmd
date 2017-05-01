@ECHO off
setlocal

SET DOCKER_IMAGE="azureiotpcs/device-simulation-dotnet:0.1-SNAPSHOT"

:: Debug|Release
SET CONFIGURATION=Release

:: strlen("\scripts\") => 9
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-9%
cd %APP_HOME%

call nuget restore
IF NOT ERRORLEVEL 0 GOTO FAIL
call msbuild /m /p:Configuration=%CONFIGURATION%;Platform="Any CPU"
IF NOT ERRORLEVEL 0 GOTO FAIL

cd WebService\bin\%CONFIGURATION%
docker build --tag %DOCKER_IMAGE% --squash --compress --label "Tags=azure,iot,pcs,.NET" .
IF NOT ERRORLEVEL 0 GOTO FAIL

:: - - - - - - - - - - - - - -
goto :END

:FAIL
echo Command failed
endlocal
exit /B 1

:END
endlocal
