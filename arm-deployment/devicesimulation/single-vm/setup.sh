#!/usr/bin/env bash
# Copyright (c) Microsoft. All rights reserved.
# Note: Windows Bash doesn't support shebang extra params

# Solution: devicesimulation

# Note: this script is invoked by setup-wrapper.sh and errors are stored in /app/setup.log
set -ex

APP_PATH="/app"
WEBUICONFIG="${APP_PATH}/webui-config.js"
WEBUICONFIG_SAFE="${APP_PATH}/webui-config.js.safe"
WEBUICONFIG_UNSAFE="${APP_PATH}/webui-config.js.unsafe"
ENVVARS="${APP_PATH}/env-vars"
DOCKERCOMPOSE="${APP_PATH}/docker-compose.yml"
CERTS="${APP_PATH}/certs"
CERT="${CERTS}/tls.crt"
PKEY="${CERTS}/tls.key"

# ========================================================================

# Default values
export HOST_NAME="localhost"
export PCS_LOG_LEVEL="Info"
export PCS_WEBUI_AUTH_TYPE="aad"
export PCS_IOTHUB_CONNSTRING=""

while [ "$#" -gt 0 ]; do
    case "$1" in
        --solution-setup-url)      PCS_SOLUTION_SETUP_URL="$2" ;; # e.g. https://raw.githubusercontent.com/Azure/device-simulatio-dotnet/DS-1.0.0/arm-deployment/devicesimulation
        --release-version)         PCS_RELEASE_VERSION="$2" ;;
        --docker-tag)              PCS_DOCKER_TAG="$2" ;;
        --solution-type)           PCS_SOLUTION_TYPE="$2" ;;
        --solution-name)           PCS_SOLUTION_NAME="$2" ;;
        --subscription-domain)     PCS_SUBSCRIPTION_DOMAIN="$2" ;;
        --subscription-id)         PCS_SUBSCRIPTION_ID="$2" ;;
        --hostname)                HOST_NAME="$2" ;;
        --log-level)               PCS_LOG_LEVEL="$2" ;;
        --resource-group)          PCS_RESOURCE_GROUP="$2" ;;
        --docdb-name)              PCS_DOCDB_NAME="$2" ;;
        --docdb-connstring)        PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING="$2" ;;
        --ssl-certificate)         PCS_CERTIFICATE="$2" ;;
        --ssl-certificate-key)     PCS_CERTIFICATE_KEY="$2" ;;
        --auth-audience)           PCS_AUTH_AUDIENCE="$2" ;;
        --auth-issuer)             PCS_AUTH_ISSUER="$2" ;;
        --auth-type)               PCS_WEBUI_AUTH_TYPE="$2" ;;
        --aad-appid)               PCS_WEBUI_AUTH_AAD_APPID="$2" ;;
        --aad-sp-client-id)        PCS_AAD_CLIENT_SP_ID="$2" ;;
        --aad-app-secret)          PCS_AAD_SECRET="$2" ;;
        --aad-tenant)              PCS_WEBUI_AUTH_AAD_TENANT="$2" ;;
        --aad-instance)            PCS_WEBUI_AUTH_AAD_INSTANCE="$2" ;;
        --cloud-type)              PCS_CLOUD_TYPE="$2" ;;
        --deployment-id)           PCS_DEPLOYMENT_ID="$2" ;;
        --resource-group-location) PCS_RESOURCE_GROUP_LOCATION="$2" ;;
        --vmss-name)               PCS_VMSS_NAME="$2" ;;
        --storage-connstring)      PCS_AZURE_STORAGE_ACCOUNT="$2" ;;
        --iothub-name)             PCS_IOHUB_NAME="$2" ;;
        --iothub-sku)              PCS_IOTHUB_SKU="$2" ;;
        --iothub-tier)             PCS_IOTHUB_TIER="$2" ;;
        --iothub-units)            PCS_IOTHUB_UNITS="$2" ;;
        --iothub-connstring)       PCS_IOTHUB_CONNSTRING="$2" ;;
        --app-insights-ikey)       PCS_APPINSIGHTS_INSTRUMENTATIONKEY="$2" ;;
    esac
    shift
done

if [ -z "$PCS_SOLUTION_SETUP_URL" ]; then
    echo "Setup URL not specified (see --solution-setup-url)"
    exit 1
fi

if [ -z "$PCS_RELEASE_VERSION" ]; then
    echo "Release version not specified (see --release-version)"
    exit 1
fi

