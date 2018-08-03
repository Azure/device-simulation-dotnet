API specifications - SimulationScripts
================================

## Uploading simulation script files

### Uploading file with POST

When invoking the API using the POST HTTP method, the service will always
attempt to create a new simulation script model with the content of the
uploadedscirpt file.

Request:
```
POST /v1/simulationscripts
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW

------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="file"; filename="Reboot-method.js"
Content-Type: application/javascript


------WebKitFormBoundary7MA4YWxkTrZu0gW--
```

Response:
```
200 OK
Content-Type: application/json; charset=utf-8
```
```json
{
  "ETag": "\"00000300-0000-0000-0000-5b60d3870000\"",
  "Id": "b62d3316-effe-41d4-8767-e0ca6d07f013",
  "Type": "javascript",
  "Name": "Reboot-method.js",
  "Content": "// Copyright (c) Microsoft. All rights reserved.\n\n/*global log*/\n/*global updateState*/\n/*global sleep*/\n/*jslint node: true*/\n\n\"use strict\";\n\n// Default state\nvar state = {\n    // reboot just changes whether the device is on or offline\n    online: true\n};\n\n/**\n * Entry point function called by the method.\n *\n * @param context        The context contains current time, device model and id\n * @param previousState  The device state since the last iteration\n * @param previousProperties  The device properties since the last iteration\n */\n/*jslint unparam: true*/\nfunction main(context, previousState, previousProperties) {\n\n    // Reboot - devices goes offline and comes online after 20 seconds\n    log(\"Executing 'Reboot' JavaScript method simulation.\");\n\n    state.DeviceMethodStatus = \"Rebooting device...\";\n    state.CalculateRandomizedTelemetry = false;\n    state.online = false;\n    // update the state to offline\n    updateState(state);\n\n    // Sleep for 15 seconds\n    sleep(15000);\n\n    state.DeviceMethodStatus = \"Successfully rebooted device.\";\n    updateState(state);\n\n    // Sleep for 5 seconds\n    sleep(5000);\n    state.CalculateRandomizedTelemetry = true;\n    // update the state back to online\n    state.online = true;\n    state.DeviceMethodStatus = \"\";\n    updateState(state);\n\n    log(\"'Reboot' JavaScript method simulation completed.\");\n}\n",
  "Path": "Storage",
  "$metadata": {
    "$type": "SimulationScript;1",
    "$uri": "/v1/simulationscripts/b62d3316-effe-41d4-8767-e0ca6d07f013",
    "$created": "2018-07-31T21:24:24+00:00",
    "$modified": "2018-07-31T21:24:24+00:00"
  }
}
```

### Editing with PUT

When invoking the API using the PUT HTTP method, the service will attempt
to modify an existing simulation script. When using PUT, the simulation 
script Id is passed through the URL. PUT requests are idempotent and don't
generate errors when retried (unless the payload differs during a retry, 
in which case the ETag mismatch will generate an error).

```
PUT /v1/simulationscripts/53009673-6c49-4514-9dbd-5f811723c195 HTTP/1.1
Host: localhost:9003
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW
Cache-Control: no-cache
Postman-Token: f5b9c0b9-1ce7-4996-a5e1-431967fc3aa4

------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="file"; filename="Reboot-method.js"
Content-Type: application/javascript


------WebKitFormBoundary7MA4YWxkTrZu0gW
Content-Disposition: form-data; name="Etag"

"b20375e9-0000-0000-0000-5b5914650000"
------WebKitFormBoundary7MA4YWxkTrZu0gW--
```
```json
{
  "ETag": "\"01004b86-0000-0000-0000-0105b6004c86\"",
  "Id": "b62d3316-effe-41d4-8767-e0ca6d07f013",
  "Type": "javascript",
  "Name": "Reboot-method.js",
  "Content": "// Copyright (c) Microsoft. All rights reserved.\n\n/*global log*/\n/*global updateState*/\n/*global sleep*/\n/*jslint node: true*/\n\n\"use strict\";\n\n// Default state\nvar state = {\n    // reboot just changes whether the device is on or offline\n    online: true\n};\n\n/**\n * Entry point function called by the method.\n *\n * @param context        The context contains current time, device model and id\n * @param previousState  The device state since the last iteration\n * @param previousProperties  The device properties since the last iteration\n */\n/*jslint unparam: true*/\nfunction main(context, previousState, previousProperties) {\n\n    // Reboot - devices goes offline and comes online after 20 seconds\n    log(\"Executing 'Reboot' JavaScript method simulation.\");\n\n    state.DeviceMethodStatus = \"Rebooting device...\";\n    state.CalculateRandomizedTelemetry = false;\n    state.online = false;\n    // update the state to offline\n    updateState(state);\n\n    // Sleep for 15 seconds\n    sleep(15000);\n\n    state.DeviceMethodStatus = \"Successfully rebooted device.\";\n    updateState(state);\n\n    // Sleep for 5 seconds\n    sleep(5000);\n    state.CalculateRandomizedTelemetry = true;\n    // update the state back to online\n    state.online = true;\n    state.DeviceMethodStatus = \"\";\n    updateState(state);\n\n    log(\"'Reboot' JavaScript method simulation completed.\");\n}\n",
  "Path": "Storage",
  "$metadata": {
    "$type": "SimulationScript;1",
    "$uri": "/v1/simulationscripts/b62d3316-effe-41d4-8767-e0ca6d07f013",
    "$created": "2018-07-31T21:24:24+00:00",
    "$modified": "2018-07-31T22:24:24+00:00"
  }
}
```

