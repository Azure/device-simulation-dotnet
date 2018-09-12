// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global updateProperty*/
/*jslint node: true*/

"use strict";

// Default state
var state = {
    online: true,
    altitude: 83.0,
    current_data_index: 0,
    latitude: 47.50066160,
    longitude: -122.1859029,
    speed: 80.0,
    speed_unit: "mph",
    temperature: 38.0,
    temperature_unit: "F"
};

// Default properties
// Note: property names are case sensitive
var properties = {
    Latitude: 47.50066160,
    Longitude: -122.1859029
};

// Demo loop of data for truck
var data = [
    { latitude: 47.50066160, longitude: -122.1859029, altitude: 83.0, temperature: 57.2072 },
    { latitude: 47.50344484, longitude: -122.1768335, altitude: 113.0, temperature: 56.5592 },
    { latitude: 47.50530025, longitude: -122.1569494, altitude: 123.0, temperature: 56.3432 },
    { latitude: 47.50530025, longitude: -122.1569494, altitude: 123.0, temperature: 56.3432 },
    { latitude: 47.50588005, longitude: -122.1413568, altitude: 136.0, temperature: 56.0624 },
    { latitude: 47.50240114, longitude: -122.1298555, altitude: 113.0, temperature: 56.5592 },
    { latitude: 47.49811017, longitude: -122.1157792, altitude: 108.0, temperature: 56.6672 },
    { latitude: 47.49845323, longitude: -122.0979256, altitude: 102.0, temperature: 56.7968 },
    { latitude: 47.51028147, longitude: -122.0863957, altitude: 125.0, temperature: 56.3000 },
    { latitude: 47.51631052, longitude: -122.0721478, altitude: 134.0, temperature: 56.1056 },
    { latitude: 47.52593239, longitude: -122.0644230, altitude: 69.0, temperature: 57.5096 },
    { latitude: 47.53636374, longitude: -122.0627064, altitude: 36.0, temperature: 58.2224 },
    { latitude: 47.54783583, longitude: -122.0613331, altitude: 16.0, temperature: 58.6544 },
    { latitude: 47.54575018, longitude: -122.0540089, altitude: 16.0, temperature: 58.6544 },
    { latitude: 47.54383827, longitude: -122.0431943, altitude: 20.0, temperature: 58.5680 },
    { latitude: 47.53647963, longitude: -122.0326371, altitude: 29.0, temperature: 58.3736 },
    { latitude: 47.53149603, longitude: -122.0232672, altitude: 49.0, temperature: 57.9416 },
    { latitude: 47.53636374, longitude: -122.0088477, altitude: 94.0, temperature: 56.9696 },
    { latitude: 47.53253915, longitude: -121.9981188, altitude: 130.0, temperature: 56.1920 },
    { latitude: 47.53068471, longitude: -121.9867034, altitude: 150.0, temperature: 55.7600 },
    { latitude: 47.53549454, longitude: -121.9714112, altitude: 164.0, temperature: 55.4576 },
    { latitude: 47.53242325, longitude: -121.9557042, altitude: 151.0, temperature: 55.7384 },
    { latitude: 47.52813473, longitude: -121.9404263, altitude: 164.0, temperature: 55.4576 },
    { latitude: 47.51944072, longitude: -121.9279809, altitude: 140.0, temperature: 55.9760 },
    { latitude: 47.50968239, longitude: -121.9089264, altitude: 199.0, temperature: 54.7016 },
    { latitude: 47.50713140, longitude: -121.8903727, altitude: 276.0, temperature: 53.0384 },
    { latitude: 47.50736331, longitude: -121.8837638, altitude: 277.0, temperature: 53.0168 },
    { latitude: 47.51080999, longitude: -121.8561367, altitude: 282.0, temperature: 52.9088 },
    { latitude: 47.50489626, longitude: -121.8252377, altitude: 194.0, temperature: 54.8096 },
    { latitude: 47.50489626, longitude: -121.8252377, altitude: 194.0, temperature: 54.8096 },
    { latitude: 47.51080999, longitude: -121.8561367, altitude: 282.0, temperature: 52.9088 },
    { latitude: 47.50736331, longitude: -121.8837638, altitude: 277.0, temperature: 53.0168 },
    { latitude: 47.50713140, longitude: -121.8903727, altitude: 276.0, temperature: 53.0384 },
    { latitude: 47.50968239, longitude: -121.9089264, altitude: 199.0, temperature: 54.7016 },
    { latitude: 47.51944072, longitude: -121.9279809, altitude: 140.0, temperature: 55.9760 },
    { latitude: 47.52813473, longitude: -121.9404263, altitude: 164.0, temperature: 55.4576 },
    { latitude: 47.53242325, longitude: -121.9557042, altitude: 151.0, temperature: 55.7384 },
    { latitude: 47.53549454, longitude: -121.9714112, altitude: 164.0, temperature: 55.4576 },
    { latitude: 47.53068471, longitude: -121.9867034, altitude: 150.0, temperature: 55.7600 },
    { latitude: 47.53253915, longitude: -121.9981188, altitude: 130.0, temperature: 56.1920 },
    { latitude: 47.53636374, longitude: -122.0088477, altitude: 94.0, temperature: 56.9696 },
    { latitude: 47.53149603, longitude: -122.0232672, altitude: 49.0, temperature: 57.9416 },
    { latitude: 47.53647963, longitude: -122.0326371, altitude: 29.0, temperature: 58.3736 },
    { latitude: 47.54383827, longitude: -122.0431943, altitude: 20.0, temperature: 58.5680 },
    { latitude: 47.54575018, longitude: -122.0540089, altitude: 16.0, temperature: 58.6544 },
    { latitude: 47.54783583, longitude: -122.0613331, altitude: 16.0, temperature: 58.6544 },
    { latitude: 47.53636374, longitude: -122.0627064, altitude: 36.0, temperature: 58.2224 },
    { latitude: 47.52593239, longitude: -122.0644230, altitude: 69.0, temperature: 57.5096 },
    { latitude: 47.51631052, longitude: -122.0721478, altitude: 134.0, temperature: 56.1056 },
    { latitude: 47.51028147, longitude: -122.0863957, altitude: 125.0, temperature: 56.3000 },
    { latitude: 47.49845323, longitude: -122.0979256, altitude: 102.0, temperature: 56.7968 },
    { latitude: 47.49811017, longitude: -122.1157792, altitude: 108.0, temperature: 56.6672 },
    { latitude: 47.50240114, longitude: -122.1298555, altitude: 113.0, temperature: 56.5592 },
    { latitude: 47.50588005, longitude: -122.1413568, altitude: 136.0, temperature: 56.0624 },
    { latitude: 47.50530025, longitude: -122.1569494, altitude: 123.0, temperature: 56.3432 },
    { latitude: 47.50344484, longitude: -122.1768335, altitude: 113.0, temperature: 56.5592 },
    { latitude: 47.50066160, longitude: -122.1859029, altitude: 83.0, temperature: 57.2072 }
];

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
    var value = avg * (1 + ((percentage / 100) * (2 * Math.random() - 1)));
    value = Math.max(value, min);
    value = Math.min(value, max);
    return value;
}

/**
 * Returns the next data point in the predefined data set.
 * Loops back to start if the end of the list has been reached.
 */
function getNextMessage() {
    if (state.current_data_index === data.length - 1) {
        state.current_data_index = 0;
    } else {
        state.current_data_index += 1;
    }
    return data[state.current_data_index];
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

    // Get the next data point in the demo loop
    var nextMessage = getNextMessage();
    state.latitude = nextMessage.latitude;
    state.longitude = nextMessage.longitude;

    // Apply some variability to the pre-defined values
    state.altitude = vary(nextMessage.altitude, 5, nextMessage.altitude - 3, nextMessage.altitude + 3);
    state.temperature = vary(nextMessage.temperature, 5, nextMessage.temperature - 1, nextMessage.temperature + 1);

    // 25 +/- 5%,  Min 0, Max 80
    state.speed = vary(25, 5, 0, 80);

    updateState(state);
    updateProperty("Latitude", nextMessage.latitude);
    updateProperty("Longitude", nextMessage.longitude);
}
