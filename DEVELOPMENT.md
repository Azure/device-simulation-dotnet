* [Build, Run locally and with Docker](#build-run-locally-and-with-docker)
* [Service configuration](#configuration)
* [Azure IoT Hub setup](#azure-iot-hub-setup)
* [Development setup](#development-setup)

Run and Debug with Visual Studio
================================

Visual Studio lets you quickly open the application without using a command
prompt, without configuring anything outside of the IDE.

Steps using Visual Studio 2017:

1. Open the solution using the `device-simulation.sln` file.
1. When the solution is loaded, right click on the `WebService` project,
   select `Properties` and go to the `Debug` section.
1. Add a new environment variable with name
   `PCS_DEVICESIMULATION_WEBSERVICE_PORT` and value `9003`.
1. Add a new environment variable with name
   `PCS_IOTHUBMANAGER_WEBSERVICE_URL` and value `http://127.0.0.1:9002/v1`.
1. In the same section set the `App URL` to
   `http://localhost:9003/v1/status`
1. Right click on the "WebService" project and "Set as StartUp Project".
1. The toolbar should switch automatically to "WebService" and "IIS Express",
   otherwise change these manually.
1. Press F5, or the Run icon. VisualStudio should open your browser showing
   the service status in JSON format.

Dependencies
============

The Device Simulation service depends on
[IoT Hub Manager](https://github.com/Azure/iothub-manager-dotnet).
In order to run simulations, you need to start the IoT Hub manager:

Bash console:
```
docker run -it -p 9002:9002 -e PCS_IOTHUBMANAGER_WEBSERVICE_PORT=9002 \
    -e PCS_IOTHUB_CONN_STRING=$PCS_IOTHUB_CONN_STRING \
    azureiotpcs/iothubmanager-dotnet:0.1-SNAPSHOT
```

Windows console:
```
docker run -it -p 9002:9002 -e PCS_IOTHUBMANAGER_WEBSERVICE_PORT=9002 ^
    -e PCS_IOTHUB_CONN_STRING=%PCS_IOTHUB_CONN_STRING% ^
    azureiotpcs/iothubmanager-dotnet:0.1-SNAPSHOT
```

Run and Debug with IntelliJ Rider
=================================

1. Open the solution using the `device-simulation.sln` file.
1. When the solution is loaded, got to `Run -> Edit Configurations` and
   create a new `.NET Project` configuration.
1. In the configuration select the WebService project
1. Add a new environment variable with name
   `PCS_DEVICESIMULATION_WEBSERVICE_PORT` and value `9003`.
1. Add a new environment variable with name
   `PCS_IOTHUBMANAGER_WEBSERVICE_URL` and value `http://127.0.0.1:9002/v1`.
1. Save the settings and run the configuration just created, from the IDE
   toolbar.
1. You should see the service bootstrap messages in IntelliJ Run window,
   with details such as the URL where the web service is running, plus
   the service logs.

Build and Run from the command line
===================================

The [scripts](scripts) folder contains some scripts for frequent tasks:

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

Package the application to a Docker image
=========================================

The `scripts` folder includes a [docker](scripts/docker) subfolder with the files
required to package the service into a Docker image:

* `Dockerfile`: docker images specifications
* `build`: build a Docker container and store the image in the local registry
* `run`: run the Docker container from the image stored in the local registry
* `content`: a folder with files copied into the image, including the entry point script

You can also start Device Simulation and its dependency IoT Hub Manager
in one simple step, using Docker Compose with the
[docker-compose.yml](scripts/docker/docker-compose.yml) file in the project:

```
cd scripts/docker
docker-compose up
```

The Docker compose configuration requires some environment variables,
similarly to the steps described above.

Configuration
=============

The service configuration is stored using ASP.NET Core configuration
adapters, in `appsettings.ini`. The INI format allows to store values in a
readable format, with comments. The application also supports inserting
environment variables, such as credentials and networking details.

Development setup
=================

## .NET setup

The project workflow is managed via .NET Core 1.0.4, which you need
to install in your environment, so that you can run all the scripts
and ensure that your IDE works as expected.

* [.NET Core](https://dotnet.github.io)

We provide also a
[Java version](https://github.com/Azure/device-simulation-java)
of this project and other Azure IoT PCS components.

## IDE

Here are some IDE that you can use to work on Azure IoT PCS:

* [Visual Studio](https://www.visualstudio.com)
* [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac)
* [IntelliJ Rider](https://www.jetbrains.com/rider)
* [Visual Studio Code](https://code.visualstudio.com)

## Git setup

The project includes a Git hook, to automate some checks before accepting a
code change. You can run the tests manually, or let the CI platform to run
the tests. We use the following Git hook to automatically run all the tests
before sending code changes to GitHub and speed up the development workflow.

If at any point you want to remove the hook, simply delete the file installed
under `.git/hooks`. You can also bypass the pre-commit hook using the
`--no-verify` option.

#### Pre-commit hook with sandbox

To setup the included hooks, open a Windows/Linux/MacOS console and execute:

```
cd PROJECT-FOLDER
cd scripts/git
setup --with-sandbox
```

With this configuration, when checking in files, git will verify that the
application passes all the tests, running the build and the tests inside
a Docker container configured with all the development requirements.

#### Pre-commit hook without sandbox

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

## Code style

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
