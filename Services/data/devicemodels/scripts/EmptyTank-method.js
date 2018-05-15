// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global sleep*/
/*jslint node: true*/

"use strict";

// Default state
var state = {
    CalculateRandomizedTelemetry: true
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
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 * @param previousProperties  The device properties since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState, previousProperties) {

    // Restore the global device properties and the global state before
    // generating the new telemetry, so that the telemetry can apply changes
    // using the previous function state.
    restoreSimulation(previousState, previousProperties);

    log("Executing 'EmptyTank' JavaScript method.");

    var fullFuelLevel = 70;
    var increment = fullFuelLevel / 5;

    if (state.fuellevel == 0) {
        log("Exiting 'EmptyTank' JavaScript method. Fuel tank is already empty.");
        return;
    }

    // Pause the simulation while emptying the tank
    state.CalculateRandomizedTelemetry = false;
    updateState(state);

    // Decrease
    state.fuellevel = fullFuelLevel - increment;
    updateState(state);
    log("Fuel level decreased to " + state.fuellevel);
    sleep(1000);

    // Decrease
    state.fuellevel -= increment;
    updateState(state);
    log("Fuel level decreased to " + state.fuellevel);
    sleep(1000);

    // Decrease
    state.fuellevel -= increment;
    updateState(state);
    log("Fuel level decreased to " + state.fuellevel);
    sleep(1000);

    // Decrease
    state.fuellevel -= increment;
    updateState(state);
    log("Fuel level decreased to " + state.fuellevel);
    sleep(1000);

    // Decrease
    state.fuellevel = 0;
    updateState(state);
    log("Fuel level decreased to " + state.fuellevel);
    sleep(1000);

    // Resume the simulation
    state.CalculateRandomizedTelemetry = true;
    updateState(state);

    log("'EmptyTank' JavaScript method simulation completed.");
}
