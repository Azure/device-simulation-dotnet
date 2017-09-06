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
 * Entry point function called by the simulation engine.
 *
 * @param context        The context contains current time, device model and id, not used
 * @param previousState  The device state since the last iteration, not used
 */
/*jslint unparam: true*/
function main(context, previousState) {

    // Reboot - devices goes offline and comes online after 20 seconds
    log("Executing IncreasePressure simulation function.");
    state.pressure = 250;
    state.CalculateRandomizedTelemetry = false;
    // update the state to 250
    updateState(state);

}
