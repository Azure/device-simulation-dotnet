FROM microsoft/dotnet:2.0.3-runtime-jessie

MAINTAINER Devis Lucato (https://github.com/dluc)

LABEL Tags="Azure,IoT,Solutions,Simulation,.NET"

ARG user=pcsuser

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
