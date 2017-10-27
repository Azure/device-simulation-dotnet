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
 * Entry point function called by the method.
 *
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState) {

    log("Starting 'Reboot' method simulation (20 seconds)");

    // Go offline
    disableTelemetry();
    disableSensorSimulation();

    // Reboot
    reportMethodProgress("Rebooting device...");
    sleep(15000);

    // Update twin
    reportMethodProgress("Successfully rebooted device.");
    sleep(5000);

    // Reset method execution progress status
    reportMethodProgress("");

    // Back online
    enableSensorSimulation();
    enableTelemetry();

    log("'Reboot' method simulation completed");
}
