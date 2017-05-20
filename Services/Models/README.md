Service layer models
====================

## Guidelines

* Do not reference classes from the "webservice" project.
* Ensure datetime values are transfered using UTC timezone.
* Use `new DateTimeOffset(azureDevice.LastActivityTime, TimeSpan.Zero)`
  to parse datetime values returned by Azure IoT SDK.

## Conventions

* Add the "ServiceModel" suffix to the models in this folder. This allows to
  distinguish these classes from the classes in the SDK and in the
  webservice.
* For DateTime fields use System.DateTimeOffset.
