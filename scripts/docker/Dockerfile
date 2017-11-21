FROM microsoft/dotnet:2.0.3-runtime-jessie

MAINTAINER Devis Lucato (https://github.com/dluc)

LABEL Tags="Azure,IoT,Solutions,Simulation,.NET"

COPY . /app/
WORKDIR /app

RUN \
    # Ensures the entry point is executable
    chmod ugo+x /app/run.sh && \
    # Allow user to mount custom files in /add/data.
    # Both web service and simulation agent have a copy of the "data" folder,
    # inherited from the Service project. This remove the two copies and move
    # the files in /app/data, which is the folder that can be mounted to
    # provide custom device models and/or customized scripts.
    mv /app/webservice/data /app && \
    cd /app/webservice && rm -fR data && ln -s /app/data && \
    cd /app/simulationagent && rm -fR data && ln -s /app/data && \
    # Clean up destination folder
    rm -f /app/Dockerfile /app/.dockerignore

VOLUME ["/app/data"]

ENTRYPOINT ["/bin/bash", "/app/run.sh"]
