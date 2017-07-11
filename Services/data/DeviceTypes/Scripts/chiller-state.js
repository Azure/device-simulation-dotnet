// Copyright (c) Microsoft. All rights reserved.

/**
 * Simulate a Chiller at with fluctuating temperature.
 *
 * Example: chiller 'Simulated.Chiller.0' has a different average temperature.
 */

// Default state, just in case main() is called with an empty previousState.
var state = {
    temperature: 50.5,
    temperature_unit: "F",
    humidity: 50,
    voltage: 110.0,
    power: 3.0,
    power_unit: "kW"
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

    // 49...51
    state.temperature = 49 + (Math.random() * 2);

    // Example: chiller 'Simulated.Chiller.0' has a different average temperature.
    if (context.deviceId === "Simulated.Chiller.0") {
        // 19...31
        state.temperature = 19 + (Math.random() * 2);
    }

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
