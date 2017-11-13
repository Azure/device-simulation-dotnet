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
          "cargotemperature": 38,
          "cargotemperature_unit": "F"
        },
        "Script": {
          "Type": "javascript",
          "Path": "truck-01-state.js",
          "Interval": "00:00:05"
        }
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
          "MessageTemplate": "{\"cargotemperature\":${cargotemperature},\"cargotemperature_unit\":\"${cargotemperature_unit}\"}",
          "MessageSchema": {
            "Name": "truck-cargotemperature;v1",
            "Format": "JSON",
            "Fields": {
              "cargotemperature": "Double",
              "cargotemperature_unit": "Text"
            }
          }
        }
      ],
      "CloudToDeviceMethods": {
        "DecreaseCargoTemperature": {
          "Type": "javascript",
          "Path": "TBD.js",
          "Interval": "00:00:00"
        },
        "IncreaseCargoTemperature": {
          "Type": "javascript",
          "Path": "TBD.js",
          "Interval": "00:00:00"
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
