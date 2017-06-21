FROM microsoft/dotnet:1.1.2-runtime-jessie

MAINTAINER Devis Lucato (https://github.com/dluc)

# These ports will be useful only when running the service with Docker.
# When running the service in standalone mode, the TCP ports might be
# different depending on the settings stored in app.config.
# We expose 3 ports just in case we have multiple endpoints.
# Typically only port 8080 will be used for the web service.
EXPOSE 8080 8081 8082

ENTRYPOINT ["/bin/bash", "/app/run.sh"]

COPY . /app/
WORKDIR /app
