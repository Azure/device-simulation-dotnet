API specifications - Device Properties
======================================

## Get a list of device model properties

The list of device model properties contains properties from all device models.

Request:
```
GET /v1/devicemodelproperties
```

Response:
```
200 OK
Content-Type: application/json
```
```json
{
  "Items": [
    {
      "Id": "Type"
    },
    {
      "Id": "Firmware"
    },
    {
      "Id": "Location"
    },
    {
      "Id": "Latitude"
    },
    {
      "Id": "Longitude"
    }
  ],
  "$metadata": {
    "$type": "DeviceModelPropertyList;1",
    "$uri": "/v1/devicemodelproperties"
  }
}
```
