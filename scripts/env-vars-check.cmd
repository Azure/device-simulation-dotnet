:: Copyright (c) Microsoft. All rights reserved.

@ECHO off & setlocal enableextensions enabledelayedexpansion

IF "%PCS_IOTHUB_CONNSTRING%" == "" (
    echo Error: the PCS_IOTHUB_CONNSTRING environment variable is not defined.
    exit /B 1
)

IF "%PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING%" == "" (
    echo Error: the PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING environment variable is not defined.
    exit /B 1
)

endlocal
