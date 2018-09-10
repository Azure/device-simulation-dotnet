:: Copyright (c) Microsoft. All rights reserved.

::  Prepare the environment variables used by the application.
::
::  For more information about finding IoT Hub settings, more information here:
::
::  * https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal#endpoints
::  * https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted
::

:: Azure IoT Hub Connection string
SETX PCS_IOTHUB_CONNSTRING "your Azure IoT Hub connection string"

:: Endpoint to reach the storage adapter
SETX PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING "your DocumentDb connection string"

::  The Azure storage account used to create and delete devices in bulk
SETX PCS_AZURE_STORAGE_ACCOUNT "your Azure Storage Account connection string"
