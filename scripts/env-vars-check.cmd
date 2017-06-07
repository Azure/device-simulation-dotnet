@ECHO off
setlocal enableextensions enabledelayedexpansion

IF "%PCS_SIMULATION_WEBSERVICE_PORT%" == "" (
    echo Error: the PCS_SIMULATION_WEBSERVICE_PORT environment variable is not defined.
    exit /B 1
)

IF "%PCS_IOTHUBMANAGER_WEBSERVICE_HOST%" == "" (
    echo Error: the PCS_IOTHUBMANAGER_WEBSERVICE_HOST environment variable is not defined.
    exit /B 1
)

IF "%PCS_IOTHUBMANAGER_WEBSERVICE_PORT%" == "" (
    echo Error: the PCS_IOTHUBMANAGER_WEBSERVICE_PORT environment variable is not defined.
    exit /B 1
)

endlocal
