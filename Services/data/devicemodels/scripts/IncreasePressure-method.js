// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global sleep*/
/*jslint node: true*/

"use strict";

// Default state
var state = {
    pressure: 250.0,
    CalculateRandomizedTelemetry: true
};

/**
 * Restore the global state using data from the previous iteration.
 *
 * @param previousState The output of main() from the previous iteration
 */
function restoreState(previousState) {
    // If the previous state is null, force a default state
    if (previousState !== undefined && previousState !== null) {
        // copy, individual values
        state.pressure = previousState.pressure;
    } else {
        log("Using default state");
    }
}

/**
 * Entry point function called by the simulation engine.
 *
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState) {

    // Reboot - devices goes offline and comes online after 20 seconds
    log("Executing DecreasePressure simulation function.");
    state.pressure = 250;
    state.CalculateRandomizedTelemetry = false;
    // update the state to 250
    updateState(state);

    return state;
}
