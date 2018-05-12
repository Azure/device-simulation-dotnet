// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global sleep*/
/*jslint node: true*/

"use strict";

// Default state
var state = {
    // reboot just changes whether the device is on or offline
    online: true
};

/**
 * Entry point function called by the method.
 *
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 * @param previousProperties  The device properties since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState, previousProperties) {

    // Reboot - devices goes offline and comes online after 20 seconds
    log("Executing 'Reboot' JavaScript method simulation.");

    state.DeviceMethodStatus = "Rebooting device...";
    state.CalculateRandomizedTelemetry = false;
    state.online = false;
    // update the state to offline
    updateState(state);

    // Sleep for 15 seconds
    sleep(15000);

    state.DeviceMethodStatus = "Successfully rebooted device.";
    updateState(state);

    // Sleep for 5 seconds
    sleep(5000);
    state.CalculateRandomizedTelemetry = true;
    // update the state back to online
    state.online = true;
    state.DeviceMethodStatus = "";
    updateState(state);

    log("'Reboot' JavaScript method simulation completed.");
}
