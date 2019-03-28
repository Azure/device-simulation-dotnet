#!/usr/bin/env bash
# Copyright (c) Microsoft. All rights reserved.

cd /app/

echo "Setting environment variables."
# Running in current shell

. set_env.sh PCS_IOTHUB_CONNSTRING iotHubConnectionString PCS_STORAGEADAPTER_WEBSERVICE_URL storageAdapterWebServiceUrl PCS_WEBUI_AUTH_AAD_TENANT aadTenantId PCS_AUTH_AUDIENCE aadAppId PCS_AAD_SECRET aadAppSecret PCS_AAD_CLIENT_SP_ID aadAppId

echo "Starting service."
cd webservice && dotnet Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.dll
