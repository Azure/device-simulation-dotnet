API specifications - Simulations
================================

## Creating simulations

The service supports only one simulation, with ID "1".

### Creation with POST

When invoking the API using the POST HTTP method, the service will always
attempt to create a new simulation. Since the service allows
to create only one simulation, retrying the request will result in errors
after the first request has been successfully completed.

```
POST /v1/simulations
Content-Type: application/json; charset=utf-8
```
```json
{
  "Enabled": <true|false>,
  "DeviceModels": [
    {
      "Id": "<model ID>",
      "Count": <count>
    },
    {
      "Id": "<model ID>",
      "Count": <count>
    },
    ...
  ]
}
```

### Creation and Editing with PUT

When invoking the API using the PUT HTTP method, the service will attempt
to modify an existing simulation, creating a new one if the Id does not
match any existing simulation. When using PUT, the simulation Id is passed
through the URL. PUT requests are idempotent and don't generate errors when
retried (unless the payload differs during a retry, in which case the ETag
mismatch will generate an error).

```
PUT /v1/simulations/1
Content-Type: application/json; charset=utf-8
```
```json
{
  "Enabled": <true|false>,
  "DeviceModels": [
    {
      "Id": "<model ID>",
      "Count": <count>
    },
    {
      "Id": "<model ID>",
      "Count": <count>
    },
    ...
  ]
}
```

## Create default simulation

The default simulation can be created without passing any input data, in which
case the simulation created starts immediately. A client can however specify the
status explicitly if required, for example to create the simulation without
starting it. The format of the response remains the same.

### Case 1

Request:
```
POST /v1/simulations?template=default
Content-Type: application/json; charset=utf-8
```

### Case 2

Request:
```
POST /v1/simulations?template=default
Content-Type: application/json; charset=utf-8
```
```json
{ "Enabled": false }
```

Response:
```
200 OK
Content-Type: application/json; charset=utf-8
```
```json
{
  "ETag": "969ee1fb277640",
  "Id": "1",
  "Enabled": true,
  "DeviceModels": [
    {
      "Id": "truck-01",
      "Count": 1
    },
    {
      "Id": "engine-02",
      "Count": 1
    },
    {
      "Id": "truck-02",
      "Count": 1
    },
    {
      "Id": "engine-01",
      "Count": 1
    }
  ],
  "$metadata": {
    "$type": "Simulation;1",
    "$uri": "/v1/simulations/1",
    "$version": "1",
    "$created": "2017-05-31T01:21:37+00:00",
    "$modified": "2017-05-31T01:21:37+00:00"
  }
}
```

## Pointing a simulation to a custom Azure IoT Hub

By default the simulation will run using the hub specified in the
configuration file.

To use a different Azure IoT Hub, it is possible to set a custom
connection string for the IoT Hub to connect to, in the PUT request.

Note: the SharedAccessKeyName must be either "iothubowner" or a custom policy with registry read/write and service permissions.

Example value in the JSON payload: 
```
"ConnectionString": "HostName=<iothub name>.azure-devices.net;SharedAccessKeyName=<iothubowner | custom policy>;SharedAccessKey=<valid iothub key>"
```

When using a custom connection string, the web service response will
remove the sensitive key before returning the simulation details.

If a valid connection string has already been provided, a partial
connection string with an empty secret key may be provided:
Example:
```
"ConnectionString": "HostName=iothub-abcde.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey="
```

To switch back to the default Azure IoT Hub stored in the configuration file,
use the value "default" as the connection string.

```
PUT /v1/simulations/1

{
  "Enabled": <true|false>,
  "IotHub": {
      "ConnectionString": "<valid iothub connection string | default>"
  }
  "DeviceModels": [
    {
      "Id": "<model ID>",
      "Count": <count>
    },
    ...
  ]
  ...
}
```

## Scheduling the simulation and setting a duration

Unless specified, a simulation will run continuously, until stopped.

It is possible to set a start and end time, for example to schedule the
simulation to run in the future and for a defined duration.

To set start and end times, use the `StartTime` and `EndTime`, set in UTC
timezone. Both values are optional, an empty `EndTime` will cause the
simulation to run forever, once started.

