@ECHO off

:: Note: use lowercase names for the Docker images
SET DOCKER_IMAGE="azureiotpcs/device-simulation-dotnet:0.1-SNAPSHOT"
SET EXT_PORT=8080

:: Check dependencies
docker version > NUL
IF NOT ERRORLEVEL 0 GOTO MISSING_DOCKER

:: Start the application
echo Starting microservice...
docker run -it -p %EXT_PORT%:8080 -e PCS_IOTHUB_CONN_STRING=%PCS_IOTHUB_CONN_STRING% %DOCKER_IMAGE%

:: - - - - - - - - - - - - - -
goto :END

:MISSING_DOCKER
    echo ERROR: 'docker' command not found.
    echo Install Docker and make sure the 'docker' command is in the PATH.
    echo Docker installation: https://www.docker.com/community-edition#/download
    exit /B 1

:END
endlocal
