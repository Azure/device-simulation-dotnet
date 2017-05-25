
function readSensor(context, state) {

    // Example of parameters:
    //  context.deviceId = "a-chiller-1002"
    //  context.currentTime = "2025-09-20T13:24:59+00:00"
    //  state.lastResult = 122

    // How to parse the date
    var now = new Date(context.currentTime);

    // Some logic here, to generate a value based on the previous result
    var newResult = state.lastResult + 1;
    state.lastResult = newResult;

    // Return the new sensor state and the sensor data
    // Note: 'value' is the exposed field used in the device type configuration
    return { state: state, value: newResult };
}
