// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global sleep*/
/*jslint node: true*/

"use strict";

// Default state
var state = {
    flightStatus: 1
};

/**
 * Entry point function called by the method.
 *
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState) {

    // Reboot - devices goes offline and comes online after 20 seconds
    log("Executing emergency landing simulation function.");

    state.DeviceMethodStatus = "Emergency landing...";
    state.CalculateRandomizedTelemetry = false;
    state.online = false;
    // update the state to offline
    updateState(state);

    // Sleep for 15 seconds
    sleep(15000);

    state.DeviceMethodStatus = "Successfully landed drone.";
    updateState(state);

    // Sleep for 5 seconds
    sleep(5000);
    state.CalculateRandomizedTelemetry = true;
    // update the state back to online
    state.online = true;
    state.DeviceMethodStatus = "";
    updateState(state);

}
