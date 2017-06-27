::  Prepare the environment variables used by the application.
::
::  For more information about finding IoT Hub settings, more information here:
::
::  * https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal#endpoints
::  * https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted
::

:: The port where Device Simulation web service is listening
SET PCS_DEVICESIMULATION_WEBSERVICE_PORT = "9002"

:: The URL where IoT Hub Manager web service is listening
SET PCS_IOTHUBMANAGER_WEBSERVICE_URL = "http://127.0.0.1:9002/v1"