# Note: Solution = devicesimulation
REPOSITORY="https://raw.githubusercontent.com/Azure/device-simulation-dotnet/${PCS_RELEASE_VERSION}/arm-deployment/devicesimulation/single-vm"
SCRIPTS_URL="${REPOSITORY}/scripts/"
SETUP_URL="${REPOSITORY}/setup/"

# ========================================================================

### Install Docker and Docker Compose

install_docker_ce() {
    apt-get update -o Acquire::CompressionTypes::Order::=gz \
        && DEBIAN_FRONTEND=noninteractive apt-get upgrade -y \
        && apt-get update \
        && apt-get remove docker docker-engine docker.io \
        && apt-get -y --allow-downgrades --allow-remove-essential --allow-change-held-packages --no-install-recommends install apt-transport-https ca-certificates curl gnupg2 software-properties-common \
        && curl -fsSL https://download.docker.com/linux/$(. /etc/os-release; echo "$ID")/gpg | sudo apt-key add - \
        && add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/$(. /etc/os-release; echo "$ID") $(lsb_release -cs) stable" \
        && apt-get update \
        && DEBIAN_FRONTEND=noninteractive apt-get -y --allow-downgrades install docker-ce docker-compose \
        && docker run --rm hello-world && docker rmi hello-world

    local RESULT=$?
    if [ $RESULT -ne 0 ]; then
        INSTALL_DOCKER_RESULT="FAIL"
    else
        INSTALL_DOCKER_RESULT="OK"
    fi
}

set +e
INSTALL_DOCKER_RESULT="OK"
install_docker_ce
if [ "$INSTALL_DOCKER_RESULT" != "OK" ]; then
    set -e
    echo "Error: first attempt to install Docker failed, retrying..."
    # Retry once, in case apt wasn't ready
    sleep 30
    install_docker_ce
    if [ "$INSTALL_DOCKER_RESULT" != "OK" ]; then
        echo "Error: Docker installation failed"
        exit 1
    fi
fi
set -e

# ========================================================================

# Configure Docker registry based on host name
# ToDo: we may need to add similar parameter to AzureGermanCloud and AzureUSGovernment
config_for_azure_china() {
    set +e
    local host_name=$1
    if (echo $host_name | grep -c  "\.cn$") ; then
        # If the host name has .cn suffix, dockerhub in China will be used to avoid slow network traffic failure.
        local config_file='/etc/docker/daemon.json'
        echo "{\"registry-mirrors\": [\"https://registry.docker-cn.com\"]}" > ${config_file}
        service docker restart

        # Rewrite the AAD issuer in Azure China environment
        export PCS_AUTH_ISSUER="https://sts.chinacloudapi.cn/$2/"
    fi
    set -e
}

config_for_azure_china $HOST_NAME $PCS_WEBUI_AUTH_AAD_TENANT

# ========================================================================

# Note: the directory might already exists, with "-p" there is no error
mkdir -p ${APP_PATH}
cd ${APP_PATH}

# ========================================================================

# Docker compose file

DOCKERCOMPOSE_SOURCE="${PCS_SOLUTION_SETUP_URL}/single-vm/docker-compose.yml"
wget $DOCKERCOMPOSE_SOURCE -O ${DOCKERCOMPOSE}
sed -i 's/${PCS_DOCKER_TAG}/'${PCS_DOCKER_TAG}'/g' ${DOCKERCOMPOSE}

# ========================================================================

# HTTPS certificates
mkdir -p ${CERTS}
touch ${CERT} && chmod 550 ${CERT}
touch ${PKEY} && chmod 550 ${PKEY}
# Always have quotes around the certificate and key value to preserve the formatting
echo "${PCS_CERTIFICATE}"      > ${CERT}
echo "${PCS_CERTIFICATE_KEY}"  > ${PKEY}

# ========================================================================

# Download scripts
wget $SCRIPTS_URL/logs.sh     -O /app/logs.sh     && chmod 750 /app/logs.sh
wget $SCRIPTS_URL/start.sh    -O /app/start.sh    && chmod 750 /app/start.sh
wget $SCRIPTS_URL/stats.sh    -O /app/stats.sh    && chmod 750 /app/stats.sh
wget $SCRIPTS_URL/status.sh   -O /app/status.sh   && chmod 750 /app/status.sh
wget $SCRIPTS_URL/stop.sh     -O /app/stop.sh     && chmod 750 /app/stop.sh

# ========================================================================

# Web App configuration
touch ${WEBUICONFIG} && chmod 444 ${WEBUICONFIG}
touch ${WEBUICONFIG_SAFE} && chmod 444 ${WEBUICONFIG_SAFE}
touch ${WEBUICONFIG_UNSAFE} && chmod 444 ${WEBUICONFIG_UNSAFE}

