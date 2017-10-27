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

    log("Starting 'Increase Pressure' method simulation (5 seconds)");

    var state = {
        simulation_state: "high_pressure"
    };

    disableSensorSimulation();

    state.pressure = 170;
    updateState(state);
    log("Pressure increased to " + state.pressure);
    sleep(1000);

    state.pressure = 190;
    updateState(state);
    log("Pressure increased to " + state.pressure);
    sleep(1000);

    state.pressure = 210;
    updateState(state);
    log("Pressure increased to " + state.pressure);
    sleep(1000);

    state.pressure = 230;
    updateState(state);
    log("Pressure increased to " + state.pressure);
    sleep(1000);

    state.pressure = 250;
    updateState(state);
    log("Pressure increased to " + state.pressure);
    sleep(1000);

    enableSensorSimulation();

    log("'Increase Pressure' method simulation completed");
}
