// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*jslint node: true*/

"use strict";

// Default state
var state = {
    online: true,
    temperature: 75.0,
    temperature_unit: "F",
    humidity: 70.0,
    humidity_unit: "%",
    pressure: 250.0,
    pressure_unit: "psig"
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

function sleep(delay) {
    //TODO: There must be a sleep function that doesn't spin the CPU?
    var start = new Date().getTime();
    while (new Date().getTime() < start + delay);
}

/**
 * Entry point function called by the simulation engine.
 *
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState) {

    //Reboot - devices goes offline and comes online after 20 seconds
    log("Executing reboot simulation function.");

    // Restore the global state before generating the new telemetry, so that
    // the telemetry can apply changes using the previous function state.
    restoreState(previousState);

    state.online = "False";

    //update the state to offline
    UpdateState(state);

    //Sleep for 20 seconds
    sleep(20000);

    state.online = "True";
    //update the state back to online
    UpdateState(state);
    
    return state;
}
