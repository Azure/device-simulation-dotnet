// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global sleep*/
/*jslint node: true*/

"use strict";

// Default state
var state = {
    online: true,
    Firmware: "1.0.0",
    DeviceMethodStatus: "Updating Firmware"
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
    state.Firmware = context.Firmware;
    updateState(state);
    sleep(7500);

    state.CalculateRandomizedTelemetry = true;
    state.online = true;
    state.DeviceMethodStatus = "";
    updateState(state);

}
