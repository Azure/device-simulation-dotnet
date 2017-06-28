:: Usage:
:: Run the service in the local environment:     .\scripts\run
:: Run the service in inside a Docker container: .\scripts\run -s
:: Run the service in inside a Docker container: .\scripts\run --in-sandbox

@ECHO off
setlocal

:: Debug|Release
SET CONFIGURATION=Release

:: strlen("\scripts\") => 9
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-9%
cd %APP_HOME%

IF "%1"=="-s" GOTO :RunInSandbox
IF "%1"=="--in-sandbox" GOTO :RunInSandbox


:RunHere
    :: Check dependencies
    dotnet --version > NUL 2>&1
    IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOTNET

    :: Check settings
    call .\scripts\env-vars-check.cmd
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    :: Restore nuget packages and compile the application
    call dotnet restore
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    start "" dotnet run --configuration %CONFIGURATION% --project SimulationAgent/SimulationAgent.csproj
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    start "" dotnet run --configuration %CONFIGURATION% --project WebService/WebService.csproj --background
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    goto :END


:RunInSandbox
    :: Check dependencies
    docker version > NUL 2>&1
    IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOCKER

    :: Create cache folders to speed up future executions
    mkdir .cache\sandbox\.config 2>NUL
    mkdir .cache\sandbox\.dotnet 2>NUL
    mkdir .cache\sandbox\.nuget 2>NUL

    :: Check settings
    call .\scripts\env-vars-check.cmd
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    :: Start the sandbox and run the application
    docker run -it ^
        -p %PCS_DEVICESIMULATION_WEBSERVICE_PORT%:8080 ^
        -e "PCS_DEVICESIMULATION_WEBSERVICE_PORT=8080" ^
        -e "PCS_IOTHUBMANAGER_WEBSERVICE_URL=%PCS_IOTHUBMANAGER_WEBSERVICE_URL%" ^
        -v %APP_HOME%\.cache\sandbox\.config:/root/.config ^
        -v %APP_HOME%\.cache\sandbox\.dotnet:/root/.dotnet ^
        -v %APP_HOME%\.cache\sandbox\.nuget:/root/.nuget ^
        -v %APP_HOME%:/opt/code ^
        azureiotpcs/code-builder-dotnet:1.0-dotnetcore /opt/scripts/run

    :: Error 125 typically triggers on Windows if the drive is not shared
    IF %ERRORLEVEL% EQU 125 GOTO DOCKER_SHARE
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    goto :END


:: - - - - - - - - - - - - - -
goto :END

:MISSING_DOTNET
    echo ERROR: 'dotnet' command not found.
    echo Install .NET Core 1.1.2 and make sure the 'dotnet' command is in the PATH.
    echo Nuget installation: https://dotnet.github.io/
    exit /B 1

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
