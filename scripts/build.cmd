@ECHO off
setlocal enableextensions enabledelayedexpansion

:: Debug|Release
SET CONFIGURATION=Release

:: strlen("\scripts\") => 9
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-9%
cd %APP_HOME%

:: Check dependencies
nuget 2> NUL
IF NOT ERRORLEVEL 0 GOTO MISSING_NUGET
msbuild /version 2> NUL
IF NOT ERRORLEVEL 0 GOTO MISSING_MSBUILD

:: Restore nuget packages and compile the application
echo Downloading dependencies...
call nuget restore
IF NOT ERRORLEVEL 0 GOTO FAIL
echo Compiling code...
call msbuild /m /p:Configuration=%CONFIGURATION%;Platform="Any CPU"
IF NOT ERRORLEVEL 0 GOTO FAIL

:: Find all the test assemblies and run the tests with XUnit runner
echo Running tests...
SET PATTERN=bin\%CONFIGURATION%
for /r %%i in (*.Test.dll) do (
    echo.%%i | findstr /C:%PATTERN% >nul
    IF !ERRORLEVEL! EQU 0 (
        echo === %%i
        .\packages\xunit.runner.console.2.2.0\tools\xunit.console.exe %%i -verbose -nologo -noshadow -parallel all
        IF ERRORLEVEL 1 GOTO FAIL
    )
)

:: - - - - - - - - - - - - - -
goto :END

:MISSING_NUGET
    echo ERROR: 'nuget' command not found.
    echo Install Nuget CLI and make sure the 'nuget' command is in the PATH.
    echo Nuget installation: https://docs.microsoft.com/en-us/nuget/guides/install-nuget
    exit /B 1

:MISSING_MSBUILD
    echo ERROR: 'msbuild' command not found.
    echo Install Visual Studio IDE and make sure the 'msbuild' command is in the PATH.
    echo Visual Studio installation: https://docs.microsoft.com/visualstudio/install
    echo MSBuild installation without Visual Studio: http://stackoverflow.com/questions/42696948
    exit /B 1

:FAIL
    echo Command failed
    endlocal
    exit /B 1

:END
endlocal
