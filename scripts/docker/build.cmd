@ECHO off & setlocal enableextensions enabledelayedexpansion

:: Note: use lowercase names for the Docker images
SET DOCKER_IMAGE="azureiotpcs/device-simulation-dotnet"

:: Debug|Release
SET CONFIGURATION=Release

:: strlen("\scripts\docker\") => 16
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-16%
cd %APP_HOME%

:: The version is stored in a file, to avoid hardcoding it in multiple places
set /P APP_VERSION=<%APP_HOME%/version

:: Check dependencies
dotnet --version > NUL 2>&1
IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOTNET
docker version > NUL 2>&1
IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOCKER

:: Restore packages and build the application
call dotnet restore
IF %ERRORLEVEL% NEQ 0 GOTO FAIL
call dotnet build --configuration %CONFIGURATION%
IF %ERRORLEVEL% NEQ 0 GOTO FAIL

:: Build the container image
rmdir /s /q out\docker
rmdir /s /q WebService\bin\Docker
rmdir /s /q SimulationAgent\bin\Docker

mkdir out\docker\webservice
mkdir out\docker\simulationagent

dotnet publish WebService      --configuration %CONFIGURATION% --output bin\Docker
dotnet publish SimulationAgent --configuration %CONFIGURATION% --output bin\Docker

xcopy /s WebService\bin\Docker\*       out\docker\webservice\
xcopy /s SimulationAgent\bin\Docker\*  out\docker\simulationagent\

copy scripts\docker\.dockerignore               out\docker\
copy scripts\docker\Dockerfile                  out\docker\
copy scripts\docker\content\run.sh              out\docker\

cd out\docker\
docker build --tag %DOCKER_IMAGE%:%APP_VERSION% --squash --compress --label "Tags=azure,iot,pcs,simulation,.NET" .
IF %ERRORLEVEL% NEQ 0 GOTO FAIL

:: - - - - - - - - - - - - - -
goto :END

:MISSING_DOTNET
    echo ERROR: 'dotnet' command not found.
    echo Install .NET Core 1.1.2 and make sure the 'dotnet' command is in the PATH.
    echo Nuget installation: https://dotnet.github.io/
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
