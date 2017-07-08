// Copyright (c) Microsoft. All rights reserved.

/**
 * Example of a stateful function generating two correlated values.
 *
 * The script fakes temperature and humidity, in a day/night cycle.
 * The temperature increases during the day and decreases during the night.
 * The humidity varies in relation to the temperature.
 */

// Initialize the state, for the first execution
// Note: "temperature" and "humidity" are the keys used in the message
// template, e.g. ${foo.temperature} and ${foo.humidity}
var state = {
    temperature: 50,
    humidity: 50
};

/**
 * Entry point function called by the simulation engine.
 *
 * @param context        The context contains current time and device id
 * @param previousState  The output of this function from the previous iteration
 *
 * @returns {{temperature: number, humidity: number}}
 */
function main(context, previousState) {

    // Example of parameters:
    //   context.deviceId = "a-chiller-1002"
    //   context.currentTime = "2025-09-20T13:24:59+00:00"
    //   previousState.temperature = 74
    //   previousState.humidity = 25

    // Restore the global state before generating the new telemetry, so that
    // the telemetry can apply changes using the previous function state.
    restoreState(previousState);

    // Update the state in this new iteration, using context information
    // passed in by the simulation engine.
    updateState(context);

    // Return the new state, which contains the new telemetry
    return state;
}

/**
 * Restore the global state using data from the previous iteration.
 *
 * @param previousState The output of main() from the previous iteration
 */
function restoreState(previousState) {
    // If the previous state is null, this is likely the first iteration
    if (typeof(previousState) !== "undefined" && previousState !== null) {
        state = previousState;
    }
}

/**
 * Calculate new telemetry, using the previous state and the context information
 */
function updateState(context) {

    // Temporarily clone the state and leave the state untouched until
    // the new telemetry is ready.
    var newState = state;

    // Parse the date
    var now = new Date(context.currentTime);
    var hourOfTheDay = now.getHours();

    /**
     * The temperature increases for 12 hours, to a maximum of 90F,
     * and decreases for 12 hours, to a minimum of 50F.
     *
     * The humidity is indirectly correlated.
     */
    if (hourOfTheDay >= 6 && hourOfTheDay < 18) {
        newState.temperature = Math.min(state.temperature + 0.1, 90);
    } else {
        newState.temperature = Math.max(state.temperature - 0.1, 50);
    }

    /**
     * When it's hot the humidity decreases to a minimum of 0%
     * When it's cold the humidity increases to a maximum of 90%
     */
    if (newState.temperature > 90) {
        newState.humidity = Math.max(0, state.humidity - 1);
    } else if (newState.temperature < 70) {
        newState.humidity = Math.min(100, state.humidity + 1);
    }

    // After generating the new state, store the data in the global variable
    state = newState;
}
