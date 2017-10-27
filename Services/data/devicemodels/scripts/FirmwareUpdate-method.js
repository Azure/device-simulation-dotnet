// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global reportMethodProgress*/
/*global sleep*/
/*global disableSensorSimulation*/
/*global enableSensorSimulation*/
/*global enableTelemetry*/
/*global disableTelemetry*/
/*jslint node: true*/

"use strict";

/**
 * Entry point function called by the simulation engine.
 *
 * @param context        The context contains current time, device model and id, not used
 * @param previousState  The device state since the last iteration, not used
 */
/*jslint unparam: true*/
function main(context, previousState) {

    log("Starting 'Firmware Update' method simulation, new firmware version: " + context.Firmware);

    // Go offline
    disableTelemetry();
    disableSensorSimulation();

    // Report method start
    reportMethodProgress("Command received, updating firmware version to " + context.Firmware);
    sleep(5000);

    // Download firmware image
    log("Image Downloading...");
    reportMethodProgress("Image Downloading...");
    sleep(7500);

    // Install new firmware
    log("Executing firmware update simulation function, firmware version passed:" + context.Firmware);
    reportMethodProgress("Downloaded, applying firmware...");
    sleep(5000);

    // Reboot
    reportMethodProgress("Rebooting...");
    sleep(10000);

    // Update twin
    reportMethodProgress("Firmware Updated.");
    var state = {};
    state.Firmare = context.Firmware;
    updateState(state);
    sleep(7500);

    // Reset method execution progress status
    reportMethodProgress("");

    // Back online
    enableSensorSimulation();
    enableTelemetry();

    log("'Firmware Update' method simulation completed");
}
