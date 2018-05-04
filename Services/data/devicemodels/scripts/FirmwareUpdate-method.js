// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global updateProperty*/
/*global sleep*/
/*jslint node: true*/

"use strict";

// Default state
var state = {
    online: true
};

// Default device properties
var properties = {
    Firmware: "1.0.0",
    FirmwareUpdateStatus: "Updating Firmware"
};

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

    // Restore the global device properties and the global state before
    // generating the new telemetry, so that the telemetry can apply changes
    // using the previous function state.
    restoreSimulation(previousState, previousProperties);

    // Reboot - devices goes offline and comes online after 20 seconds
    log("Executing 'FirmwareUpdate' JavaScript method; Firmware version passed:" + context.Firmware);

    var DevicePropertyKey = "FirmwareUpdateStatus";
    var FirmwareKey = "Firmware";

    // update the status to offline & firmware updating
    state.online = false;
    state.CalculateRandomizedTelemetry = false;
    var status = "Command received, updating firmware version to ";
    status = status.concat(context.Firmware);
    updateProperty(DevicePropertyKey, status);
    sleep(5000);

    log("Image Downloading...");
    status = "Image Downloading...";
    updateProperty(DevicePropertyKey, status);
    sleep(7500);

    log("Executing firmware update simulation function, firmware version passed:" + context.Firmware);
    status = "Downloaded, applying firmware...";
    updateProperty(DevicePropertyKey, status);
    sleep(5000);

    status = "Rebooting...";
    updateProperty(DevicePropertyKey, status);
    sleep(5000);

    status = "Firmware Updated.";
    updateProperty(DevicePropertyKey, status);
    properties.Firmware = context.Firmware;
    updateProperty(FirmwareKey, context.Firmware);
    sleep(7500);

    state.CalculateRandomizedTelemetry = true;
    state.online = true;
    updateState(state);

    log("'FirmwareUpdate' JavaScript method simulation completed.");
}
