// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*jslint node: true*/

"use strict";

var center_latitude = 47.620433;
var center_longitude = -122.350987;

// Default state
var state = {
    online: true,
    temperature: 85.0,
    temperature_unit: "F",
    pressure: 150.0,
    pressure_unit: "psig",
    latitude: center_latitude,
    longitude: center_longitude
};

/**
 * Restore the global state using data from the previous iteration.
 *
 * @param previousState The output of main() from the previous iteration
 */
function restoreState(previousState) {
    // If the previous state is null, force a default state
    if (previousState !== undefined && previousState !== null) {
        state = previousState;
    } else {
        log("Using default state");
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
 * Entry point function called by the simulation engine.
 *
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState) {

    // Restore the global state before generating the new telemetry, so that
    // the telemetry can apply changes using the previous function state.
    restoreState(previousState);

    // 85 +/- 25%,  Min 35, Max 100
    state.temperature = vary(85, 25, 35, 100);

    return state;
}
