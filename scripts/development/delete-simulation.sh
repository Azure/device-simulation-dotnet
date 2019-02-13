#!/usr/bin/env bash

set -e

SIM_ID="$1"

if [ -z "$SIM_ID" ]; then
    echo "Simulation ID not specified. Usage: <script> <SIM_ID>"
    exit 1
fi

curl -v -X DELETE 'http://localhost:9003/v1/simulations/'$SIM_ID