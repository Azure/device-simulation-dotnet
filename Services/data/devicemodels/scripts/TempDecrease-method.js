// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global sleep*/
/*jslint node: true*/
/*jslint todo: true*/

"use strict";

/**
 * Entry point function called by the method.
 *
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 * @param previousProperties  The device properties since the last iteration
 */

/*jslint unparam: true*/
function main(context, previousState, previousProperties) {

    log("Executing 'TempDecrease' JavaScript method.");

    var state = {
        temperature: previousState.temperature,
        CalculateRandomizedTelemetry: false
    };
    updateState(state);

    // temperature increment
    var increment = 5;

    // Decrease
    state.temperature -= increment;
    updateState(state);
    log("Decreasing temperature to " + state.temperature);
    sleep(1000);

    // Decrease
    state.temperature -= increment;
    updateState(state);
    log("Decreasing temperature to " + state.temperature);
    sleep(1000);

    // Decrease
    state.temperature -= increment;
    updateState(state);
    log("Decreasing temperature to " + state.temperature);
    sleep(1000);

    // Decrease
    state.temperature -= increment;
    updateState(state);
    log("Decreasing temperature to " + state.temperature);
    sleep(1000);

    // Decrease
    state.temperature -= increment;
    updateState(state);
    log("Decreasing temperature to " + state.temperature);
    sleep(1000);

    // Resume the simulation
    state.CalculateRandomizedTelemetry = true;

    updateState(state);

    log("'TempDecrease' JavaScript method simulation completed.");
}
