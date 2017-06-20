@ECHO off
setlocal

:: Debug|Release
SET CONFIGURATION=Release

:: strlen("\scripts\") => 9
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-9%
cd %APP_HOME%

:: Check dependencies
dotnet --version > NUL 2>&1
IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOTNET

:: Check settings
call .\scripts\env-vars-check.cmd
IF %ERRORLEVEL% NEQ 0 GOTO FAIL

:: Restore nuget packages and compile the application
call dotnet restore
IF %ERRORLEVEL% NEQ 0 GOTO FAIL
call dotnet build --configuration %CONFIGURATION%
IF %ERRORLEVEL% NEQ 0 GOTO FAIL

cd %APP_HOME%
cd WebService\bin\%CONFIGURATION%\netcoreapp1.1\
start /B dotnet Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.dll --background

cd %APP_HOME%
cd SimulationAgent\bin\%CONFIGURATION%\netcoreapp1.1\\
call dotnet Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.dll

:: - - - - - - - - - - - - - -
goto :END

:MISSING_DOTNET
    echo ERROR: 'dotnet' command not found.
    echo Install .NET Core 1.1.2 and make sure the 'dotnet' command is in the PATH.
    echo Nuget installation: https://dotnet.github.io/
    exit /B 1

:FAIL
    echo Command failed
    endlocal
    exit /B 1

:END
endlocal
