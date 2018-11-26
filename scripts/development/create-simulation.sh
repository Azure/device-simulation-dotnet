#!/usr/bin/env bash

set -e

SIM_ID="$1"
DEVICE_COUNT="$2"

if [ -z "$SIM_ID" ]; then
    echo "Simulation ID not specified. Usage: <script> <SIM_ID> <COUNT>"
    exit 1
fi

if [ -z "$DEVICE_COUNT" ]; then
    echo "Simulation ID not specified. Usage: <script> <SIM_ID> <COUNT>"
    exit 1
fi

CONN_STRING="default"
SIM_ENABLED="true"
DELETE_DEVICES="true"

JSON='{'
JSON=$JSON'"Name": "-",'
JSON=$JSON'"Description": "-",'
#JSON=$JSON'"StartTime": "...",'
#JSON=$JSON'"EndTime": "...",'
JSON=$JSON'"Enabled": '$SIM_ENABLED','
JSON=$JSON'"DeleteDevicesWhenSimulationEnds": '$DELETE_DEVICES','
JSON=$JSON'"IoTHubs": [{ "ConnectionString": "'$CONN_STRING'" }],'
JSON=$JSON'"DeviceModels": [{"Id": "truck-01","Count": '$DEVICE_COUNT'}],'
JSON=$JSON'"RateLimits": {'
JSON=$JSON'"RegistryOperationsPerMinute": 100,'
JSON=$JSON'"TwinReadsPerSecond": 10,'
JSON=$JSON'"TwinWritesPerSecond": 10,'
JSON=$JSON'"ConnectionsPerSecond": 120,'
JSON=$JSON'"DeviceMessagesPerSecond": 120'
JSON=$JSON'}'
JSON=$JSON'}'

curl -v -X PUT 'http://localhost:9003/v1/simulations/'$SIM_ID \
    -H 'Content-Type: application/json' \
    -d "${JSON}"