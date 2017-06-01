#!/usr/bin/env bash

cd /app/

cd webservice && mono Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.exe --background & \
    cd simulationagent && mono Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.exe && \
    fg
