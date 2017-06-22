@ECHO off
setlocal enableextensions enabledelayedexpansion

:: strlen("\scripts\") => 9
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-9%
cd %APP_HOME%

:: Check dependencies
dotnet --version > NUL 2>&1
IF %ERRORLEVEL% NEQ 0 GOTO MISSING_DOTNET

:: Restore nuget packages and compile the application with both Debug and Release configurations
call dotnet restore
IF %ERRORLEVEL% NEQ 0 GOTO FAIL
call dotnet build --configuration Debug
IF %ERRORLEVEL% NEQ 0 GOTO FAIL
call dotnet build --configuration Release
IF %ERRORLEVEL% NEQ 0 GOTO FAIL

:: - - - - - - - - - - - - - -
goto :END

:MISSING_DOTNET
    echo ERROR: 'dotnet' command not found.
    echo Install .NET Core 1.1.2 and make sure the 'dotnet' command is in the PATH.
    echo Nuget installation: https://dotnet.github.io
    exit /B 1

:FAIL
    echo Command failed
    endlocal
    exit /B 1

:END
endlocal
