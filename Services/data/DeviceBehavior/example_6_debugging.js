// Copyright (c) Microsoft. All rights reserved.

/**
 * Example of a function logging to the service log.
 */

/**
 * Entry point function called by the simulation engine.
 *
 * @returns {{rnd: number}}
 */
function main() {

    log("This message will appear in the service logs.");

    return {
        rnd: Math.floor((Math.random() * 10) + 1)
    };
}
