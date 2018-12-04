#!/usr/bin/env bash

PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING="AccountEndpoint=https://_______.documents.azure.com:443/;AccountKey=_______;"

if [ ! -z "$1" ]; then
    PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING="$1"
fi

docker run -itd \
    -p 9022:9022 \
    -e PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING \
    --name pcs-storage-adapter \
    azureiotpcs/pcs-storage-adapter-dotnet:testing
