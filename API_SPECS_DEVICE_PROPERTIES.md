API specifications - Device Properties
======================================

## Get a list of device properties

The list of device properties contains properties from all device models.

Request:
```
GET /v1/deviceproperties
```

Response:
```
200 OK
Content-Type: application/JSON
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
    "$type": "DevicePropertyList;1",
    "$uri": "/v1/deviceProperties"
  }
}
```
