// Copyright (c) Microsoft. All rights reserved.

/**
 * Example of a function generating a random number between 1 and 10.
 */

/**
 * Entry point function called by the simulation engine.
 *
 * @returns {{rnd: number}}
 */
function main() {

    var value = Math.floor((Math.random() * 10) + 1);

    // Note: "rnd" is the key used in the message template, e.g. ${foo.rnd}
    return {
        rnd: value
    };
}
