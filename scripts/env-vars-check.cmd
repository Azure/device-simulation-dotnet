@ECHO off & setlocal enableextensions enabledelayedexpansion

IF "%PCS_DEVICESIMULATION_WEBSERVICE_PORT%" == "" (
    echo Error: the PCS_DEVICESIMULATION_WEBSERVICE_PORT environment variable is not defined.
    exit /B 1
)

IF "%PCS_IOTHUBMANAGER_WEBSERVICE_URL%" == "" (
    echo Error: the PCS_IOTHUBMANAGER_WEBSERVICE_URL environment variable is not defined.
    exit /B 1
)

endlocal
