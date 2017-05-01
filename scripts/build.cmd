@ECHO off
setlocal enableextensions enabledelayedexpansion

:: Debug|Release
SET CONFIGURATION=Release

:: strlen("\scripts\") => 9
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-9%
cd %APP_HOME%

call nuget restore
IF NOT ERRORLEVEL 0 GOTO FAIL
call msbuild /m /p:Configuration=%CONFIGURATION%;Platform="Any CPU"
IF NOT ERRORLEVEL 0 GOTO FAIL

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

:FAIL
echo Command failed
endlocal
exit /B 1

:END
endlocal
