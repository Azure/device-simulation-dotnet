#!/usr/bin/env bash
# Copyright (c) Microsoft. All rights reserved.
# Note: Windows Bash doesn't support shebang extra params
set -e

cd /app

if [[ "$1" == "" ]]; then
  docker-compose logs
else
  docker logs -f --tail 100 $1
fi
