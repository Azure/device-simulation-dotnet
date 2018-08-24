API specifications - DeviceModelScripts
=======================================

Device model scripts are used by simulated devices.

For each simulated device there are two types of scripts:

1. Behavior scripts: defines the behavior of an enabled simulated 
device including initial states.
2. Method scripts: defines the behavior or states changes of an 
enabled simulated device after a C2D method been triggered.

Device model scripts can be compiled into the service or uploaded into 
storage with the associated device model.

## Uploading device model script files

### Uploading file with POST

When invoking the API using the POST HTTP method, the service will always
attempt to create a new device model script model with the content of the
uploaded script file.

Request:
```
POST /v1/devicemodelscripts
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
  "Content": "// ... javascript code here ..."
  "Path": "Storage",
  "$metadata": {
    "$type": "DeviceModelScript;1",
    "$uri": "/v1/devicemodelscripts/b62d3316-effe-41d4-8767-e0ca6d07f013",
    "$created": "2018-07-31T21:24:24+00:00",
    "$modified": "2018-07-31T21:24:24+00:00"
  }
}
```

### Editing with PUT

When invoking the API using the PUT HTTP method, the service will attempt to
modify an existing device model script. When using PUT, the device model script
Id is passed through the URL. PUT requests are idempotent and don't generate 
errors when retried (unless the script was modified during the request by 
another user, in which case the ETag mismatch will generate an error).

```
PUT /v1/devicemodelscripts/53009673-6c49-4514-9dbd-5f811723c195 HTTP/1.1
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
  "Content": "// ... javascript code here ..."
  "Path": "Storage"
}
```

## Get device model scripts

### Get a list of device model scripts

Request:
```
GET /v1/devicemodelscripts
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
      "Content": "// ... javascript code here ..."
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
      "Content": "// ... javascript code here ..."
	  "Path": "Storage",
      "$metadata": {
        "$type": "DeviceModelScript;1",
        "$uri": "/v1/devicemodelscripts/f1943029-a1af-4952-bdf6-55a8311e0fab",
        "$created": "2018-07-31T22:40:49+00:00",
        "$modified": "2018-07-31T22:40:49+00:00"
      }
    }
  ],
  "$metadata": {
    "$type": "DeviceModelScriptList;1",
    "$uri": "/v1/devicemodelscripts"
  }
}
```

### Get a device model script by id

Request:
```
GET /v1/devicemodelscripts/{id}
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
  "Content": "// ... javascript code here ..."
  "Path": "Storage",
  "$metadata": {
    "$type": "DeviceModelScript;1",
    "$uri": "/v1/devicemodelscripts/b62d3316-effe-41d4-8767-e0ca6d07f013",
    "$created": "2018-07-31T21:24:24+00:00",
    "$modified": "2018-07-31T21:24:24+00:00"
  }
}
```

## Deleting a device model script

Device model scripts can be deleted using the DELETE method.

Request:
```
DELETE /v1/devicemodelscripts/{id}
```
Response:
```
200 OK
```
