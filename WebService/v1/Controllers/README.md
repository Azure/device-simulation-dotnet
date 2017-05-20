Web service controllers
=======================

## Guidelines

* Always access external services through logic in the service layer.
* Consume Azure IoT SDK code through the service layer, i.e. do not reference
  classes from the Azure IoT SDK (or other SDKs).

## Conventions

* Version controllers and models together (e.v. under 'v1' namespace).