echo "var DeploymentConfig = {"                        > ${WEBUICONFIG_SAFE}
echo "  solutionName: '${PCS_SOLUTION_NAME}',"        >> ${WEBUICONFIG_SAFE}
echo "  authEnabled: true,"                           >> ${WEBUICONFIG_SAFE}
echo "  authType: '${PCS_WEBUI_AUTH_TYPE}',"          >> ${WEBUICONFIG_SAFE}
echo "  aad : {"                                      >> ${WEBUICONFIG_SAFE}
echo "    tenant: '${PCS_WEBUI_AUTH_AAD_TENANT}',"    >> ${WEBUICONFIG_SAFE}
echo "    appId: '${PCS_WEBUI_AUTH_AAD_APPID}',"      >> ${WEBUICONFIG_SAFE}
echo "    instance: '${PCS_WEBUI_AUTH_AAD_INSTANCE}'" >> ${WEBUICONFIG_SAFE}
echo "  },"                                           >> ${WEBUICONFIG_SAFE}
echo "  maxDevicesPerSimulation: 20000,"              >> ${WEBUICONFIG_SAFE}
echo "  minTelemetryInterval: 10000"                  >> ${WEBUICONFIG_SAFE}
echo "}"                                              >> ${WEBUICONFIG_SAFE}

echo "var DeploymentConfig = {"                        > ${WEBUICONFIG_UNSAFE}
echo "  solutionName: '${PCS_SOLUTION_NAME}',"        >> ${WEBUICONFIG_UNSAFE}
echo "  authEnabled: false,"                          >> ${WEBUICONFIG_UNSAFE}
echo "  authType: '${PCS_WEBUI_AUTH_TYPE}',"          >> ${WEBUICONFIG_UNSAFE}
echo "  aad : {"                                      >> ${WEBUICONFIG_UNSAFE}
echo "    tenant: '${PCS_WEBUI_AUTH_AAD_TENANT}',"    >> ${WEBUICONFIG_UNSAFE}
echo "    appId: '${PCS_WEBUI_AUTH_AAD_APPID}',"      >> ${WEBUICONFIG_UNSAFE}
echo "    instance: '${PCS_WEBUI_AUTH_AAD_INSTANCE}'" >> ${WEBUICONFIG_UNSAFE}
echo "  },"                                           >> ${WEBUICONFIG_UNSAFE}
echo "  maxDevicesPerSimulation: 20000,"              >> ${WEBUICONFIG_UNSAFE}
echo "  minTelemetryInterval: 10000"                  >> ${WEBUICONFIG_UNSAFE}
echo "}"                                              >> ${WEBUICONFIG_UNSAFE}

cp -p ${WEBUICONFIG_SAFE} ${WEBUICONFIG}

# ========================================================================

# Environment variables
touch ${ENVVARS} && chmod 440 ${ENVVARS}

