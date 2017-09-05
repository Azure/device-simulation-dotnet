// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global UpdateState(state)*/
/*jslint node: true*/

"use strict";

// Default state
var state = {
    temperature: 75.0
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
    state.pressure = 150;
    // update the state to 150
    updateState(state);

    return state;
}
