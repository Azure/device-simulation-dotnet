#  Prepare the environment variables used by the application.
#
#  For more information about finding IoT Hub settings, more information here:
#
#  * https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal#endpoints
#  * https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-csharp-csharp-getstarted
#

# see: Shared access policies ⇒ key name ⇒ Connection string
$env:IOTHUB_CONN_STRING = '...'

# The host where IoT Hub Manager web service is listening
$env:PCS_IOTHUBMANAGER_WEBSERVICE_HOST = '127.0.0.1'

# The port where IoT Hub Manager web service is listening
$env:PCS_IOTHUBMANAGER_WEBSERVICE_PORT = '9001'

# The port where Device Simulation web service is listening
$env:PCS_SIMULATION_WEBSERVICE_PORT = '9002'
