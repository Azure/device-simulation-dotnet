// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global sleep*/
/*jslint node: true*/

"use strict";

// Default state
var state = {
    pressure: 250.0,
    CalculateRandomizedTelemetry: false
};

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
    state.CalculateRandomizedTelemetry = true;
    // update the state to 150
    updateState(state);

}
