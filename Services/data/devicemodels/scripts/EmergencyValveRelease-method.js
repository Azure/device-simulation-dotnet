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
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState) {

    log("Starting 'Emergency Valve Release' method simulation");

    var state = {
        simulation_state: "normal_pressure"
    };

    disableSensorSimulation();

    state.pressure = 150;
    updateState(state);
    log("Pressure decreased to " + state.pressure);

    enableSensorSimulation();

    log("'Emergency Valve Release' method simulation completed");
}
