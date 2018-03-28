// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global updateProperty*/
/*global sleep*/
/*jslint node: true*/

"use strict";

// Default state
var state = {
    online: true,
    Firmware: "1.0.0",
    DeviceMethodStatus: "Updating Firmware"
};

// Default device properties
var properties = {};

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
 * Entry point function called by the simulation engine.
 *
 * @param context        The context contains current time, device model and id, not used
 * @param previousState  The device state since the last iteration, not used
 * @param previousProperties  The device properties since the last iteration
 */

/*jslint unparam: true*/
function main(context, previousState, previousProperties) {

    // Reboot - devices goes offline and comes online after 20 seconds
    log("Executing firmware update simulation function, firmware version passed:" + context.Firmware);

    // update the state to offline & firmware updating
    state.online = false;
    state.CalculateRandomizedTelemetry = false;
    var status = "Command received, updating firmware version to ";
    status = status.concat(context.Firmware);
    state.DeviceMethodStatus = status;
    updateState(state);
    sleep(5000);

    log("Image Downloading...");
    state.DeviceMethodStatus = "Image Downloading...";
    updateState(state);
    sleep(7500);

    log("Executing firmware update simulation function, firmware version passed:" + context.Firmware);
    state.DeviceMethodStatus = "Downloaded, applying firmware...";
    updateState(state);
    sleep(5000);

    state.DeviceMethodStatus = "Rebooting...";
    updateState(state);
    sleep(5000);

    state.DeviceMethodStatus = "Firmware Updated.";
    state.Firmware = properties.Firmware = context.Firmware;
    updateProperty(state);
    updateProperty(properties);
    sleep(7500);

    state.CalculateRandomizedTelemetry = true;
    state.online = true;
    state.DeviceMethodStatus = "";
    updateState(state);

}