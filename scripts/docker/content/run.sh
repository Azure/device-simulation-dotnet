#!/usr/bin/env bash

cd /app/

cd webservice && dotnet Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.dll --background & \
    cd simulationagent && dotnet Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.dll && \
    fg
