* [Contribution license Agreement](#contribution-license-agreement)
* [Azure IoT Hub setup](#azure-iot-hub-setup)
* [Development setup](#development-setup)
* [Build and Run](#build-and-run)

Contribution license Agreement
==============================

If you want/plan to contribute, we ask you to sign a
[CLA](https://cla.microsoft.com/) (Contribution license Agreement).
A friendly bot will remind you about it when you submit a pull-request.

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

Some scripts also require .NET Core, where we are migrating the solution.

* [.NET for Windows](https://support.microsoft.com/help/3151802/the-.net-framework-4.6.2-web-installer-for-windows)
* [Mono 5](http://www.mono-project.com/download/beta)
* [.NET Core](https://dotnet.github.io/)

We provide also a [Java version here](https://github.com/Azure/device-simulation-java).

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

Note: the hook requires [Mono 5](http://www.mono-project.com/download/beta).

To setup the included hooks, open a Windows/Linux/MacOS console and execute:

```
cd PROJECT-FOLDER
cd scripts/git
setup
```

If at any point you want to remove the hook, simply delete the file installed
under `.git/hooks`. You can also bypass the pre-commit hook using the
`--no-verify` option.

## Code style

If you use ReSharper or Rider, you can load the code style settings from
the repository, stored in
[device-simulation.sln.DotSettings](device-simulation.sln.DotSettings)

Build and Run
=============

The [scripts](scripts) folder includes some scripts for frequent tasks:

* `build`: compile all the projects and run the tests.
* `compile`: compile all the projects.
* `run`: compile the projects and run the service. This will prompt for
  elevated privileges in Windows to run the web service.
