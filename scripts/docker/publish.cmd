@ECHO off & setlocal enableextensions enabledelayedexpansion

:: Note: use lowercase names for the Docker images
SET DOCKER_IMAGE="azureiotpcs/device-simulation-dotnet"

:: strlen("\scripts\docker\") => 16
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-16%
cd %APP_HOME%

set /P APP_VERSION=<%APP_HOME%/version

docker push %DOCKER_IMAGE%:%APP_VERSION%

endlocal
