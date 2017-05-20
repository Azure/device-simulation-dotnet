FROM mono:latest

MAINTAINER Devis Lucato (https://github.com/dluc)

EXPOSE 8080

ENTRYPOINT ["mono", "Microsoft.Azure.IoTSolutions.ProjectNameHere.WebService.exe"]

COPY . /app/
WORKDIR /app