## Get simulation scripts

### Get a list of simulation scripts

Request:
```
GET /v1/simulationscripts
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
      "ETag": "\"01004b86-0000-0000-0000-5b60e56f0000\"",
      "Id": "371e7ef7-9d99-4beb-a934-f3914faa02a3",
      "Type": "javascript",
      "Name": "FirmwareUpdate-method.js",
      "Content": "// Copyright (c) Microsoft. All rights reserved.\n\n/*global log*/\n/*global updateState*/\n/*global updateProperty*/\n/*global sleep*/\n/*jslint node: true*/\n\n\"use strict\";\n\n// Default state\nvar state = {\n    online: true\n};\n\n// Default device properties\nvar properties = {\n    Firmware: \"1.0.0\",\n    FirmwareUpdateStatus: \"Updating Firmware\"\n};\n\n/**\n * Restore the global state using data from the previous iteration.\n *\n * @param previousState device state from the previous iteration\n * @param previousProperties device properties from the previous iteration\n */\nfunction restoreSimulation(previousState, previousProperties) {\n    // If the previous state is null, force a default state\n    if (previousState) {\n        state = previousState;\n    } else {\n        log(\"Using default state\");\n    }\n\n    if (previousProperties) {\n        properties = previousProperties;\n    } else {\n        log(\"Using default properties\");\n    }\n}\n\n/**\n * Entry point function called by the simulation engine.\n *\n * @param context        The context contains current time, device model and id, not used\n * @param previousState  The device state since the last iteration, not used\n * @param previousProperties  The device properties since the last iteration\n */\n/*jslint unparam: true*/\nfunction main(context, previousState, previousProperties) {\n\n    // Restore the global device properties and the global state before\n    // generating the new telemetry, so that the telemetry can apply changes\n    // using the previous function state.\n    restoreSimulation(previousState, previousProperties);\n\n    // Reboot - devices goes offline and comes online after 20 seconds\n    log(\"Executing 'FirmwareUpdate' JavaScript method; Firmware version passed:\" + context.Firmware);\n\n    var DevicePropertyKey = \"FirmwareUpdateStatus\";\n    var FirmwareKey = \"Firmware\";\n\n    // update the status to offline & firmware updating\n    state.online = false;\n    state.CalculateRandomizedTelemetry = false;\n    var status = \"Command received, updating firmware version to \";\n    status = status.concat(context.Firmware);\n    updateProperty(DevicePropertyKey, status);\n    sleep(5000);\n\n    log(\"Image Downloading...\");\n    status = \"Image Downloading...\";\n    updateProperty(DevicePropertyKey, status);\n    sleep(7500);\n\n    log(\"Executing firmware update simulation function, firmware version passed:\" + context.Firmware);\n    status = \"Downloaded, applying firmware...\";\n    updateProperty(DevicePropertyKey, status);\n    sleep(5000);\n\n    status = \"Rebooting...\";\n    updateProperty(DevicePropertyKey, status);\n    sleep(5000);\n\n    status = \"Firmware Updated.\";\n    updateProperty(DevicePropertyKey, status);\n    properties.Firmware = context.Firmware;\n    updateProperty(FirmwareKey, context.Firmware);\n    sleep(7500);\n\n    state.CalculateRandomizedTelemetry = true;\n    state.online = true;\n    updateState(state);\n\n    log(\"'FirmwareUpdate' JavaScript method simulation completed.\");\n}\n",
      "Path": "Storage",
      "$metadata": {
        "$type": "SimulationScript;1",
        "$uri": "/v1/simulationscripts/371e7ef7-9d99-4beb-a934-f3914faa02a3",
        "$created": "2018-07-31T22:40:48+00:00",
        "$modified": "2018-07-31T22:40:48+00:00"
      }
    },
    {
      "ETag": "\"01005386-0000-0000-0000-5b60e5700000\"",
      "Id": "f1943029-a1af-4952-bdf6-55a8311e0fab",
      "Type": "javascript",
      "Name": "IncreasePressure-method.js",
      "Content": "// Copyright (c) Microsoft. All rights reserved.\n\n/*global log*/\n/*global updateState*/\n/*global sleep*/\n/*jslint node: true*/\n\n\"use strict\";\n\n/**\n * Entry point function called by the simulation engine.\n *\n * @param context        The context contains current time, device model and id, not used\n * @param previousState  The device state since the last iteration, not used\n * @param previousProperties  The device properties since the last iteration\n */\n/*jslint unparam: true*/\nfunction main(context, previousState, previousProperties) {\n\n    log(\"Executing 'IncreasePressure' JavaScript method simulation (5 seconds).\");\n\n    // Pause the simulation and change the simulation mode so that the\n    // pressure will fluctuate at ~250 when it resumes\n    var state = {\n        simulation_state: \"high_pressure\",\n        CalculateRandomizedTelemetry: false\n    };\n    updateState(state);\n\n    // Increase\n    state.pressure = 170;\n    updateState(state);\n    log(\"Pressure increased to \" + state.pressure);\n    sleep(1000);\n\n    // Increase\n    state.pressure = 190;\n    updateState(state);\n    log(\"Pressure increased to \" + state.pressure);\n    sleep(1000);\n\n    // Increase\n    state.pressure = 210;\n    updateState(state);\n    log(\"Pressure increased to \" + state.pressure);\n    sleep(1000);\n\n    // Increase\n    state.pressure = 230;\n    updateState(state);\n    log(\"Pressure increased to \" + state.pressure);\n    sleep(1000);\n\n    // Increase\n    state.pressure = 250;\n    updateState(state);\n    log(\"Pressure increased to \" + state.pressure);\n    sleep(1000);\n\n    // Resume the simulation\n    state.CalculateRandomizedTelemetry = true;\n    updateState(state);\n\n    log(\"'IncreasePressure' JavaScript method simulation completed.\");\n}\n",
      "Path": "Storage",
      "$metadata": {
        "$type": "SimulationScript;1",
        "$uri": "/v1/simulationscripts/f1943029-a1af-4952-bdf6-55a8311e0fab",
        "$created": "2018-07-31T22:40:49+00:00",
        "$modified": "2018-07-31T22:40:49+00:00"
      }
    }
  ],
  "$metadata": {
    "$type": "SimulationScriptList;1",
    "$uri": "/v1/simulationscripts"
  }
}
```

