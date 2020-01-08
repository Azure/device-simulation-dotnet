#!/usr/bin/env bash
# Copyright (c) Microsoft. All rights reserved.
# Note: Windows Bash doesn't support shebang extra params
set -e

list=$(docker ps -aq)

if [ -n "$list" ]; then
    docker rm -f $list
fi
