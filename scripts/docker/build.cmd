@ECHO off
setlocal

:: Note: use lowercase names for the Docker images
SET DOCKER_IMAGE="azureiotpcs/device-simulation-dotnet:0.1-SNAPSHOT"

:: Debug|Release
SET CONFIGURATION=Release

:: strlen("\scripts\docker\") => 16
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-16%
cd %APP_HOME%

:: Check dependencies
nuget 2> NUL
IF %ERRORLEVEL% NEQ 0 GOTO MISSING_NUGET
msbuild /version 2> NUL
IF %ERRORLEVEL% NEQ 0 GOTO MISSING_MSBUILD
docker version > NUL
IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOCKER

:: Restore packages and build the application
call nuget restore
IF %ERRORLEVEL% NEQ 0 GOTO FAIL
call msbuild /m /p:Configuration=%CONFIGURATION%;Platform="Any CPU"
IF %ERRORLEVEL% NEQ 0 GOTO FAIL

:: Build the container image
rmdir /s /q out\docker
mkdir out\docker\webservice
mkdir out\docker\simulationagent

xcopy /s WebService\bin\%CONFIGURATION%\*       out\docker\webservice\
xcopy /s SimulationAgent\bin\%CONFIGURATION%\*  out\docker\simulationagent\
copy scripts\docker\.dockerignore               out\docker\
copy scripts\docker\Dockerfile                  out\docker\
copy scripts\docker\content\run.sh              out\docker\

cd out\docker\
docker build --tag %DOCKER_IMAGE% --squash --compress --label "Tags=azure,iot,pcs,.NET" .
IF %ERRORLEVEL% NEQ 0 GOTO FAIL

:: - - - - - - - - - - - - - -
goto :END

:MISSING_NUGET
    echo ERROR: 'nuget' command not found.
    echo Install Nuget CLI and make sure the 'nuget' command is in the PATH.
    echo Nuget installation: https://docs.microsoft.com/en-us/nuget/guides/install-nuget
    exit /B 1

:MISSING_MSBUILD
    echo ERROR: 'msbuild' command not found.
    echo Install Visual Studio IDE and make sure the 'msbuild' command is in the PATH.
    echo Visual Studio installation: https://docs.microsoft.com/visualstudio/install
    echo MSBuild installation without Visual Studio: http://stackoverflow.com/questions/42696948
    exit /B 1

:MISSING_DOCKER
    echo ERROR: 'docker' command not found.
    echo Install Docker and make sure the 'docker' command is in the PATH.
    echo Docker installation: https://www.docker.com/community-edition#/download
    exit /B 1

:FAIL
    echo Command failed
    endlocal
    exit /B 1

:END
endlocal
