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
    temperature: 49.0,
    temperature_unit: "F"
};

// Default properties
var properties = {
    Latitude: 47.50066160,
    Longitude: -122.1859029
};

// Demo loop of data for truck
var data = [
    { latitude: 47.50066160, longitude: -122.1859029, altitude: 83.0, temperature: 57.2072 },
    { latitude: 47.50344484, longitude: -122.1768335, altitude: 113.0, temperature: 56.5592 },
    { latitude: 47.50530025, longitude: -122.1569494, altitude: 123.0, temperature: 56.3432 },
    { latitude: 47.50588005, longitude: -122.1413568, altitude: 136.0, temperature: 56.0624 },
    { latitude: 47.50240114, longitude: -122.1298555, altitude: 113.0, temperature: 56.5592 },
    { latitude: 47.49811017, longitude: -122.1157792, altitude: 108.0, temperature: 56.6672 },
    { latitude: 47.49845323, longitude: -122.0979256, altitude: 102.0, temperature: 56.7968 },
    { latitude: 47.49437276, longitude: -122.0852188, altitude: 108.0, temperature: 56.6672 },
    { latitude: 47.48892117, longitude: -122.0773224, altitude: 105.0, temperature: 56.7320 },
    { latitude: 47.48114878, longitude: -122.0603279, altitude: 102.0, temperature: 56.7968 },
    { latitude: 47.47906048, longitude: -122.0392136, altitude: 81.0, temperature: 57.2504 },
    { latitude: 47.48474510, longitude: -122.0271973, altitude: 62.0, temperature: 57.6608 },
    { latitude: 47.47221492, longitude: -122.0233349, altitude: 101.0, temperature: 56.8184 },
    { latitude: 47.46119052, longitude: -122.0099167, altitude: 95.0, temperature: 56.9480 },
    { latitude: 47.45260149, longitude: -121.9951538, altitude: 118.0, temperature: 56.4512 },
    { latitude: 47.44168908, longitude: -121.9850258, altitude: 132.0, temperature: 56.1488 },
    { latitude: 47.43541923, longitude: -121.9754128, altitude: 156.0, temperature: 55.6304 },
    { latitude: 47.44849993, longitude: -121.9585900, altitude: 270.0, temperature: 53.1680 },
    { latitude: 47.46019325, longitude: -121.9474034, altitude: 377.0, temperature: 50.8568 },
    { latitude: 47.46529967, longitude: -121.9345001, altitude: 406.0, temperature: 50.2304 },
    { latitude: 47.46557599, longitude: -121.9265687, altitude: 413.0, temperature: 50.0792 },
    { latitude: 47.47075370, longitude: -121.9128708, altitude: 347.0, temperature: 51.5048 },
    { latitude: 47.47353853, longitude: -121.8981079, altitude: 271.0, temperature: 53.1464 },
    { latitude: 47.48370961, longitude: -121.8891243, altitude: 234.0, temperature: 53.9456 },
    { latitude: 47.49658500, longitude: -121.8860344, altitude: 280.0, temperature: 52.9520 },
    { latitude: 47.50736331, longitude: -121.8837638, altitude: 277.0, temperature: 53.0168 },
    { latitude: 47.51080999, longitude: -121.8561367, altitude: 282.0, temperature: 52.9088 },
    { latitude: 47.50489626, longitude: -121.8252377, altitude: 194.0, temperature: 54.8096 },
    { latitude: 47.50489626, longitude: -121.8252377, altitude: 194.0, temperature: 54.8096 },
    { latitude: 47.51080999, longitude: -121.8561367, altitude: 282.0, temperature: 52.9088 },
    { latitude: 47.50736331, longitude: -121.8837638, altitude: 277.0, temperature: 53.0168 },
    { latitude: 47.49658500, longitude: -121.8860344, altitude: 280.0, temperature: 52.9520 },
    { latitude: 47.48370961, longitude: -121.8891243, altitude: 234.0, temperature: 53.9456 },
    { latitude: 47.47353853, longitude: -121.8981079, altitude: 271.0, temperature: 53.1464 },
    { latitude: 47.47075370, longitude: -121.9128708, altitude: 347.0, temperature: 51.5048 },
    { latitude: 47.46557599, longitude: -121.9265687, altitude: 413.0, temperature: 50.0792 },
    { latitude: 47.46529967, longitude: -121.9345001, altitude: 406.0, temperature: 50.2304 },
    { latitude: 47.46019325, longitude: -121.9474034, altitude: 377.0, temperature: 50.8568 },
    { latitude: 47.44849993, longitude: -121.9585900, altitude: 270.0, temperature: 53.1680 },
    { latitude: 47.43541923, longitude: -121.9754128, altitude: 156.0, temperature: 55.6304 },
    { latitude: 47.44168908, longitude: -121.9850258, altitude: 132.0, temperature: 56.1488 },
    { latitude: 47.45260149, longitude: -121.9951538, altitude: 118.0, temperature: 56.4512 },
    { latitude: 47.46119052, longitude: -122.0099167, altitude: 95.0, temperature: 56.9480 },
    { latitude: 47.47221492, longitude: -122.0233349, altitude: 101.0, temperature: 56.8184 },
    { latitude: 47.48474510, longitude: -122.0271973, altitude: 62.0, temperature: 57.6608 },
    { latitude: 47.47906048, longitude: -122.0392136, altitude: 81.0, temperature: 57.2504 },
    { latitude: 47.48114878, longitude: -122.0603279, altitude: 102.0, temperature: 56.7968 },
    { latitude: 47.48892117, longitude: -122.0773224, altitude: 105.0, temperature: 56.7320 },
    { latitude: 47.49437276, longitude: -122.0852188, altitude: 108.0, temperature: 56.6672 },
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
