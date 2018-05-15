// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global sleep*/
/*jslint node: true*/

"use strict";

// Default state
var state = {
    temperature: 0,
    CalculateRandomizedTelemetry: true
};

// Default properties
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
 * Entry point function called by the method.
 *
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 * @param previousProperties  The device properties since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState, previousProperties) {

    log("Executing 'TempIncrease' JavaScript method.");

    // Restore the global device properties and the global state before
    // generating the new telemetry, so that the telemetry can apply changes
    // using the previous function state.
    restoreSimulation(previousState, previousProperties);

    // Pause the simulation
    state.CalculateRandomizedTelemetry = false;
    updateState(state);

    // temperature increment
    var increment = 5;

    // Increase
    state.temperature += increment;
    updateState(state);
    log("Decreasing temperature to " + state.temperature);
    sleep(1000);

    // Increase
    state.temperature += increment;
    updateState(state);
    log("Decreasing temperature to " + state.temperature);
    sleep(1000);

    // Increase
    state.temperature += increment;
    updateState(state);
    log("Decreasing temperature to " + state.temperature);
    sleep(1000);

    // Increase
    state.temperature += increment;
    updateState(state);
    log("Decreasing temperature to " + state.temperature);
    sleep(1000);

    // Increase
    state.temperature += increment;
    updateState(state);
    log("Decreasing temperature to " + state.temperature);
    sleep(1000);

    // Resume the simulation
    state.CalculateRandomizedTelemetry = true;

    updateState(state);

    log("'TempIncrease' JavaScript method simulation completed.");
}
