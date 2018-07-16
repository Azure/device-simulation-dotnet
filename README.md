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

Each **device model** defines a distinct behavior (e.g. the data generated
by virtual sensors), frequency and format of the telemetry, network protocols,
and which methods are supported.

<img src="https://github.com/Azure/device-simulation-dotnet/blob/master/docs/overview.png">

Dependencies
============

The service depends on:

* [Azure IoT Hub][iothub-url] used to store virtual devices, to send
  telemetry and receive method calls
* [Storage adapter microservice][storageadapter-url] used to store the
  simulation details
* Configuration settings to connect to IoT Hub and the Storage Adapter.
  These settings are stored in environment variables, which are referenced
  by the service configuration. See below for more information.

How to use the microservice
===========================

## Quickstart - Running the service with Docker

1. Create an instance of [Azure IoT Hub][iothub-url]
1. Follow the [Storage quickstart instructions][storageadapter-url]
   for setting up the storage used by Storage Adapter microservice.
1. Find your Iot Hub connection string. See
   [Understanding IoTHub Connection Strings][iothubconnstring-url] if you
   need help finding it.
1. Store the "IoT Hub Connection string" in the [env-vars-setup](scripts)
   script, then run the script. When using MacOS/Linux, the environment
   variables need to be set in the same terminal session where Docker is
   executed, every time a new session is created.
1. [Install Docker Compose][docker-compose-install-url]
1. Start the Simulation service using docker compose:
   ```
   cd scripts
   cd docker
   docker-compose up
   ```
1. Use an HTTP client such as [Postman][postman-url], to exercise the
   [RESTful API][wiki-createsim-url] to create a simulation.

## Running the service locally, e.g. for development tasks

The service can be started from any C# IDE and from the command line.
The only difference you might notice is how environment variables
are configured. See the [Configuration and Environment variables](#configuration-and-environment-variables) documentation below for more information.

1. [Install .NET Core 2.x][dotnet-install]
1. Install any recent edition of Visual Studio (Windows/MacOS) or Visual
   Studio Code (Windows/MacOS/Linux).
1. Create an instance of [Azure IoT Hub][iothub-url].
1. Follow the [Storage quickstart instructions][storageadapter-url] for setting
   up and running the Storage Adapter microservice, which should be listening
   at http://127.0.0.1:9022
1. Open the solution in Visual Studio or VS Code
1. Define the following environment variables. See [Configuration and Environment variables](#configuration-and-environment-variables) for detailed information for setting these for your enviroment.
   * `PCS_IOTHUB_CONNSTRING` = {your Azure IoT Hub connection string}
1. Start the WebService project (e.g. press F5)
1. Test if the service is running, opening http://127.0.0.1:9003/v1/status
1. Using an HTTP client like [Postman][postman-url],
   use the [RESTful API][wiki-createsim-url] to create a simulation.

## Project Structure

The solution contains the following projects and folders:

* **WebService**: ASP.NET Web API exposing a RESTful API for Simulation
  functionality, e.g. start, stop, add devices, etc. This is also the
  service entry point, starting all the main threads.
* **SimulationAgent**: Library containing the logic that controls the
  simulation. The logic is started by the WebService assembly.
* **Services**: Library containing common business logic for interacting with
  Azure IoT Hub, Storage Adapter, and to run the simulation code.
* **WebService.Test**: Unit tests for the ASP.NET Web API project.
* **SimulationAgent.Test**: Unit tests for the SimulationAgent project.
* **Services.Test**: Unit tests for the Services library.
* **scripts**: a folder containing scripts for the command line console,
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

The service configuration is accessed via ASP.NET Core configuration
adapters, and stored in [appsettings.ini](WebService/appsettings.ini).
The INI format allows to store values in a readable format, with comments.

The configuration also supports references to environment variables, e.g. to
import credentials and network details. Environment variables are not
mandatory though, you can for example edit appsettings.ini and write
credentials directly in the file. Just be careful not sharing the changes,
e.g. sending a Pull Request or checking in the changes in git.

The configuration file in the repository references some environment
variables that need to be defined. Depending on the OS and the IDE used,
there are several ways to manage environment variables.

1. If you're using Visual Studio or Visual Studio for Mac, the environment
   variables are loaded from the project settings. Right click on WebService,
   and select Options/Properties, and find the section with the list of env
   vars. See [WebService/Properties/launchSettings.json](WebService/Properties/launchSettings.json).
1. Visual Studio Code loads the environment variables from
   [.vscode/launch.json](.vscode/launch.json)
1. When running the service **with Docker** or **from the command line**, the
   application will inherit environment variables values from the system. 
   * [This page][windows-envvars-howto-url] describes how to setup env vars
     in Windows. We suggest to edit and execute once the
     [env-vars-setup.cmd](scripts/env-vars-setup.cmd) script included in the
     repository. The settings will persist across terminal sessions and reboots.
   * For Linux and MacOS, we suggest to edit and execute
     [env-vars-setup](scripts/env-vars-setup) each time, before starting the
     service. Depending on OS and terminal, there are ways to persist values
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

Please enter issues, bugs, or suggestions as GitHub Issues here: 
https://github.com/Azure/device-simulation-dotnet/issues.





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
[dotnet-install]: https://www.microsoft.com/net/learn/get-started
[windows-envvars-howto-url]: https://superuser.com/questions/949560/how-do-i-set-system-environment-variables-in-windows-10
[docker-compose-install-url]: https://docs.docker.com/compose/install

[device-model-wiki]: https://github.com/Azure/device-simulation-dotnet/wiki/Device-Models
[simulation-service-api-spec-wiki]: https://github.com/Azure/device-simulation-dotnet/wiki/%5BAPI-Specifications%5D-Service
[device-models-api-spec-wiki]: https://github.com/Azure/device-simulation-dotnet/wiki/%5BAPI-Specifications%5D-Device-Models
[simulation-api-spec-wiki]: https://github.com/Azure/device-simulation-dotnet/wiki/%5BAPI-Specifications%5D-Simulations
