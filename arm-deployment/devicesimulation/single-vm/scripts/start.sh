#!/usr/bin/env bash
# Copyright (c) Microsoft. All rights reserved.
# Note: Windows Bash doesn't support shebang extra params
set -e

# Network settings
sysctl -w net.ipv4.ip_local_port_range="1024 65535"
sysctl -w net.ipv4.tcp_max_syn_backlog=4096
sysctl -w net.core.somaxconn=1024

cd /app

source "env-vars"

COL_NO="\033[0m" # no color
COL_WARN="\033[1;33m" # light yellow
COL_ERR="\033[1;31m" # light red

APP_PATH="/app"
WEBUICONFIG="${APP_PATH}/webui-config.js"
WEBUICONFIG_SAFE="${APP_PATH}/webui-config.js.safe"
WEBUICONFIG_UNSAFE="${APP_PATH}/webui-config.js.unsafe"

rm -f ${WEBUICONFIG}
cp -p ${WEBUICONFIG_SAFE} ${WEBUICONFIG}

if [[ "$1" == "--unsafe" || "$2" == "--unsafe" ]]; then
  echo -e "${COL_ERR}WARNING! Starting services in UNSAFE mode!${COL_NO}"
  # Disable Auth
  export PCS_AUTH_REQUIRED="false"
  # Allow cross-origin requests from anywhere
  export PCS_CORS_WHITELIST="{ 'origins': ['*'], 'methods': ['*'], 'headers': ['*'] }"

  rm -f ${WEBUICONFIG}
  cp -p ${WEBUICONFIG_UNSAFE} ${WEBUICONFIG}
fi

list=$(docker ps -aq)
if [ -n "$list" ]; then
    echo -e "${COL_WARN}Stopping existing services...${COL_NO}"
    docker rm -f $list
fi

if [[ "$1" == "--debug" || "$2" == "--debug" ]]; then
  echo -e "${COL_WARN}Starting services... Press CTRL+C to exit${COL_NO}"
  docker-compose up
  exit 0
fi

echo -e "${COL_WARN}Starting services...${COL_NO}"
nohup docker-compose up > /dev/null 2>&1&

ISUP=$(curl -ks https://localhost/ | grep -i "html" | wc -l)
while [[ "$ISUP" == "0" ]]; do
  echo "Waiting for web site to start..."
  sleep 3
  ISUP=$(curl -ks https://localhost/ | grep -i "html" | wc -l)
done
