#!/usr/bin/env bash
# Copyright (c) Microsoft. All rights reserved.

cd /app/

cd webservice && dotnet Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.dll
