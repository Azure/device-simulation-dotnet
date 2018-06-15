API specifications - Device Model Properties
======================================

## Get a list of device model properties

The list of device model properties contains properties from all device models.

Request:
```
GET /v1/deviceModelProperties
```

Response:
```
200 OK
Content-Type: application/json
```
```json
{
    "Items": [
        "Properties.Reported.Type",
        "Properties.Reported.Firmware",
        "Properties.Reported.Model",
        "Properties.Reported.Location",
        "Properties.Reported.Latitude",
        "Properties.Reported.Longitude",
        "Properties.Reported.FirmwareUpdateStatus"
    ],
    "$metadata": {
        "$type": "DeviceModelPropertyList;1",
        "$uri": "/v1/deviceModelProperties"
    }
}
```
