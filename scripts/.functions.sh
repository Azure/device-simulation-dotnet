#!/usr/bin/env bash

COL_NO="\033[0m" # no color
COL_ERR="\033[1;31m" # light red
COL_H1="\033[1;33m" # yellow
COL_H2="\033[1;36m" # light cyan

header() {
    echo -e "${COL_H1}\n### $1 ${COL_NO}"
}

error() {
    echo -e "${COL_ERR}$1 ${COL_NO}"
}

check_dependency_nuget() {
    set +e
    TEST=$(which nuget)
    if [[ -z "$TEST" ]]; then
        echo "ERROR: 'nuget' command not found."
        echo "Install Mono 5.x and the 'nuget' package, and make sure the 'nuget' command is in the PATH."
        echo "Mono installation: http://www.mono-project.com/docs/getting-started/install"
        exit 1
    fi
    set -e
}

check_dependency_msbuild() {
    set +e
    TEST=$(which msbuild)
    if [[ -z "$TEST" ]]; then
        echo "ERROR: 'msbuild' command not found."
        echo "Install Mono 5.x and the 'msbuild' package, and make sure the 'nuget' command is in the PATH."
        echo "Mono installation: http://www.mono-project.com/docs/getting-started/install"
        exit 1
    fi
    set -e
}

check_dependency_mono() {
    set +e
    TEST=$(which mono)
    if [[ -z "$TEST" ]]; then
        echo "ERROR: 'msbuild' command not found."
        echo "Install Mono 5.x and make sure the 'mono' command is in the PATH."
        echo "Mono installation: http://www.mono-project.com/docs/getting-started/install"
        exit 1
    fi
    set -e
}

check_dependency_dotnet() {
    set +e
    TEST=$(which dotnet)
    if [[ -z "$TEST" ]]; then
        echo "ERROR: 'dotnet' command not found."
        echo "Install .NET Core and make sure the 'dotnet' command is in the PATH."
        echo ".NET Core installation: https://dotnet.github.io"
        exit 1
    fi
    set -e
}

check_dependency_docker() {
    set +e
    TEST=$(which docker)
    if [[ -z "$TEST" ]]; then
        echo "ERROR: 'docker' command not found."
        echo "Install Docker and make sure the 'docker' command is in the PATH."
        echo "Docker installation: https://www.docker.com/community-edition#/download"
        exit 1
    fi
    set -e
}
