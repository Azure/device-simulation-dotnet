#!/usr/bin/env bash
# Copyright (c) Microsoft. All rights reserved.

cd /app/

# Running in current shell
. set_env.sh PCS_IOTHUB_CONNSTRING iotHubConnectionString PCS_STORAGEADAPTER_WEBSERVICE_URL storageAdapterWebServiceUrl

cd webservice && dotnet Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.dll
