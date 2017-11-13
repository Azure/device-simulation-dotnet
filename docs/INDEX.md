Azure IoT Device Simulation
===========================

This service allows management of a pool of simulated devices.  The primarily
objective is to allow testing the end-to-end flow of device-to-cloud (D2C)
telemetry and invoking cloud-to-device (C2D) methods.

The microservice provides a RESTful endpoint to create a simulation (only one),
start, and stop a simulation containing multiple devices. Each simulation is
composed of a set of virtual devices of different types.  The devices are
registered with an IoTHub, send telemetry, update the twin and receive method
calls.

### Features:
1. Get list of device models that can be simulated
2. Create a simulation for a set of customized device models
3. Create a "seed" or "default" simulation with 1 devices per model
4. Only one simulation per deployment can be created
5. Simulations start immediately, unless specified differently
6. Get details of the running simulation
7. Stop existing simulation
8. Start existing simulation
9. Invoke methods on the devices
10. Stateful devices, i.e. devices can simulate long-running flows, state
    machines, etc.

### Components
1. Web service: API for the UI to retrieve information and start/stop
2. Storage: simulation details, and status of the simulation On/Off
3. Simulation actors: background processes sending events and listening for
   method calls

### Dependencies
1. IoT Hub Manager, used to manage devices
   a. Requires an Azure IotHub.
2. Storage Adapter, used to store the simulation status

# Resources

* Web Service API specifications
  * [Device models](API_SPECS_DEVICE_MODELS.md)
  * [Simulations](API_SPECS_SIMULATIONS.md)
  * [Service](API_SPECS_SERVICE.md)
* [Device models](DEVICE_MODELS.md)
* [Contributing to the project](CONTRIBUTING.md)
