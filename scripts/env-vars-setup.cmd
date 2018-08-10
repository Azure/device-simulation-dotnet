:: Copyright (c) Microsoft. All rights reserved.

::  Prepare the environment variables used by the application.
::
::  For more information about finding IoT Hub settings, more information here:
::
::  * https://docs.microsoft.com/azure/iot-hub/iot-hub-create-through-portal#endpoints
::  * https://docs.microsoft.com/azure/iot-hub/iot-hub-csharp-csharp-getstarted
::

:: Azure IoT Hub Connection string
SETX PCS_IOTHUB_CONNSTRING "your Azure IoT Hub connection string"

:: Endpoint to reach the storage adapter
SETX PCS_STORAGEADAPTER_WEBSERVICE_URL "http://127.0.0.1:9022/v1"
