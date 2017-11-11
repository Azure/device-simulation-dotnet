#!/usr/bin/env bash
# Copyright (c) Microsoft. All rights reserved.

cd /app/

cd webservice && dotnet Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.dll & \
    cd simulationagent && dotnet Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.dll && \
    fg
