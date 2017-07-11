// Copyright (c) Microsoft. All rights reserved.

/**
 * Simulate a Room with temperature increasing during the day, and decreasing
 * during the night. The humidity also varies depending on the temperature.
 */

// Default state, just in case main() is called with an empty previousState.
var state = {
    temperature: 50,
    temperature_unit: "F",
    humidity: 50,
    humidity_unit: "%",
    lights_on: false
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
        state.temperature = Math.min(state.temperature + 0.1, 90);
    } else {
        state.temperature = Math.max(state.temperature - 0.1, 50);
    }

    /**
     * When it's hot the humidity decreases to a minimum of 0%
     * When it's cold the humidity increases to a maximum of 90%
     */
    if (state.temperature > 90) {
        state.humidity = Math.max(0, state.humidity - 1);
    } else if (state.temperature < 70) {
        state.humidity = Math.min(100, state.humidity + 1);
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
