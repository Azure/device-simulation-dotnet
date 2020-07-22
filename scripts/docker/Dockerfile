FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

LABEL maintainer = @miwolfms @tommo-ms @saixiaohui

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
