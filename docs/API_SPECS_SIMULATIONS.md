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
  "DeviceTypes": [
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
  "DeviceTypes": [
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

## Creating a simulation customizing the device models

Device models information is typically loaded from the device models
definitiond stored in the JSON files.  However, it's possible to override
some of these details, using the `override` block in the API request.
The API allows to override the telemetry frequency, the script used to
generate telemetry, and the format of the telemetry.

The following example shows how to override the telemetry frequency for
a device model sending 3 different messages (note that the order is
important):

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
            "Interval":"00:00:10"
          },
          {
            "Interval":"00:00:20"
          },
          {
            "Interval":"00:00:30"
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
    }
  ]
}
```

The following example shows how to override the script used to generate
the virtual sensors state, and the content of the telemetry messages:

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
        "Simulation":{
          "InitialState":{
            "temperature": 70,
            "temperature_unit": "F",
            "humidity": 50,
            "humidity_unit": "psig",
          },
          "Script":{
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
            },
            "Interval":"00:00:10"
          }
        },
        "Telemetry": [
          {
            "Interval":"00:00:10",
            "MessageTemplate":"{\"temperature\":${temperature},\"temperature_unit\":\"temperature\",\"humidity\":${humidity},\"humidity_unit\":\"humidity\"}",
            "MessageSchema":{
              "Name":"custom-sensors;v1",
              "Format":"JSON",
              "Fields":{
                "<name 1>":"double",
                "<name 1>_unit":"text",
                "<name 2>":"double",
                "<name 2>_unit":"text"
              }
            }
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
simulation ID.  This can be coupled with a GET to pull the existing
imulation content, editing it, then calling PUT with the modification(s).
