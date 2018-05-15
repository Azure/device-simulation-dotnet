API specifications - Device Models
==================================

## Get list of device models that can be simulated

The list of device models is injected into the service using a list of
configuration files, which are automatically discovered when the service starts.

To create a new device model, a new configuration file is added to the folder
where the configuration files are stored, and the microservice is restarted or re-deployed.

The service configuration allows to specify the path where these files are stored.

Request:
```
GET /v1/devicemodels
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
      "Id": "truck-01",
      "Version": "0.0.1",
      "Name": "Truck",
      "Description": "Truck with GPS, speed and cargo temperature sensors",
      "Protocol": "AMQP",
      "Simulation": {
        "InitialState": {
          "online": true,
          "latitude": 47.445301,
          "longitude": -122.296307,
          "speed": 30,
          "speed_unit": "mph",
          "temperature": 38,
          "temperature_unit": "F"
        },
        "Interval": "00:00:10",
        "Scripts": [
          {
            "Type": "javascript",
            "Path": "truck-01-state.js"
          }
        ]
      },
      "Properties": {
        "Type": "Truck",
        "Location": "Field",
        "Latitude": 47.445301,
        "Longitude": -122.296307
      },
      "Telemetry": [
        {
          "Interval": "00:00:03",
          "MessageTemplate": "{\"latitude\":${latitude},\"longitude\":${longitude}}",
          "MessageSchema": {
            "Name": "truck-geolocation;v1",
            "Format": "JSON",
            "Fields": {
              "latitude": "Double",
              "longitude": "Double"
            }
          }
        },
        {
          "Interval": "00:00:05",
          "MessageTemplate": "{\"speed\":${speed},\"speed_unit\":\"${speed_unit}\"}",
          "MessageSchema": {
            "Name": "truck-speed;v1",
            "Format": "JSON",
            "Fields": {
              "speed": "Double",
              "speed_unit": "Text"
            }
          }
        },
        {
          "Interval": "00:00:05",
          "MessageTemplate": "{\"temperature\":${temperature},\"temperature_unit\":\"${temperature_unit}\"}",
          "MessageSchema": {
            "Name": "truck-temperature;v1",
            "Format": "JSON",
            "Fields": {
              "temperature": "Double",
              "temperature_unit": "Text"
            }
          }
        }
      ],
      "CloudToDeviceMethods": {
        "SetTemperature": {
          "Type": "javascript",
          "Path": "SetTemperature-method.js"
        }
      },
      "$metadata": {
        "$type": "DeviceModel;1",
        "$uri": "/v1/devicemodels/truck-01"
      }
    }
  ],
  "$metadata": {
    "$type": "DeviceModelList;1",
    "$uri": "/v1/devicemodels"
  }
}
```
