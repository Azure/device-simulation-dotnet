[![Build][build-badge]][build-url]
[![Issues][issues-badge]][issues-url]
[![Gitter][gitter-badge]][gitter-url]

Device Simulation Overview
==========================

This service allows management of a pool of simulated devices. The service
helps test the end-to-end flow of IoT applications. The service simulates
devices that send device-to-cloud (D2C) telemetry and allows cloud-to-device
(C2D) methods to be invoked by application connected IoT Hub.

The service provides a RESTful endpoint to configure the simulation details,
to start and stop the simulation, to add and remove virtual devices. The
simulation is composed by a set of virtual devices, of different **models**,
each sending telemetry and replying to method calls.

Each **device model** defines a distinct behavior, like the data generated
by virtual sensors, frequency and format of the telemetry, network protocols,
which methods are supported.

<img src="https://github.com/Azure/device-simulation-dotnet/blob/master/docs/overview.png">

Dependencies
============

The simulation service depends on:

* [Azure IoT Hub][iothub-url] used to store virtual devices, to send
  telemetry and receive method calls
* [Storage adapter microservice][storageadapter-url] used to store the
  simulation details
* Configuration settings to connect to IoT Hub and the Storage Adapter.
  These settings are stored in environment variables, which are referenced
  by the service configuration.

How to use the microservice
===========================

## Quickstart - Running the service with Docker

1. Create an instance of [Azure IoT Hub][iothub-url]
1. Follow the [Storage quickstart instructions][storageadapter-url]
   for setting up the Storage Adapter microservice storage.
1. Find your Iot Hub connection string. See
   [Understanding IoTHub Connection Strings][iothubconnstring-url] if you
   need help finding it.
1. Store the "IoT Hub Connection string" in the [env-vars-setup](scripts)
   script, then run the script. In MacOS/Linux the environment variables
   need to be set in the same session where you run Docker Compose,
   every time a new session is created.
1. [Install Docker Compose][docker-compose-install-url]
1. Start the Simulation service using docker compose:
   ```
   cd scripts
   cd docker
   docker-compose up
   ```
1. Use an HTTP client such as [Postman][postman-url], to exercise the
   [RESTful API][wiki-createsim-url] to create a simulation.

## Running the service with Visual Studio

1. Install any edition of [Visual Studio 2017][vs-install-url] or Visual
   Studio for Mac. When installing check ".NET Core" workload. If you
   already have Visual Studio installed, then ensure you have
   [.NET Core Tools for Visual Studio 2017][dotnetcore-tools-url]
   installed (Windows only).
1. Create an instance of [Azure IoT Hub][iothub-url].
1. Follow the [Storage quickstart instructions][storageadapter-url]
   for setting up and running the Storage Adapter microservice.
1. Open the solution in Visual Studio
1. Edit the WebService and SimulationAgent project properties, and
   define the following required environment variables. In Windows
   you can also set these [in your system][windows-envvars-howto-url].
   1. `PCS_IOTHUB_CONNSTRING` = {your Azure IoT Hub connection string}
   1. `PCS_STORAGEADAPTER_WEBSERVICE_URL` = http://localhost:9022/v1
1. In Visual Studio, start the WebService project
1. In Visual Studio, start the SimulationAgent project
1. Using an HTTP client like [Postman][postman-url],
   use the [RESTful API][wiki-createsim-url] to create a simulation.

## Project Structure

The solution contains the following projects and folders:

* **WebService**: ASP.NET Web API exposing a RESTful API for Simulation
  functionality, e.g. start, stop, add devices, etc.
* **SimulationAgent**: Console application controlling the simulation
  execution and managing the IoT Hub connections.
* **Services**: Library containing common business logic for interacting with
  Azure IoT Hub, Storage Adapter, and to run the simulation code.
* **WebService.Test**: Unit tests for the ASP.NET Web API project.
* **SimulationAgent.Test**: Unit tests for the SimulationAgent project.
* **Services.Test**: Unit tests for the Services library.
* **scripts**: a folder containing scripts from the command line console,
  to build and run the solution, and other frequent tasks.

## Build and Run from the command line

The [scripts](scripts) folder contains scripts for many frequent tasks:

* `build`: compile all the projects and run the tests.
* `compile`: compile all the projects.
* `run`: compile the projects and run the service. This will prompt for
  elevated privileges in Windows to run the web service.

## Building a customized Docker image

The `scripts` folder includes a [docker](scripts/docker) subfolder with the
scripts required to package the service into a Docker image:

