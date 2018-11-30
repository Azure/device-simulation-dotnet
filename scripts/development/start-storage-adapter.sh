#!/usr/bin/env bash

PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING="AccountEndpoint=https://_______.documents.azure.com:443/;AccountKey=_______;"

docker run -itd \
    -p 9022:9022 \
    -e PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING \
    azureiotpcs/pcs-storage-adapter-dotnet:testing
