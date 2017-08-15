[![Build][build-badge]][build-url]
[![Issues][issues-badge]][issues-url]
[![Gitter][gitter-badge]][gitter-url]

Device Simulation
=================

This service allows to manage a pool of simulated devices, to test the
end-to-end flow of device-to-cloud (D2C) telemetry, invoking
cloud-to-device (C2D) commands, methods, etc.

The microservice provides a RESTful endpoint to set the simulation details,
to start and stop the simulation, to add and remove virtual devices. The
simulation is composed by a set of virtual devices, of different models,
each sending telemetry and replying to method calls.

* [Device Simulation Wiki](https://github.com/Azure/device-simulation-dotnet/wiki)
* [Development setup, scripts and tools](DEVELOPMENT.md)
* [How to contribute to the project](CONTRIBUTING.md)

How to use the microservice
===========================

## Quick demo using the public Docker image

After cloning the repository, follow these steps:

1. Install Docker Compose: https://docs.docker.com/compose/install
1. Create an instance of [Azure IoT Hub](https://azure.microsoft.com/services/iot-hub)
1. Store the "IoT Hub Connection string" in the [env-vars-setup](scripts)
   script. For more information about environment variables, see the
   [development notes](DEVELOPMENT.md#configuration-and-environment-variables).
1. Using an HTTP client like [Postman](https://www.getpostman.com),
   use the
   [RESTful API](https://github.com/Azure/device-simulation-dotnet/wiki/%5BAPI-Specifications%5D-Simulations#create-default-simulation)
   to create a simulation.

By default, Docker Compose will start the service using the sample device
types defined in [sample-volume](scripts/docker/sample-volume):
* to load device models definitions from a different folder, edit the
  [docker-compose.yml](scripts/docker/docker-compose.yml)
* to add your custom simulations, add the JSON and Javascript files into the
  folder and restart the service. See the
  [wiki](https://github.com/Azure/device-simulation-dotnet/wiki)
  for more information about device models and the API.

## Working with Visual Studio

After cloning the repository, follow these steps:

1. Install Docker: https://docs.docker.com/engine/installation
1. Create an instance of [Azure IoT Hub](https://azure.microsoft.com/services/iot-hub)
1. Open the solution in Visual Studio
1. Either in Visual Studio or in your system, define the following environment
   variable:
    1. `PCS_IOTHUB_CONNSTRING` = {your Azure IoT Hub connection string}

   For more information about environment variables, see the
   [development notes](DEVELOPMENT.md#configuration-and-environment-variables).
1. In Visual Studio, start the WebService project
1. In Visual Studio, Start the SimulationAgent project
1. Using an HTTP client like [Postman](https://www.getpostman.com),
   use the
   [RESTful API](https://github.com/Azure/device-simulation-dotnet/wiki/%5BAPI-Specifications%5D-Simulations#create-default-simulation)
   to create a simulation.


[build-badge]: https://img.shields.io/travis/Azure/device-simulation-dotnet.svg
[build-url]: https://travis-ci.org/Azure/device-simulation-dotnet
[issues-badge]: https://img.shields.io/github/issues/azure/device-simulation-dotnet.svg
[issues-url]: https://github.com/azure/device-simulation-dotnet/issues
[gitter-badge]: https://img.shields.io/gitter/room/azure/iot-pcs.js.svg
[gitter-url]: https://gitter.im/azure/iot-pcs
