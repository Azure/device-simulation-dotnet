Service layer
=============

## Guidelines

* The service layer is responsible for the business logic of the microservice
  and for dealing with external dependencies like storage, IoT Hub, etc.
* The service layer has no knowledge of the web service or other entry points.

## Conventions

* Configuration is injected into the service layer by the entry point projects.
