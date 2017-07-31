@ECHO off & setlocal enableextensions enabledelayedexpansion

:: Usage:
:: Run the service in the local environment:  scripts\run
:: Run the service inside a Docker container: scripts\run -s
:: Run the service inside a Docker container: scripts\run --in-sandbox
:: Run only the web service:                  scripts\run --webservice
:: Run only the simulation:                   scripts\run --simulation
:: Run the IoT Hub Manager Docker image:      scripts\run --iothubman
:: Show how to use this script:               scripts\run -h
:: Show how to use this script:               scripts\run --help

:: Debug|Release
SET CONFIGURATION=Release

:: strlen("\scripts\") => 9
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-9%
cd %APP_HOME%

IF "%1"=="-h" GOTO :Help
IF "%1"=="--help" GOTO :Help
IF "%1"=="-s" GOTO :RunInSandbox
IF "%1"=="--in-sandbox" GOTO :RunInSandbox
IF "%1"=="--webservice" GOTO :RunWebService
IF "%1"=="--simulation" GOTO :RunSimulation
IF "%1"=="--iothubman" GOTO :RunIoTHubMan

:Help

    echo "Usage:"
    echo "  Run the service in the local environment:  ./scripts/run"
    echo "  Run the service inside a Docker container: ./scripts/run -s|--in-sandbox"
    echo "  Run only the web service:                  ./scripts/run --webservice"
    echo "  Run only the simulation:                   ./scripts/run --simulation"
    echo "  Run the IoT Hub Manager Docker image:      ./scripts/run --iothubman"
    echo "  Show how to use this script:               ./scripts/run -h|--help"

    goto :END


:RunLocally

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

    start "" dotnet run --configuration %CONFIGURATION% --project WebService/WebService.csproj
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    goto :END


:RunWebService

    :: Check dependencies
    dotnet --version > NUL 2>&1
    IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOTNET

    :: Check settings
    call .\scripts\env-vars-check.cmd
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    :: Restore nuget packages and compile the application
    call dotnet restore
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    dotnet run --configuration %CONFIGURATION% --project WebService/WebService.csproj
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    goto :END


:RunSimulation

    :: Check dependencies
    dotnet --version > NUL 2>&1
    IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOTNET

    :: Check settings
    call .\scripts\env-vars-check.cmd
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    :: Restore nuget packages and compile the application
    call dotnet restore
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    dotnet run --configuration %CONFIGURATION% --project SimulationAgent/SimulationAgent.csproj
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    goto :END


:RunInSandbox

    :: Folder where PCS sandboxes cache data. Reuse the same folder to speed up the
    :: sandbox and to save disk space.
    :: Use PCS_CACHE="%APP_HOME%\.cache" to cache inside the project folder
    SET PCS_CACHE="%TMP%\azure\iotpcs\.cache"

    :: Check dependencies
    docker version > NUL 2>&1
    IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOCKER

    :: Create cache folders to speed up future executions
    mkdir %PCS_CACHE%\sandbox\.config > NUL 2>&1
    mkdir %PCS_CACHE%\sandbox\.dotnet > NUL 2>&1
    mkdir %PCS_CACHE%\sandbox\.nuget > NUL 2>&1
    echo Note: caching build files in %PCS_CACHE%

    :: Check settings
    call .\scripts\env-vars-check.cmd
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    :: Start the sandbox and run the application
    docker run -it ^
        -p %PCS_DEVICESIMULATION_WEBSERVICE_PORT%:%PCS_DEVICESIMULATION_WEBSERVICE_PORT% ^
        -e "PCS_DEVICESIMULATION_WEBSERVICE_PORT=%PCS_DEVICESIMULATION_WEBSERVICE_PORT%" ^
        -e "PCS_DEVICESIMULATION_CORS_WHITELIST=%PCS_DEVICESIMULATION_CORS_WHITELIST%" ^
        -e "PCS_IOTHUBMANAGER_WEBSERVICE_URL=%PCS_IOTHUBMANAGER_WEBSERVICE_URL%" ^
        -v %PCS_CACHE%\sandbox\.config:/root/.config ^
        -v %PCS_CACHE%\sandbox\.dotnet:/root/.dotnet ^
        -v %PCS_CACHE%\sandbox\.nuget:/root/.nuget ^
        -v %APP_HOME%:/opt/code ^
        azureiotpcs/code-builder-dotnet:1.0-dotnetcore /opt/code/scripts/run

    :: Error 125 typically triggers in Windows if the drive is not shared
    IF %ERRORLEVEL% EQU 125 GOTO DOCKER_SHARE
    IF %ERRORLEVEL% NEQ 0 GOTO FAIL

    goto :END


:RunIoTHubMan

    :: Check dependencies
    docker version > NUL 2>&1
    IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOCKER

    IF "%PCS_IOTHUBMANAGER_WEBSERVICE_PORT%" == "" (
        echo Error: the PCS_IOTHUBMANAGER_WEBSERVICE_PORT environment variable is not defined.
        exit /B 1
    )

    IF "%PCS_IOTHUB_CONN_STRING%" == "" (
        echo Error: the PCS_IOTHUB_CONN_STRING environment variable is not defined.
        exit /B 1
    )

    SET VERSION=latest
    docker run -it -p %PCS_IOTHUBMANAGER_WEBSERVICE_PORT%:%PCS_IOTHUBMANAGER_WEBSERVICE_PORT% ^
        -e PCS_IOTHUBMANAGER_WEBSERVICE_PORT=%PCS_IOTHUBMANAGER_WEBSERVICE_PORT% ^
        -e PCS_IOTHUB_CONN_STRING=%PCS_IOTHUB_CONN_STRING% ^
        azureiotpcs/iothubmanager-dotnet:%VERSION%

    goto :END


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

:DOCKER_SHARE
    echo ERROR: the drive containing the source code cannot be mounted.
    echo Open Docker settings from the tray icon, and fix the settings under 'Shared Drives'.
    exit /B 1

:FAIL
    echo Command failed
    endlocal
    exit /B 1

:END
endlocal
