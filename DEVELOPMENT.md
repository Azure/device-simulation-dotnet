* [Build, Run locally and with Docker](#build-run-locally-and-with-docker)
* [Service configuration](#configuration)
* [Azure IoT Hub setup](#azure-iot-hub-setup)
* [Development setup](#development-setup)

Build, Run locally and with Docker
==================================

The [scripts](scripts) folder includes some scripts for frequent tasks:

* `build`: compile all the projects and run the tests.
* `compile`: compile all the projects.
* `run`: compile the projects and run the service. This will prompt for
  elevated privileges in Windows to run the web service.
* `docker/build`: build a Docker container and store the image in the local
  registry.
* `docker/run`: run the Docker container from the image stored in the local
  registry.

### Sandbox

Most of the scripts assume you have configured your development environment,
with tools like .NET Core and Docker. You can avoid installing .NET Core,
and install only Docker, and use the scripts with `-in-sandbox` suffix,
for example:

* `build-in-sandbox`: like `build` but executes the task inside of a Docker
   container.
* `compile-in-sandbox`: like `compile` but executes the task inside of a
   Docker container.
* `run-in-sandbox`: like `run` but executes the task inside of a Docker
   container.

The Docker images used for the sandbox is hosted on Docker Hub
[here](https://hub.docker.com/r/azureiotpcs/code-builder-dotnet).

Configuration
=============

The service configuration is stored using ASP.NET Core configuration
adapters, in `appsettings.ini`. The INI format allows to store values in a
readable format, with comments. The application also supports inserting
environment variables, such as credentials and networking details.

Azure IoT Hub setup
===================

At some point you will probably want to setup your Azure IoT Hub, for
development and integration tests.

The project includes some Bash scripts to help you with this setup:

* Create new IoT Hub: `./scripts/iothub/create-hub.sh`
* List existing hubs: `./scripts/iothub/list-hubs.sh`
* Show IoT Hub details (e.g. keys): `./scripts/iothub/show-hub.sh`

and in case you had multiple Azure subscriptions:

* Show subscriptions list: `./scripts/iothub/list-subscriptions.sh`
* Change current subscription: `./scripts/iothub/select-subscription.sh`

Development setup
=================

## .NET setup

The project workflow is managed via .NET Core 1.0.4.
We recommend to install .NET Core in your environment, so that you can
run all the scripts and ensure that your IDE works as expected.

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