echo "# Valid values: Debug, Info, Warn, Error"                                                           > ${ENVVARS}
echo "export PCS_LOG_LEVEL=\"${PCS_LOG_LEVEL}\""                                                         >> ${ENVVARS}
echo ""                                                                                                  >> ${ENVVARS}
echo "export HOST_NAME=\"${HOST_NAME}\""                                                                 >> ${ENVVARS}
echo "export PCS_AUTH_ISSUER=\"${PCS_AUTH_ISSUER}\""                                                     >> ${ENVVARS}
echo "export PCS_AUTH_AUDIENCE=\"${PCS_AUTH_AUDIENCE}\""                                                 >> ${ENVVARS}
echo "export PCS_IOTHUB_CONNSTRING=\"${PCS_IOTHUB_CONNSTRING}\""                                         >> ${ENVVARS}
echo "export PCS_IOHUB_NAME=\"${PCS_IOHUB_NAME}\""                                                       >> ${ENVVARS}
echo "export PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING=\"${PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING}\""   >> ${ENVVARS}
echo "export PCS_SUBSCRIPTION_DOMAIN=\"${PCS_SUBSCRIPTION_DOMAIN}\""                                     >> ${ENVVARS}
echo "export PCS_SUBSCRIPTION_ID=\"${PCS_SUBSCRIPTION_ID}\""                                             >> ${ENVVARS}
echo "export PCS_WEBUI_AUTH_AAD_APPID=\"${PCS_WEBUI_AUTH_AAD_APPID}\""                                   >> ${ENVVARS}
echo "export PCS_WEBUI_AUTH_AAD_TENANT=\"${PCS_WEBUI_AUTH_AAD_TENANT}\""                                 >> ${ENVVARS}
echo "export PCS_AAD_CLIENT_SP_ID=\"${PCS_AAD_CLIENT_SP_ID}\""                                           >> ${ENVVARS}
echo "export PCS_AAD_SECRET=\"${PCS_AAD_SECRET}\""                                                       >> ${ENVVARS}
echo "export PCS_RESOURCE_GROUP=\"${PCS_RESOURCE_GROUP}\""                                               >> ${ENVVARS}
echo "export PCS_SOLUTION_TYPE=\"${PCS_SOLUTION_TYPE}\""                                                 >> ${ENVVARS}
echo "export PCS_SOLUTION_NAME=\"${PCS_SOLUTION_NAME}\""                                                 >> ${ENVVARS}
echo "export PCS_SEED_TEMPLATE=\"multiple-simulations-template\""                                        >> ${ENVVARS}
echo "export PCS_CLOUD_TYPE=\"${PCS_CLOUD_TYPE}\""                                                       >> ${ENVVARS}
echo "export PCS_DEPLOYMENT_ID=\"${PCS_DEPLOYMENT_ID}\""                                                 >> ${ENVVARS}
echo "export PCS_AZURE_STORAGE_ACCOUNT=\"${PCS_AZURE_STORAGE_ACCOUNT}\""                                 >> ${ENVVARS}
echo "export PCS_RESOURCE_GROUP_LOCATION=\"${PCS_RESOURCE_GROUP_LOCATION}\""                             >> ${ENVVARS}
echo "export PCS_VMSS_NAME=\"${PCS_VMSS_NAME}\""                                                         >> ${ENVVARS}
echo "export PCS_APPINSIGHTS_INSTRUMENTATIONKEY=\"${PCS_APPINSIGHTS_INSTRUMENTATIONKEY}\""               >> ${ENVVARS}

# Setting some empty vars as these are required vars by Config service
echo "export PCS_DEVICESIMULATION_WEBSERVICE_URL=\"\""                                                   >> ${ENVVARS}
echo "export PCS_TELEMETRY_WEBSERVICE_URL=\"\""                                                          >> ${ENVVARS}
echo "export PCS_IOTHUBMANAGER_WEBSERVICE_URL=\"\""                                                      >> ${ENVVARS}
echo "export PCS_BINGMAP_KEY=\"\""                                                                       >> ${ENVVARS}

echo ""                                                                                                  >> ${ENVVARS}
echo "##########################################################################################"        >> ${ENVVARS}
echo "# Development settings, don't change these in Production"                                          >> ${ENVVARS}
echo "# You can run 'start.sh --unsafe' to temporarily disable Auth and Cross-Origin protections"        >> ${ENVVARS}
echo ""                                                                                                  >> ${ENVVARS}
echo "# Format: true | false"                                                                            >> ${ENVVARS}
echo "# empty => Auth required"                                                                          >> ${ENVVARS}
echo "export PCS_AUTH_REQUIRED=\"\""                                                                     >> ${ENVVARS}
echo ""                                                                                                  >> ${ENVVARS}
echo "# Format: { 'origins': ['*'], 'methods': ['*'], 'headers': ['*'] }"                                >> ${ENVVARS}
echo "# empty => CORS support disabled"                                                                  >> ${ENVVARS}
echo "export PCS_CORS_WHITELIST=\"\""                                                                    >> ${ENVVARS}

# ========================================================================

# Shell environment enhancements
echo "### CUSTOMIZATIONS ###" >> /etc/nanorc
wget $SETUP_URL/nanorc -O /tmp/nanorc && cat /tmp/nanorc >> /etc/nanorc

echo "### CUSTOMIZATIONS ###" >> /etc/bash.bashrc
wget $SETUP_URL/bashrc -O /tmp/bashrc && cat /tmp/bashrc >> /etc/bash.bashrc

# Optional script to customize Bash shell, to be executed manually by the user
wget https://aka.ms/bashir-setup -O /etc/bashir-script \
    && chmod 444 /etc/bashir-script \
    && echo 'cat /etc/bashir-script | bash && . ~/.bashir && echo "Restart Bash to see the changes (e.g. exit and login again)."' > /usr/local/bin/bashir-setup \
    && chmod 755 /usr/local/bin/bashir-setup

# ========================================================================

# Auto-start after reboots
wget $SETUP_URL/init -O /etc/init.d/azure-iot-solution \
    && chmod 755 /etc/init.d/azure-iot-solution \
    && update-rc.d azure-iot-solution defaults

# ========================================================================

nohup /app/start.sh > /dev/null 2>&1&
