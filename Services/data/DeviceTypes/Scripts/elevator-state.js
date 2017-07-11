// Copyright (c) Microsoft. All rights reserved.

/**
 * Simulate an Elevator that moves up and down, starting from
 * floor 1, up to floor 10, and back.
 *
 * Example: elevator 'Simulated.Elevator.1' travels to the 20th floor.
 */

// Default state, just in case main() is called with an empty previousState.
var state = {
    floor: 1,
    direction: "up"
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

    // Make sure there is a valid direction set
    if (typeof(state.direction) === "undefined" || state.direction === null
        || (state.direction !== "up" && state.direction !== "down")) {
        state.direction = "up";
    }
    
    // Floors in the building
    var floors = 10;
    
    // Example: elevator 'Simulated.Elevator.1' travels to the 20th floor.
    if (context.deviceId === "Simulated.Elevator.1") floors = 20;

    if (state.direction === "up") state.floor++;
    if (state.direction === "down") state.floor--;

    if (state.floor < 1) {
        state.floor = 1;
        state.direction = "up";
    }

    if (state.floor > floors) {
        state.floor = floors;
        state.direction = "down";
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
