// Copyright (c) Microsoft. All rights reserved.

/**
 * Simulate a moving Truck.
 */

// Default state, just in case main() is called with an empty previousState.
var state = {
    latitude: 44.898556,
    longitude: 10.043592,
    speed: 10
};

/**
 * Entry point function called by the simulation engine.
 *
 * @param context        The context contains current time, device type and id
 * @param previousState  The device state since the last iteration
 */
function main(context, previousState) {

    // Restore the global state before generating the new telemetry, so that
    // the telemetry can apply changes using the previous function state.
    restoreState(previousState);

    state.latitude -= 0.01;
    state.longitude -= 0.01;

    if (state.latitude < 42) state.latitude = 44.898556;
    if (state.longitude < 9) state.latitude = 10.043592;

    return state;
}

/**
 * Restore the global state using data from the previous iteration.
 *
 * @param previousState The output of main() from the previous iteration
 */
function restoreState(previousState) {
    // If the previous state is null, force a default state
    if (typeof(previousState) !== "undefined" && previousState !== null) {
        state = previousState;
    } else {
        log("Using default state");
    }
}
