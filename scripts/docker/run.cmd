@ECHO off

:: Note: use lowercase names for the Docker images
SET DOCKER_IMAGE="azureiotpcs/device-simulation-dotnet:0.1-SNAPSHOT"

:: strlen("\scripts\docker\") => 16
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-16%
cd %APP_HOME%

:: Check dependencies
docker version > NUL 2>&1
IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOCKER

:: Check settings
call .\scripts\env-vars-check.cmd
IF %ERRORLEVEL% NEQ 0 GOTO FAIL

:: Start the application
echo Starting Device Simulation ...
docker run -it -p %PCS_SIMULATION_WEBSERVICE_PORT%:8080 ^
    -e PCS_SIMULATION_WEBSERVICE_PORT=8080 ^
    -e PCS_IOTHUBMANAGER_WEBSERVICE_HOST=%PCS_IOTHUBMANAGER_WEBSERVICE_HOST% ^
    -e PCS_IOTHUBMANAGER_WEBSERVICE_PORT=%PCS_IOTHUBMANAGER_WEBSERVICE_PORT% ^
    %DOCKER_IMAGE%

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
