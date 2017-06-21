@ECHO off
setlocal enableextensions enabledelayedexpansion

:: strlen("\scripts\") => 9
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-9%
cd %APP_HOME%

:: Check dependencies
docker version > NUL 2>&1
IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOCKER

:: Create cache folders to speed up future executions
mkdir .cache\sandbox\.config > NUL 2>&1
mkdir .cache\sandbox\.dotnet > NUL 2>&1
mkdir .cache\sandbox\.nuget > NUL 2>&1

:: Start the sandbox and execute the compilation script
docker run -it ^
    -v %APP_HOME%\.cache\sandbox\.config:/root/.config ^
    -v %APP_HOME%\.cache\sandbox\.dotnet:/root/.dotnet ^
    -v %APP_HOME%\.cache\sandbox\.nuget:/root/.nuget ^
    -v %APP_HOME%:/opt/code ^
    azureiotpcs/code-builder-dotnet:1.0-dotnetcore /opt/scripts/compile

:: Error 125 typically triggers on Windows if the drive is not shared
IF %ERRORLEVEL% EQU 125 GOTO DOCKER_SHARE
IF %ERRORLEVEL% NEQ 0 GOTO FAIL

:: - - - - - - - - - - - - - -
goto :END

:DOCKER_SHARE
    echo ERROR: the drive containing the source code cannot be mounted.
    echo Open Docker settings from the tray icon, and fix the settings under 'Shared Drives'.
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
