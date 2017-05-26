Service layer models
====================

## Guidelines

* Do not reference classes from the "webservice" project.
* Ensure datetime values are transfered using UTC timezone.
* Use `new DateTimeOffset(azureDevice.LastActivityTime, TimeSpan.Zero)`
  to parse datetime values returned by Azure IoT SDK.

## Conventions

* For DateTime fields use System.DateTimeOffset.
