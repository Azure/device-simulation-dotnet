Device Models
=============

Each **Simulated Device** belongs to a specific **Device Model**, which
defines the simulation behavior, for example how frequently telemetry is
sent, what kind of messages to send, which methods are supported, etc.

**Device models** are defined using a set of JSON configuration files,
(**one file for each device model**), and a set of **Javascript files
defining the simulation behavior, e.g. the random telemetry, the method logic,
etc. A typical simulation service should have:
* One JSON file for each device model (e.g. elevator.json)
* One Javascript file for each device model (e.g. elevator-state.js)
* One Javascript file for each device method (e.g. elevator-go-down.js)

The [DeviceModels folder](https://github.com/Azure/device-simulation-dotnet/tree/master/Services/data/DeviceModels)
contains some examples of these files, showing how to define a device model
and how to simulate a device behavior.

**Device Model JSON example**

```json
{
    "SchemaVersion": "1.0.0",
    "Id": "elevator-01",
    "Version": "0.0.1",
    "Name": "Elevator",
    "Description": "Elevator with floor, vibration and temperature sensors.",
    "Protocol": "AMQP",
    "Simulation": {
        "InitialState": {
            "online": true,
            "floor": 1,
            "vibration": 10.0,
            "vibration_unit": "mm",
            "temperature": 75.0,
            "temperature_unit": "F"
        },
        "Interval": "00:00:10",
        "Scripts": [
            {
                "Type": "javascript",
                "Path": "elevator-01-state.js"
            }
        ]
    },
    "Properties": {
        "Type": "Elevator",
        "Location": "Building 40",
        "Latitude": 47.636369,
        "Longitude": -122.133132
    },
    "Telemetry": [
        {
            "Interval": "00:00:10",
            "MessageTemplate": "{\"floor\":${floor},\"vibration\":${vibration},\"vibration_unit\":\"${vibration_unit}\",\"temperature\":${temperature},\"temperature_unit\":\"${temperature_unit}\"}",
            "MessageSchema": {
                "Name": "elevator-sensors;v1",
                "Format": "JSON",
                "Fields": {
                    "floor": "integer",
                    "vibration": "double",
                    "vibration_unit": "text",
                    "temperature": "double",
                    "temperature_unit": "text"
                }
            }
        }
    ],
    "CloudToDeviceMethods": {
        "FirmwareUpdate": {
            "Type": "javascript",
            "Path": "FirmwareUpdate-method.js"
        },
        "StopElevator": {
            "Type": "javascript",
            "Path": "StopElevator-method.js"
        },
        "StartElevator": {
            "Type": "javascript",
            "Path": "StartElevator-method.js"
        }
    }
}
```

**Device Model Javascript simulation example**

```javascript
/*global log*/
/*global updateState*/
/*global updateProperty*/
/*jslint node: true*/

"use strict";

var floors = 15;

// Default state
var state = {
    online: true,
    floor: 1,
    vibration: 10.0,
    vibration_unit: "mm",
    temperature: 75.0,
    temperature_unit: "F",
    moving: true
};

// Default properties
var properties = {};

/**
 * Restore the global state using data from the previous iteration.
 *
 * @param previousState device state from the previous iteration
 * @param previousProperties device properties from the previous iteration
 */
function restoreSimulation(previousState, previousProperties) {
    // If the previous state is null, force a default state
    if (previousState) {
        state = previousState;
    } else {
        log("Using default state");
    }

    if (previousProperties) {
        properties = previousProperties;
    } else {
        log("Using default properties");
    }
}

/**
 * Simple formula generating a random value around the average
 * in between min and max
 */
function vary(avg, percentage, min, max) {
    var value =  avg * (1 + ((percentage / 100) * (2 * Math.random() - 1)));
    value = Math.max(value, min);
    value = Math.min(value, max);
    return value;
}

function varyfloor(current, min, max) {
    if (current === min) {
        return current + 1;
    }
    if (current === max) {
        return current - 1;
    }
    if (Math.random() < 0.5) {
        return current - 1;
    }
    return current + 1;
}

/**
 * Entry point function called by the simulation engine.
 * Returns updated simulation state.
 * Device property updates must call updateProperties() to persist.
 *
 * @param context             The context contains current time, device model and id
 * @param previousState       The device state since the last iteration
 * @param previousProperties  The device properties since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState, previousProperties) {

    // Restore the global device properties and the global state before
    // generating the new telemetry, so that the telemetry can apply changes
    // using the previous function state.
    restoreSimulation(previousState, previousProperties);

    if (state.moving) {
        state.floor = varyfloor(state.floor, 1, floors);
        // 10 +/- 5%,  Min 0, Max 20
        state.vibration = vary(10, 5, 0, 20);
    } else {
        state.vibration = 0;
    }

    // 75 +/- 1%,  Min 25, Max 100
    state.temperature = vary(75, 1, 25, 100);

    updateState(state);
}
```

Thanks to these files, it is possible to add new device models and customize
the simulation behavior, without rebuilding the service. To add new types
or to change the simulated behavior, edit the relevant *.json and *.js
files and redeploy the device simulation microservice.

Each device model file contains the definition of the simulated device model,
including the following information:

* Device model name: string
* Protocol: AMQP | MQTT | HTTP
* The initial device state
* How often to refresh the device state
* Which Javascript file to use to refresh the device state
* A list of telemetry messages to send, each with a specific frequency
* The schema of the telemetry messages, used by backend application to
  parse the telemetry received
* A list of supported methods and the Javascript file to use to simulate
  each method.

Check the
[DeviceModels folder](https://github.com/Azure/device-simulation-dotnet/tree/master/Services/data/DeviceModels)
for some examples.

## Device Model files

#### File schema

The schema version is always "1.0.0" and is specific to the format of this
file:
```json
"SchemaVersion": "1.0.0"
```

#### Device model description

The following properties describe the device model. Each type has a unique
identifier, a semantic version, a name and a description:
```json
"Id": "chiller-01",
"Version": "0.0.1",
"Name": "Chiller",
"Description": "Chiller with external temperature, humidity and pressure sensors."
```

#### IoT Protocol

IoT devices can connect using different protocols. The simulation allows to
use either **AMQP, MQTT or HTTP**:
```json
"Protocol": "AMQP"
```

#### Simulated device state

Each simulated device has an internal state, which needs to be defined. The
state also defines the properties that can be reported in telemetry. For
example an elevator might have an initial state like:

```
"InitialState": {
    "floor": 1
},
```

while a moving device with multiple sensors might have more properties, like:

```
"InitialState": {
    "latitude": 47.445301,
    "longitude": -122.296307,
    "speed": 30.0,
    "speed_unit": "mph",
    "temperature": 38.0,
    "temperature_unit": "F",
    "moving": false
}
```

The device state is kept in memory by the simulation service, and provided in
input to the Javascript function. The Javascript function can decide to ignore
the state and generate some random data, or to update the device status in
some *realistic* way, given a desired scenario.

The function generating the state receives in input also the Device Id, the
Device Model and the Current Time, so it is possible to generate different
data by device and by time if required.

#### Generating telemetry messages

The simulation service can send multiple messages for each device, and each
message can be sent at a different frequency. Typically, a telemetry will
send a message including some data from the device state. For example, a
simulated room might send information about temperature and humidity every
10 seconds, and lights status once per minute. Note the placeholders, which
are automatically replaced with values from the device state:

```
"Telemetry": [
    {
        "Interval": "00:00:10",
        "MessageTemplate":
            "{\"temperature\":${temperature},\"temperature_unit\":\"${temperature_unit}\",\"humidity\":\"${humidity}\"}",
        "MessageSchema": {
            "Name": "RoomComfort;v1",
            "Format": "JSON",
            "Fields": {
                "temperature": "double",
                "temperature_unit": "text",
                "humidity": "integer"
            }
        }
    },
    {
        "Interval": "00:01:00",
        "MessageTemplate": "{\"lights\":${lights_on}}",
        "MessageSchema": {
            "Name": "RoomLights;v1",
            "Format": "JSON",
            "Fields": {
                "lights": "boolean"
            }
        }
    }
],
```

The placeholders use a special syntax `${NAME}` where `NAME` is a key from
the **device state object** returned by the Javascript `main()` function.
Note that strings should be quoted properly, while numbers should not.

##### Message schema

Each message type must have a well defined schema. The message schema is
also published to IoT Hub, so that backend applications can reuse the
information to interpret the incoming telemetry.

The schema supports JSON format, which allows for easy parsing,
transformation and analytics, across several systems and services.

The fields listed in the schema can be of the following types:

* Object - serialized using JSON
* Binary - serialized using base64
* Text
* Boolean
* Integer
* Double
* DateTime

#### Supported methods

Simulated devices can also react to method calls, in which case they will
execute some logic and provide some response. Similarly to the simulation,
the method logic is stored in a Javascript file, and can interact with the
device state. For example:

```
"CloudToDeviceMethods": {
    "FirmwareUpdate": {
        "Type": "javascript",
        "Path": "FirmwareUpdate-method.js"
    }
}
```

## Function script files

Functions are stored  in Javascript files, which are loaded and executed at
runtime, using [Jint](https://github.com/sebastienros/jint), a Javascript
interpreter for .NET.

The Javascript files must have a `main` function, and accept three parameters:
1. a context object which contains two properties:
    1. `currentTime` as a string with format `yyyy-MM-dd'T'HH:mm:sszzz`
    2. `deviceId`, e.g. "Simulated.Elevator.123"
    3. `deviceModel`, e.g. "Elevator"
2. a `previousState` object, which contains state values that may have been previously set by a JavaScript function call.
3. a `previousProperties` object, which contains property values that may have been previously set by a JavaScript function call.

The `main` function returns the new device state. Example:

```javascript
function main(context, previousState, previousProperties) {

    // Use context if the simulation depends on
    // time or device details.
    // Execute some logic, updating 'state'

    updateState(state);
}
```

#### Debugging script files

While it's not possible to attach a debugger to the Javascript interpreter,
it is possible to log information in the service log. For convenience, the
application provides a `log()` function which can be used to save information
useful to track and debug the function execution. In cases of syntax errors,
the interpreter will fail, and the service log will contain some information
about the `Jint.Runtime.JavaScriptException` exception occurred.

Logging example:

```javascript
function main(context, previousState, previousProperties) {

    log("This message will appear in the service logs.");

    log(context.deviceId);

    if (typeof(state) !== "undefined" && state !== null) {
        log("Previous value: " + state.temperature);
    }

    // ...

    updateState(state);
}
```
