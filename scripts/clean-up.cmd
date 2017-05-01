@ECHO off
setlocal

:: strlen("\scripts\") => 9
SET APP_HOME=%~dp0
SET APP_HOME=%APP_HOME:~0,-9%
if "%APP_HOME:~20%" == "" (
    echo Unable to detect current folder. Aborting.
    GOTO FAIL
)

echo Removing temporary folders and files...

:cleanup_dotnet
    cd %APP_HOME%
    IF NOT ERRORLEVEL 0 GOTO FAIL
    rmdir /s /q .\packages
    rmdir /s /q .\Services\bin
    rmdir /s /q .\Services\obj
    rmdir /s /q .\Services.Test\bin
    rmdir /s /q .\Services.Test\obj
    rmdir /s /q .\WebService\bin
    rmdir /s /q .\WebService\obj
    rmdir /s /q .\WebService.Test\bin
    rmdir /s /q .\WebService.Test\obj


echo Done.

:: - - - - - - - - - - - - - -
goto :END

:FAIL
echo Command failed
endlocal
exit /B 1

:END
endlocal
