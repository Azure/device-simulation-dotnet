// Copyright (c) Microsoft. All rights reserved.

/**
 * Example of a stateful function generating an increasing number.
 *
 * When the number reaches 1000, it restarts from 0.
 */

// Initialize the state, for the first execution
// Note: "count" is the key used in the message template, e.g. ${foo.count}
var state = {
    count: 0
};

/**
 * Entry point function called by the simulation engine.
 *
 * @param context        The context contains current time and device id
 * @param previousState  The output of this function from the previous iteration
 *
 * @returns {{count: number}}
 */
function main(context, previousState) {

    // Restore the global state first, so that the function can continue
    // from where it left last time.
    if (typeof(previousState) !== "undefined" && previousState !== null) {
        state = previousState;
    }

    state.count += 1;

    if (state.count > 1000) state.count = 0;

    return state;
}
