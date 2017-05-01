@ECHO off
setlocal

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

copy .\scripts\run.vbs .\WebService\bin\%CONFIGURATION%

cd WebService\bin\%CONFIGURATION%
call cscript run.vbs "Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.exe"

:: - - - - - - - - - - - - - -
goto :END

:FAIL
echo Command failed
endlocal
exit /B 1

:END
endlocal
