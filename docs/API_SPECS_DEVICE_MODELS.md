API specifications - Device Models
==================================

There are two types of device models:
1. Stock device models, compiled into the service
2. Custom device models, created by an application/a user

## Get device models

### Get a list of device models that can be simulated

The list of device models contains stock device models (which is injected into
the service using a list of configuration files automatically discovered when
the service starts) and custom device models which are stored in a database.

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
        "Interval": "00:00:05",
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
          "Path": "TempDecrease-method.js"
        },
        "IncreaseCargoTemperature": {
          "Type": "javascript",
          "Path": "TempIncrease-method.js"
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

### Get a device model by id

Request:
```
GET /v1/devicemodels/${id}
```

Response example:
```
200 OK
Content-Type: application/json; charset=utf-8
```
```json
{
  "ETag": "12345678-9941-4499-ba1b-123456789012",
  "Id": "12345678-c6c0-49ae-9bfa-123456789012",
  "Version": "0.0.0",
  "Name": "Chiller",
  "Description": "Chiller with external temperature, humidity and pressure sensors.",
  "Protocol": "MQTT",
  "Type": "",
  "Simulation": {
    "InitialState": {
      "online": true,
      "temperature": 75,
      "temperature_unit": "F",
      "humidity": 70,
      "humidity_unit": "%",
      "pressure": 150,
      "pressure_unit": "psig",
      "simulation_state": "normal_pressure"
    },
    "Interval": "00:00:10",
    "Scripts": [
      {
        "Type": "javascript",
        "Path": "chiller-01-state.js"
      }
    ]
  },
  "Properties": {},
  "Telemetry": [
    {
      "Interval": "00:00:10",
      "MessageTemplate": "{\"temperature\":${temperature},\"temperature_unit\":\"${temperature_unit}\",\"humidity\":${humidity},\"humidity_unit\":\"${humidity_unit}\",\"pressure\":${pressure},\"pressure_unit\":\"${pressure_unit}\"}",
      "MessageSchema": {
        "Name": "chiller-sensors;v1",
        "Format": "JSON",
        "Fields": {
            "temperature": "Double",
            "temperature_unit": "Text",
            "humidity": "Double",
            "humidity_unit": "Text",
            "pressure": "Double",
            "pressure_unit": "Text"
        }
      }
    }
  ],
  "CloudToDeviceMethods": {},
  "$metadata": {
    "$type": "DeviceModel;2",
    "$uri": "/v1/devicemodels/12345678-9941-4499-ba1b-123456789012",
    "$created": "2018-03-29T23:33:03+00:00",
    "$modified": "2018-03-29T23:33:03+00:00"
  }
}
```

## Creating device models

### Creating stock device models

To create a new device model, a new JSON configuration file is added to the
folder where the configuration files are stored, and the microservice is
restarted or re-deployed.

The service configuration (`appsettings.ini`) allows to specify the path where
these files are stored.

### Creating custom device models

Request:
```
POST /v1/devicemodels/
Content-Type: application/json; charset=utf-8
```
```json
{
  "Version": "0.0.1",
  "Name": "Chiller",
  "Description": "Chiller with external temperature, humidity and pressure sensors.",
  "Protocol": "MQTT",
  "Simulation": {
    "InitialState": {
      "online": true,
      "temperature": 75,
      "temperature_unit": "F",
      "humidity": 70,
      "humidity_unit": "%",
      "pressure": 150,
      "pressure_unit": "psig",
      "simulation_state": "normal_pressure"
    },
    "Interval": "00:00:10",
    "Scripts": [
      {
        "Type": "javascript",
        "Path": "chiller-01-state.js"
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
      "Interval": "00:00:10",
      "MessageTemplate": "{\"temperature\":${temperature},\"temperature_unit\":\"${temperature_unit}\",\"humidity\":${humidity},\"humidity_unit\":\"${humidity_unit}\",\"pressure\":${pressure},\"pressure_unit\":\"${pressure_unit}\"}",
      "MessageSchema": {
        "Name": "Chiller;v1",
        "Format": "JSON",
        "Fields": {
          "temperature": "Double",
          "temperature_unit": "Text",
          "humidity": "Double",
          "humidity_unit": "Text",
          "pressure": "Double",
          "pressure_unit": "Text"
        }
      }
    }
  ],
  "CloudToDeviceMethods": {}
}
```

Response:
```
200 OK
Content-Type: application/json; charset=utf-8
```
```json
{
  "ETag": "12345678-9941-4499-ba1b-123456789012",
  "Id": "12345678-c6c0-49ae-9bfa-123456789012",
  "Version": "0.0.1",
  "Name": "Chiller",
  "Description": "Chiller with external temperature, humidity and pressure sensors.",
  "Protocol": "MQTT",
  "Type": "custom",
  "Simulation": {
	"InitialState": {
	  "online": true,
	  "temperature": 75,
	  "temperature_unit": "F",
	  "humidity": 70,
	  "humidity_unit": "%",
	  "pressure": 150,
	  "pressure_unit": "psig",
	  "simulation_state": "normal_pressure"
	},
	"Interval": "00:00:10",
	"Scripts": [
	  {
		"Type": "javascript",
		"Path": "chiller-01-state.js"
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
	  "Interval": "00:00:10",
	  "MessageTemplate": "{\"temperature\":${temperature},\"temperature_unit\":\"${temperature_unit}\",\"humidity\":${humidity},\"humidity_unit\":\"${humidity_unit}\",\"pressure\":${pressure},\"pressure_unit\":\"${pressure_unit}\"}",
	  "MessageSchema": {
		"Name": "Chiller;v1",
		"Format": "JSON",
		"Fields": {
		  "temperature": "Double",
		  "temperature_unit": "Text",
		  "humidity": "Double",
		  "humidity_unit": "Text",
		  "pressure": "Double",
		  "pressure_unit": "Text"
		}
	  }
	}
  ],
  "CloudToDeviceMethods": {},
  "$metadata": {
    "$type": "DeviceModel;2",
    "$uri": "/v1/devicemodels/12345678-c6c0-49ae-9bfa-123456789012",
    "$created": "2018-03-29T23:33:03+00:00",
    "$modified": "2018-03-29T23:33:03+00:00"
  }
}
```

## Modifying a device model

### Modifying stock device models

Directly modify the JSON configuration files stored in the file system.

[Device model docs](https://docs.microsoft.com/azure/iot-suite/iot-suite-remote-monitoring-test#define-the-characteristics-of-the-new-device-type)

### Modifying custom device models

Custom device models can be modified by calling PUT and passing the existing 
device model ETag and ID. The PUT request should be preceded by a GET request,
to pull the existing device model content, before editing it and sending the
modification(s).

If a client sends a PUT request with a non-existent ID, the service automatically
creates a new model and stores it in the database.

Request:
```
PUT /v1/devicemodels/${id}
Content-Type: application/json; charset=utf-8
```
```json
{
  "ETag": "12345678-abcd-1c4a-fa9b-123456789012",
  "Id": "12345678-c6c0-49ae-9bfa-123456789012",
  "Version": "0.0.1",
  "Name": "Chiller",
  "Description": "Chiller with external temperature, humidity and pressure sensors.",
  "Protocol": "MQTT",
  "Type": "custom",
  "Simulation": {
    "InitialState": {
      "online": true,
      "temperature": 75,
      "temperature_unit": "F",
      "humidity": 70,
      "humidity_unit": "%",
      "pressure": 150,
      "pressure_unit": "psig",
      "simulation_state": "normal_pressure"
    },
    "Interval": "00:00:10",
    "Scripts": [
      {
        "Type": "javascript",
        "Path": "chiller-01-state.js"
      }
    ]
  },
  "Properties": {},
  "Telemetry": [
    {
      "Interval": "00:00:10",
      "MessageTemplate": "{\"temperature\":${temperature},\"temperature_unit\":\"${temperature_unit}\",\"humidity\":${humidity},\"humidity_unit\":\"${humidity_unit}\",\"pressure\":${pressure},\"pressure_unit\":\"${pressure_unit}\"}",
      "MessageSchema": {
        "Name": "chiller-sensors;v1",
        "Format": "JSON",
        "Fields": {
          "temperature": "Double",
          "temperature_unit": "Text",
          "humidity": "Double",
          "humidity_unit": "Text",
          "pressure": "Double",
          "pressure_unit": "Text"
        }
      }
    }
  ],
  "CloudToDeviceMethods": {},
}
```

Response:
```
200 OK
Content-Type: application/json; charset=utf-8
```
```json
{
  "ETag": "78901216-efab-9bfa-ab32-789012123456",
  "Id": "12345678-c6c0-49ae-9bfa-123456789012",
  "Version": "0.0.1",
  "Name": "Chiller",
  "Description": "Chiller with external temperature, humidity and pressure sensors.",
  "Protocol": "MQTT",
  "Type": "",
  "Simulation": {
	"InitialState": {
	  "online": true,
	  "temperature": 75,
	  "temperature_unit": "F",
	  "humidity": 70,
	  "humidity_unit": "%",
	  "pressure": 150,
	  "pressure_unit": "psig",
	  "simulation_state": "normal_pressure"
	},
	"Interval": "00:00:10",
	"Scripts": [
	  {
		"Type": "javascript",
		"Path": "chiller-01-state.js"
	  }
	]
  },
  "Properties": {},
  "Telemetry": [
	{
	  "Interval": "00:00:10",
	  "MessageTemplate": "{\"temperature\":${temperature},\"temperature_unit\":\"${temperature_unit}\",\"humidity\":${humidity},\"humidity_unit\":\"${humidity_unit}\",\"pressure\":${pressure},\"pressure_unit\":\"${pressure_unit}\"}",
	  "MessageSchema": {
		"Name": "chiller-sensors;v1",
		"Format": "JSON",
		"Fields": {
		  "temperature": "Double",
		  "temperature_unit": "Text",
		  "humidity": "Double",
		  "humidity_unit": "Text",
		  "pressure": "Double",
		  "pressure_unit": "Text"
		}
	  }
	}
  ],
  "CloudToDeviceMethods": {},
  "$metadata": {
    "$type": "DeviceModel;2",
    "$uri": "/v1/devicemodels/12345678-c6c0-49ae-9bfa-123456789012",
    "$created": "2018-03-29T23:33:03+00:00",
    "$modified": "2018-03-29T23:33:03+00:00"
  }
}
```

## Deleting a device model

### Deleting a stock device model

Stock device models can be deleted only by removing its configuration file
from the file system. 

```
DELETE /v1/devicemodels/${id}
```
Response:
```
403 Forbidden
```

### Deleting a custom device model

Custom device models can be deleted using the DELETE method with its ID.

```
DELETE /v1/devicemodels/${id}
```
Response:
```
200 OK
```