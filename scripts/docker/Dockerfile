FROM microsoft/dotnet:2.2-runtime

MAINTAINER IoTSimTeam@microsoft.com

LABEL Tags="Azure,IoT,Solutions,Simulation,.NET"

ARG user=pcsuser

RUN apt-get update \
  && DEBIAN_FRONTEND=noninteractive apt-get install -y net-tools \
  && apt-get clean \
  && rm -rf /var/lib/apt/lists/*

RUN useradd -m -s /bin/bash -U $user

COPY . /app/
RUN chown -R $user.$user /app
WORKDIR /app

RUN \
    # Ensures the entry point is executable
    chmod ugo+x /app/run.sh && \
    # Clean up destination folder
    rm -f /app/Dockerfile /app/.dockerignore

VOLUME ["/app/data"]

ENTRYPOINT ["/bin/bash", "/app/run.sh"]

USER $user
