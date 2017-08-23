[![Build][build-badge]][build-url]
[![Issues][issues-badge]][issues-url]
[![Gitter][gitter-badge]][gitter-url]

# Device Simulation
==========================
## Overview
This service allow management of a pool of simulated devices.  The service helps 
test the end-to-end flow of IoT applications.  The service simulates devices that
send device-to-cloud (D2C) telemetry and allow cloud-to-device (C2D) methods to 
be invoked and executed from a connected IoTHub.

The microservice provides a RESTful endpoint to set the simulation details,
to start and stop the simulation, to add and remove virtual devices. The
simulation is composed by a set of virtual devices, of different models,
each sending telemetry and replying to method calls.  The [Storage adapter microservice](https://github.com/Azure/pcs-storage-adapter-dotnet/README.md) is used by Simulation to store simulated devices configuration.

This microservice contains the following:
* **WebService.csproj** - C# web service exposing REST interface for Simulation
functionality
* **WebService.Test.csproj** - Unit tests for web services functionality
* **Services.csproj** - C# assembly containining business logic for interacting 
with Azure services (IoTHub, DocDb etc.)
* **Services.Test.csproj** - Unit tests for services functionality
* **SimulationAgent.csproj** - C# assembly that acts as the "controller" for 
Simulations (creates simulation, simulated devices, etc.)
* **SimulationAgent.Test.csproj** - Unit tests for services functionality
* **Solution/scripts** - contains build scripts, docker container creation scripts, 
and scripts for running the microservice from the command line

# How to use the microservice
## Quickstart - Running the service with Docker

1. Install Docker Compose: https://docs.docker.com/compose/install
1. Follow the [Storage quickstart instructions](https://github.com/Azure/pcs-storage-adapter-dotnet/README.md) for setting up the Storage Adapter microservice. Storage is used by Simulation to store simulated devices configuration in a DocDb instance.
1. Create an instance of [Azure IoT Hub](https://azure.microsoft.com/services/iot-hub)
1. Find your IotHub connection string.  See [Understanding IoTHub Connection Strings](https://blogs.msdn.microsoft.com/iotdev/2017/05/09/understand-different-connection-strings-in-azure-iot-hub/) if you need help finding it.
1. Store the "IoT Hub Connection string" in the [env-vars-setup](scripts)
   script, then run the script.
1. Run the Simulation service using docker compose [docker-compose up](scripts)
1. Use an HTTP client such as [Postman](https://www.getpostman.com),
   to exercise the 
   [RESTful API](https://github.com/Azure/device-simulation-dotnet/wiki/%5BAPI-Specifications%5D-Simulations#create-default-simulation)
   to create a simulation.

## Running the service in an IDE
### Running the service with Visual Studio
1. Install any edition of [Visual Studio 2017](https://www.visualstudio.com/downloads/). When installing check ".NET Core" workload. 	
    a. If you already have Visual Studio installed, then ensure you have [.NET Core Tools for Visual Studio 2017](https://www.microsoft.com/net/core#windowsvs2017) installed.
1. Create an instance of [Azure IoT Hub](https://azure.microsoft.com/services/iot-hub)
1. Open the solution in Visual Studio
1. Either in the project properties Visual Studio or in your system, define the following required environment variables.  For the both the WebService and 
SimulationAgent projects:
    1. `PCS_IOTHUB_CONNSTRING` = {your Azure IoT Hub connection string}
1. For just the SimulationAgent project also create the following:
    1. `PCS_STORAGEADAPTER_WEBSERVICE_URL` = {http://localhost:9022/v1}
1. In Visual Studio, start the WebService project
1. In Visual Studio, Start the SimulationAgent project
1. Using an HTTP client like [Postman](https://www.getpostman.com),
   use the
   [RESTful API](https://github.com/Azure/device-simulation-dotnet/wiki/%5BAPI-Specifications%5D-Simulations#create-default-simulation)
   to create a simulation.

### Running the service with IntelliJ Rider
1. Open the solution using the `device-simulation.sln` file.
1. When the solution is loaded, got to `Run -> Edit Configurations` and
   create a new `.NET Project` configuration.
1. In the configuration select the WebService project
1. Add a new environment variable with name
   `PCS_IOTHUB_CONNSTRING` storing your Azure IoT Hub connection string.
1. Save the settings and run the configuration just created, from the IDE
   toolbar.
1. You should see the service bootstrap messages in IntelliJ Run window,
   with details such as the URL where the web service is running, plus
   the service logs.

## Build and Run from the command line
The [scripts](scripts) folder contains scripts for many frequent tasks:

* `build`: compile all the projects and run the tests.
* `compile`: compile all the projects.
* `run`: compile the projects and run the service. This will prompt for
  elevated privileges in Windows to run the web service.

### Sandbox

The scripts assume that you configured your development environment,
with tools like .NET Core and Docker. You can avoid installing .NET Core,
and install only Docker, and use the command line parameter `--in-sandbox`
(or the short form `-s`), for example:

* `build --in-sandbox`: executes the build task inside of a Docker
    container (short form `build -s`).
* `compile --in-sandbox`: executes the compilation task inside of a Docker
    container (short form `compile -s`).
* `run --in-sandbox`: starts the service inside of a Docker container
    (short form `run -s`).

The Docker images used for the sandbox are hosted on Docker Hub
[here](https://hub.docker.com/r/azureiotpcs/code-builder-dotnet).


## Updating the Docker image
=========================================

The `scripts` folder includes a [docker](scripts/docker) subfolder with the files
required to package the service into a Docker image:

* `Dockerfile`: docker images specifications
* `build`: build a Docker container and store the image in the local registry
* `run`: run the Docker container from the image stored in the local registry
* `content`: a folder with files copied into the image, including the entry point script

You can also start Device Simulation and its dependencies in one simple step,
using Docker Compose with the
[docker-compose.yml](scripts/docker/docker-compose.yml) file in the project:

```
cd scripts/docker
docker-compose up
```

The Docker compose configuration requires the IoTHub and StorageAdapter web serviceURL environment variables, described previously.

## Configuration and Environment variables
The service configuration is stored using ASP.NET Core configuration
adapters, in [appsettings.ini](WebService/appsettings.ini). The INI
format allows to store values in a readable format, with comments.
The application also supports inserting environment variables, such as
credentials and networking details.

The configuration file in the repository references some environment
variables that need to created at least once. Depending on your OS and
the IDE, there are several ways to manage environment variables:

* For Windows users, the [env-vars-setup.cmd](scripts/env-vars-setup.cmd)
  script needs to be prepared and executed just once. When executed, the
  settings will persist across terminal sessions and reboots.
* For Linux and OSX environments, the [env-vars-setup](scripts/env-vars-setup)
  script needs to be executed every time a new console is opened.
  Depending on the OS and terminal, there are ways to persist values
  globally, for more information these pages should help:
  * https://stackoverflow.com/questions/13046624/how-to-permanently-export-a-variable-in-linux
  * https://stackoverflow.com/questions/135688/setting-environment-variables-in-os-x
  * https://help.ubuntu.com/community/EnvironmentVariables
* Visual Studio: env. vars can be set also from Visual Studio, under Project
  Properties, in the left pane select "Configuration Properties" and
  "Environment", to get to a section where you can add multiple variables.
* IntelliJ Rider: env. vars can be set in each Run Configuration, similarly to
  IntelliJ IDEA 
  (https://www.jetbrains.com/help/idea/run-debug-configuration-application.html)

## Contributing to the solution
Please follow our [contribution guildelines](CONTRIBUTING.md) and the 
following code style conventions.  We recommend using the Git setup defined below.

### Code style

If you use ReSharper or Rider, you can load the code style settings from
the repository, stored in
[device-simulation.sln.DotSettings](device-simulation.sln.DotSettings)

Some quick notes about the project code style:

1. Where reasonable, lines length is limited to 80 chars max, to help code
   reviews and command line editors.
2. Code blocks indentation with 4 spaces. The tab char should be avoided.
3. Text files use Unix end of line format (LF).
4. Dependency Injection is managed with [Autofac](https://autofac.org).
5. Web service APIs fields are CamelCased (except for metadata).

### Git setup

The project includes a Git hook, to automate some checks before accepting a
code change. You can run the tests manually, or let the CI platform to run
the tests. We use the following Git hook to automatically run all the tests
before sending code changes to GitHub and speed up the development workflow.

If at any point you want to remove the hook, simply delete the file installed
under `.git/hooks`. You can also bypass the pre-commit hook using the
`--no-verify` option.

##### Pre-commit hook with sandbox

To setup the included hooks, open a Windows/Linux/MacOS console and execute:

```
cd PROJECT-FOLDER
cd scripts/git
setup --with-sandbox
```

With this configuration, when checking in files, git will verify that the
application passes all the tests, running the build and the tests inside
a Docker container configured with all the development requirements.

##### Pre-commit hook without sandbox

Note: the hook without sandbox requires [.NET Core](https://dotnet.github.io)
in the system PATH.

To setup the included hooks, open a Windows/Linux/MacOS console and execute:

```
cd PROJECT-FOLDER
cd scripts/git
setup --no-sandbox
```

With this configuration, when checking in files, git will verify that the
application passes all the tests, running the build and the tests in your
workstation, using the tools installed in your OS.

## Troubleshooting

## Feedback
Please enter issues, bugs, or suggestions as GitHub Issues here: https://github.com/Azure/device-simulation-dotnet/issues.

## Related

* [build-badge](https://img.shields.io/travis/Azure/device-simulation-dotnet.svg)
* [build-url](https://travis-ci.org/Azure/device-simulation-dotnet)
* [issues-badge](https://img.shields.io/github/issues/azure/device-simulation-dotnet.svg)
* [issues-url](https://github.com/azure/device-simulation-dotnet/issues)
* [gitter-badge](https://img.shields.io/gitter/room/azure/iot-pcs.js.svg)
* [gitter-url](https://gitter.im/azure/iot-pcs)