### Get a simulation script by id

Request:
```
GET /v1/simulationscripts/${id}
```

Response example:
```
200 OK
Content-Type: application/json; charset=utf-8
```
```json
{
  "ETag": "\"00000300-0000-0000-0000-5b60d3870000\"",
  "Id": "b62d3316-effe-41d4-8767-e0ca6d07f013",
  "Type": "javascript",
  "Name": "Reboot-method.js",
  "Content": "// Copyright (c) Microsoft. All rights reserved.\n\n/*global log*/\n/*global updateState*/\n/*global sleep*/\n/*jslint node: true*/\n\n\"use strict\";\n\n// Default state\nvar state = {\n    // reboot just changes whether the device is on or offline\n    online: true\n};\n\n/**\n * Entry point function called by the method.\n *\n * @param context        The context contains current time, device model and id\n * @param previousState  The device state since the last iteration\n * @param previousProperties  The device properties since the last iteration\n */\n/*jslint unparam: true*/\nfunction main(context, previousState, previousProperties) {\n\n    // Reboot - devices goes offline and comes online after 20 seconds\n    log(\"Executing 'Reboot' JavaScript method simulation.\");\n\n    state.DeviceMethodStatus = \"Rebooting device...\";\n    state.CalculateRandomizedTelemetry = false;\n    state.online = false;\n    // update the state to offline\n    updateState(state);\n\n    // Sleep for 15 seconds\n    sleep(15000);\n\n    state.DeviceMethodStatus = \"Successfully rebooted device.\";\n    updateState(state);\n\n    // Sleep for 5 seconds\n    sleep(5000);\n    state.CalculateRandomizedTelemetry = true;\n    // update the state back to online\n    state.online = true;\n    state.DeviceMethodStatus = \"\";\n    updateState(state);\n\n    log(\"'Reboot' JavaScript method simulation completed.\");\n}\n",
  "Path": "Storage",
  "$metadata": {
    "$type": "SimulationScript;1",
    "$uri": "/v1/simulationscripts/b62d3316-effe-41d4-8767-e0ca6d07f013",
    "$created": "2018-07-31T21:24:24+00:00",
    "$modified": "2018-07-31T21:24:24+00:00"
  }
}
```

## Deleting a simulation script

Simulation scripts can be deleted using the DELETE method.

Request:
```
DELETE /v1/simulationscripts/{id}
```
Response:
```
200 OK
```