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
with tools like MSBuild, Nuget, .NET Core, Mono and Docker. You can avoid
installing all of these tools, and install only Docker, and use the scripts
with `-in-sandbox` suffix:

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

The service configuration is stored using Akka's
[HOCON](http://getakka.net/docs/concepts/configuration)
format in `application.conf`.

The HOCON format is a human readable format, very close to JSON, with some
useful features:

* Ability to write comments
* Support for substitutions, e.g. referencing environment variables
* Supports JSON notation

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

The project workflow is managed via .NET Framework 4.6.2+ and Mono 5.x.
We recommend to install Mono also in Windows, where Mono is used for the
Git pre-commit hook.

On the other hand you can install just Docker and rely on the builder
sandbox if you don't want to install all these dependencies.

Some scripts also require .NET Core, where we are migrating the solution.

* [.NET for Windows](https://support.microsoft.com/help/3151802/the-.net-framework-4.6.2-web-installer-for-windows)
* [Mono 5](http://www.mono-project.com/download)
* [.NET Core](https://dotnet.github.io/)

We provide also a
[Java version](https://github.com/Azure/device-simulation-java)
of this project and other Azure IoT PCS components.

## IDE

Here are some IDE that you can use to work on Azure IoT PCS:

* [Visual Studio](https://www.visualstudio.com/)
* [IntelliJ Rider](https://www.jetbrains.com/rider)
* [Visual Studio Code](https://code.visualstudio.com/)
* [Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac)

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

#### Pre-commit hook without sandbox

Note: the hook requires [Mono 5](http://www.mono-project.com/download),
Nuget and MSBuild in the system PATH.

To setup the included hooks, open a Windows/Linux/MacOS console and execute:

```
cd PROJECT-FOLDER
cd scripts/git
setup --no-sandbox
```

## Code style

If you use ReSharper or Rider, you can load the code style settings from
the repository, stored in
[solution.sln.DotSettings](solution.sln.DotSettings)

Some quick notes about the project code style:

1. Where reasonable, lines length is limited to 80 chars max, to help code
   reviews and command line editors.
2. Code blocks indentation with 4 spaces. The tab char should be avoided.
3. Text files use Unix end of line format (LF).
4. Dependency Injection is managed with
   [Unity](https://msdn.microsoft.com/library/dn223671.aspx).
5. Web service APIs fields are CamelCased (except for metadata).
