@ECHO off & setlocal enableextensions enabledelayedexpansion

IF "%PCS_IOTHUB_CONNSTRING%" == "" (
    echo Error: the PCS_IOTHUB_CONNSTRING environment variable is not defined.
    exit /B 1
)

endlocal
