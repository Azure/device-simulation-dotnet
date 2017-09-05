// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState(state)*/
/*global sleep(ms)*/
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
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState) {

    // Reboot - devices goes offline and comes online after 20 seconds
    log("Executing firmware update simulation function, firmware version passed:" + context.Firmware);

    // update the state to offline & firmware updating
    state.online = false;
    var status = "Command received, updating firmware version to ";
    status = status.concat(context.Firmware);
    state.DeviceMethodStatus = status;
    updateState(state);
    sleep(5000);

    log("Image Downloading...");
    state.DeviceMethodStatus = "Image Downloading...";
    updateState(state);
    sleep(10000);

    log("Executing firmware update simulation function, firmware version passed:" + context.Firmware);
    state.DeviceMethodStatus = "Downloaded, applying firmware...";
    updateState(state);
    sleep(5000);

    state.DeviceMethodStatus = "Rebooting...";
    updateState(state);
    sleep(5000);

    state.DeviceMethodStatus = "Firmware Updated."
    state.Firmware = context.Firmware;
    updateState(state);
    sleep(5000);

    state.DeviceMethodStatus = ""
    state.online = true;
    updateState(state);
    
    return state;
}
