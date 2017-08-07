@ECHO off & setlocal enableextensions enabledelayedexpansion

:: Note: use lowercase names for the Docker images
SET DOCKER_IMAGE="azureiotpcs/device-simulation-dotnet"

:: strlen("\scripts\docker\") => 16
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-16%
cd %APP_HOME%

:: The version is stored in a file, to avoid hardcoding it in multiple places
set /P APP_VERSION=<%APP_HOME%/version

:: Check dependencies
docker version > NUL 2>&1
IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOCKER

:: Check settings
call .\scripts\env-vars-check.cmd
IF %ERRORLEVEL% NEQ 0 GOTO FAIL

:: Start the application
echo Starting Device Simulation ...
docker run -it -p %PCS_DEVICESIMULATION_WEBSERVICE_PORT%:%PCS_DEVICESIMULATION_WEBSERVICE_PORT% ^
    -e PCS_DEVICESIMULATION_WEBSERVICE_PORT=%PCS_DEVICESIMULATION_WEBSERVICE_PORT% ^
    -e PCS_IOTHUBMANAGER_WEBSERVICE_URL=%PCS_IOTHUBMANAGER_WEBSERVICE_URL% ^
    %DOCKER_IMAGE%:%APP_VERSION%

:: - - - - - - - - - - - - - -
goto :END

:FAIL
    echo Command failed
    endlocal
    exit /B 1

:MISSING_DOCKER
    echo ERROR: 'docker' command not found.
    echo Install Docker and make sure the 'docker' command is in the PATH.
    echo Docker installation: https://www.docker.com/community-edition#/download
    exit /B 1

:END
endlocal