The fields can be set in two different formats, either passing a UTC datetime,
or a "NOW" plus/minus an
[ISO8601 formatted duration](https://en.wikipedia.org/wiki/ISO_8601#Durations).

For instance, to start the simulation immediately, and run for two hours, the
client should use:
* StartTime: NOW
* EndTime: NOW+PT2H

while to start a simulation at midnight, for two hours:
* StartTime: 2018-07-07T00:00:00z
* EndTime: 2018-07-07T02:00:00z

Request example:
```
POST /v1/simulations
Content-Type: application/json; charset=utf-8
```
```json
{
  "Enabled": true,
  "StartTime": "NOW",
  "EndTime": "NOW+P7D",
  "DeviceModels": [
    {
      "Id": "truck-01",
      "Count": 1
    }
  ]
}
```

## Create simulation passing in a list of device models and device count

A client can create a simulation different from the default template, for
example specifying which device models and how many devices to simulate.

Request:
```
POST /v1/simulations
Content-Type: application/json; charset=utf-8
```
```json
{
  "Enabled": true,
  "DeviceModels": [
    {
      "Id": "truck-01",
      "Count": 3
    },
    {
      "Id": "truck-02",
      "Count": 1
    },
    {
      "Id": "elevator-01",
      "Count": 10
    }
  ]
}
```

Response:
```
200 OK
Content-Type: application/json; charset=utf-8
```
```json
{
  "ETag": "cec0722b205740",
  "Id": "1",
  "Enabled": true,
  "DeviceModels": [
    {
      "Id": "truck-01",
      "Count": 3
    },
    {
      "Id": "truck-02",
      "Count": 1
    },
    {
      "Id": "elevator-01",
      "Count": 10
    }
  ],
  "$metadata": {
    "$type": "Simulation;1",
    "$uri": "/v1/simulations/1",
    "$version": "1",
    "$created": "2017-05-31T00:47:18+00:00",
    "$modified": "2017-05-31T00:47:18+00:00"
  }
}
```

## Creating a simulation with customized device models

Device models information is typically loaded from the device models
definitiond stored in the JSON files.  However, it's possible to override
some of these details, using an `Override` block in the API request.

The API allows to override the following device model details:

* **Scripts type, path and interval**.  If the device model defines multiple
  scripts, the order of the override must match the same order used
  in the device model JSON file.
* **Telemetry frequency, template and schema**.  If the device model defines
  multiple telemetry messages, the order of the overrides must match
  the same order used in the device model JSON file.


### Example: Custom telemetry frequency (a.k.a. "Interval")

The following example shows how to override the telemetry frequency for
a device model. Note that the order of the overrides is important.

The example creates a simulation using 4 device models: *truck-01*,
*truck-02*, *elevator-01*, *elevator-02*.
Only the frequency of *truck-01* is customized, while the others models use
the default settings, defined in their respective JSON files (*truck-02.json*,
*elevator-01.json*, *elevator-02.json*).

The *truck-01* device model defines 3 distinct telemetry messages, and
the example shows how to set a custom frequency for each one, **in order**
10 seconds, 20 seconds, and 30 seconds.

Request:
```
POST /v1/simulations
Content-Type: application/json; charset=utf-8
```
```json
{
  "Enabled": true,
  "DeviceModels": [
    {
      "Id": "truck-01",
      "Count": 3,
      "Override": {
        "Telemetry": [
          {
            "Interval": "00:00:10"
          },
          {
            "Interval": "00:00:20"
          },
          {
            "Interval": "00:00:30"
          }
        ]
      }
    },
    {
      "Id": "truck-02",
      "Count": 1
    },
    {
      "Id": "elevator-01",
      "Count": 10
    },
    {
      "Id": "elevator-02",
      "Count": 1
    }
  ]
}
```

### Example: Customize scripts (custom sensors)

The following example shows how to customize the scripts used to generate
the virtual sensors state, and the content of the telemetry messages.

Similarly to the previous example, the simulation uses multiple models,
but only one (the third in this case) is customized.

Some important notes:
* When customizing the **telemetry content**, all the details must be included:
  *Interval*, *MessageTemplate*, and *MessageSchema*
* When using internal scripts (e.g. *Math.Random.WithinRange*), the
  list of fields must be included. For instance note how the customized
  *elevator-01* uses 2 scripts: one for *temperature* and *humidity*,
  and one for *vibration*

Request:
```
POST /v1/simulations
Content-Type: application/json; charset=utf-8
```
```json
{
  "Enabled": true,
  "DeviceModels": [
    {
      "Id": "truck-01",
      "Count": 1
    },
    {
      "Id": "truck-02",
      "Count": 10
    },
    {
      "Id": "elevator-01",
      "Count": 3,
      "Override": {
        "Simulation":{
          "InitialState":{
            "temperature": 70,
            "temperature_unit": "F",
            "humidity": 50,
            "humidity_unit": "psig",
            "vibration": 50,
            "vibration_unit": "hz"
          },
          "Interval":"00:00:10",
          "Scripts":[
            {
              "Type": "internal",
              "Path": "Math.Random.WithinRange",
              "Params": {
                "temperature": {
                  "Min": 70,
                  "Max": 90
                },
                "humidity": {
                  "Min": 50,
                  "Max": 60
                }
              }
            },
            {
              "Type": "internal",
              "Path": "Math.Increasing",
              "Params": {
                "vibration": {
                  "Min": 10,
                  "Max": 90
                }
              }
            }
          ]
        },
        "Telemetry": [
          {
            "Interval":"00:00:10",
            "MessageTemplate":"{\"t\":${temperature},\"t_unit\":\"${temperature_unit}\",\"h\":${humidity},\"h_unit\":\"${humidity_unit}\",\"v\":${vibration},\"v_unit\":\"${vibration_unit}\"}",
            "MessageSchema":{
              "Name":"custom-sensors;v1",
              "Format":"JSON",
              "Fields":{
                "t":"double",
                "t_unit":"text",
                "h":"double",
                "h_unit":"text",
                "v":"double",
                "v_unit":"text"
              }
            }
          }
        ]
      }
    }
  ]
}
```

## Get details of the running simulation, including device models

Request:
```
GET /v1/simulations/1
```

Response example:
```
200 OK
Content-Type: application/json; charset=utf-8
```
```json
{
  "ETag": "cec0722b205740",
  "Id": "1",
  "Enabled": true,
  "StartTime": "2019-02-03T14:00:00",
  "EndTime": "2019-02-04T00:00:00",
  "DeviceModels": [
    {
      "Id": "truck-01",
      "Count": 3
    },
    {
      "Id": "truck-02",
      "Count": 1
    },
    {
      "Id": "elevator-01",
      "Count": 10
    }
  ],
  "$metadata": {
    "$type": "Simulation;1",
    "$uri": "/v1/simulations/1",
    "$version": "1",
    "$created": "2017-05-31T00:47:18+00:00",
    "$modified": "2017-05-31T00:47:18+00:00"
  }
}
```

## Start simulation

In order to start a simulation, the `Enabled` property needs to be changed to
`true`. While it's possible to use the PUT HTTP method, and edit the entire
simulation object, the API supports also the PATCH HTTP method, so that a
client can send a smaller request. In both cases the client should send the
correct `ETag`, to manage the optimistic concurrency.

Request:
```
PATCH /v1/simulations/1
Content-Type: application/json; charset=utf-8
```
```json
{
  "ETag": "cec0722b205740",
  "Enabled": true
}
```

Response:
```
200 OK
Content-Type: application/JSON
```
```json
{
  "ETag": "8602d62c271760",
  "Id": "1",
  "Enabled": true,
  "DeviceModels": [
    {
      "Id": "truck-01",
      "Count": 3
    },
    {
      "Id": "truck-02",
      "Count": 1
    }
  ],
  "$metadata": {
    "$type": "Simulation;1",
    "$uri": "/v1/simulations/1",
    "$version": "2",
    "$created": "2017-05-31T00:47:18+00:00",
    "$modified": "2017-05-31T00:47:18+00:00"
  }
}
```

## Stop simulation

In order to stop a simulation, the `Enabled` property needs to be changed to
`false`. While it's possible to use the PUT HTTP method, and edit the entire
simulation object, the API supports also the PATCH HTTP method, so that a
client can send a smaller request. In both cases the client should send the
correct `ETag`, to manage the optimistic concurrency.

Request:
```
PATCH /v1/simulations/1
Content-Type: application/json; charset=utf-8
```
```json
{
  "ETag": "8602d62c271760",
  "Enabled": false
}
```

Response:
```
200 OK
Content-Type: application/JSON
```
```json
{
  "ETag": "930a9aea201193",
  "Id": "1",
  "Enabled": false,
  "DeviceModels": [
    {
      "Id": "truck-01",
      "Count": 3
    },
    {
      "Id": "truck-02",
      "Count": 1
    }
  ],
  "$metadata": {
    "$type": "Simulation;1",
    "$uri": "/v1/simulations/1",
    "$version": "3",
    "$created": "2017-05-31T00:47:18+00:00",
    "$modified": "2017-05-31T00:50:59+00:00"
  }
}
```

## Modifying a running simulation

Simulations can be modified by calling PUT and passing the existing
simulation ID.  This should be coupled with a GET to pull the existing
simulation content, editing it, then calling PUT with the modification(s).

When editing a simulation a client needs to send in the request the
current `ETag` value, to correctly handle concurrent requests, avoiding
the risk of data loss in case of multiple clients attempting to change
the simulation details.

However, the service supports the special ETag value `*`, that can be used
to ignore any change happened and overwrite the existing simulation.  Note
that this is meant to be used only when data loss is acceptable, e.g.
during development sessions and in test environments.

## Deleting a simulation

Simulations can be deleted using the DELETE method.

```
DELETE /v1/simulations/1
```