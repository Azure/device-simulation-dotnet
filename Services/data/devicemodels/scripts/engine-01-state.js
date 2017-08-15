// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*jslint node: true*/

"use strict";

// Default state
var state = {
    online: true,
    fuellevel: 70.0,
    fuellevel_unit: "Gal",
    coolant: 7500.0,
    coolant_unit: "ohm",
    vibration: 10.0,
    vibration_unit: "mm"
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
    var value =  avg * (1 + ((percentage / 100) * (2 * Math.random() - 1)));
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

    // 7500 +/- 5%,  Min 200, Max 10000
    state.coolant = vary(7500, 5, 200, 10000);

    // 10 +/- 5%,  Min 0, Max 20
    state.vibration = vary(10, 5, 0, 20);

    return state;
}