* `Dockerfile`: Docker image specifications
* `build`: build a Docker image and store the image in the local registry
* `run`: run the Docker container from the image stored in the local registry
* `content`: a folder with files copied into the image, including the entry
  point script

You can also start Device Simulation and its dependencies in one simple step,
using Docker Compose with the
[docker-compose.yml](scripts/docker/docker-compose.yml) file in the project:

```
cd scripts
cd docker
docker-compose up
```

The Docker compose configuration requires the IoTHub and StorageAdapter web
service URL environment variables, described previously.

## Configuration and Environment variables

The service configuration is stored using ASP.NET Core configuration
adapters, in [appsettings.ini](WebService/appsettings.ini) and
[appsettings.ini](SimulationAgent/appsettings.ini). The INI format allows to
store values in a readable format, with comments. The application also
supports references to environment variables, which is used to import
credentials and networking details.

The configuration files in the repository reference some environment
variables that need to be created at least once. Depending on your OS and
the IDE, there are several ways to manage environment variables:

* Windows: the variables can be set [in the system][windows-envvars-howto-url]
  as a one time only task. The
  [env-vars-setup.cmd](scripts/env-vars-setup.cmd) script included needs to
  be prepared and executed just once. The settings will persist across
  terminal sessions and reboots.
* Visual Studio: the variables can be set in the projects's settings, both
  WebService and SimulationAgent, under Project Propertie -> Configuration
  Properties -> Environment
* For Linux and OSX environments, the [env-vars-setup](scripts/env-vars-setup)
  script needs to be executed every time a new console is opened.
  Depending on the OS and terminal, there are ways to persist values
  globally, for more information these pages should help:
  * https://stackoverflow.com/questions/13046624/how-to-permanently-export-a-variable-in-linux
  * https://stackoverflow.com/questions/135688/setting-environment-variables-in-os-x
  * https://help.ubuntu.com/community/EnvironmentVariables

Other resources
===============

* [Device Models specification][device-model-wiki]
* [Simulation service API specs][simulation-service-api-spec-wiki]
* [Device Models API specs][device-models-api-spec-wiki]
* [Simulations API specs][simulation-api-spec-wiki]

Contributing to the solution
============================

Please follow our [contribution guidelines](docs/CONTRIBUTING.md).  We love PRs too.

Troubleshooting
===============

{TODO}

Feedback
==========

Please enter issues, bugs, or suggestions as GitHub Issues here: https://github.com/Azure/device-simulation-dotnet/issues.





[build-badge]: https://img.shields.io/travis/Azure/device-simulation-dotnet.svg
[build-url]: https://travis-ci.org/Azure/device-simulation-dotnet
[issues-badge]: https://img.shields.io/github/issues/azure/device-simulation-dotnet.svg
[issues-url]: https://github.com/azure/device-simulation-dotnet/issues
[gitter-badge]: https://img.shields.io/gitter/room/azure/iot-solutions.js.svg
[gitter-url]: https://gitter.im/azure/iot-solutions

[iothub-url]: https://azure.microsoft.com/services/iot-hub
[storageadapter-url]: https://github.com/Azure/pcs-storage-adapter-dotnet/blob/master/README.md
[iothubconnstring-url]: https://blogs.msdn.microsoft.com/iotdev/2017/05/09/understand-different-connection-strings-in-azure-iot-hub
[postman-url]: https://www.getpostman.com
[wiki-createsim-url]: https://github.com/Azure/device-simulation-dotnet/wiki/%5BAPI-Specifications%5D-Simulations#create-default-simulation
[vs-install-url]: https://www.visualstudio.com/downloads
[dotnetcore-tools-url]: https://www.microsoft.com/net/core#windowsvs2017
[windows-envvars-howto-url]: https://superuser.com/questions/949560/how-do-i-set-system-environment-variables-in-windows-10
[docker-compose-install-url]: https://docs.docker.com/compose/install

[device-model-wiki]: https://github.com/Azure/device-simulation-dotnet/wiki/Device-Models
[simulation-service-api-spec-wiki]: https://github.com/Azure/device-simulation-dotnet/wiki/%5BAPI-Specifications%5D-Service
[device-models-api-spec-wiki]: https://github.com/Azure/device-simulation-dotnet/wiki/%5BAPI-Specifications%5D-Device-Models
[simulation-api-spec-wiki]: https://github.com/Azure/device-simulation-dotnet/wiki/%5BAPI-Specifications%5D-Simulations
