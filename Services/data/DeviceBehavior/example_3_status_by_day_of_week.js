// Copyright (c) Microsoft. All rights reserved.

/**
 * Example of a function generating different data for different devices.
 */

/**
 * Entry point function called by the simulation engine.
 *
 * @param context  The context contains current time and device id
 *
 * @returns {{status: string}}
 */
function main(context) {

    // Default result
    var result = "unknown";

    var now = new Date(context.currentTime);
    var dayOfTheWeek = now.getUTCDay();

    // Closed on Sunday and Saturday
    if (dayOfTheWeek === 0 || dayOfTheWeek === 6) {
        result = "closed";
    } else {
        result = "open";
    }

    // Note: "status" is the key used in the message template, e.g. ${foo.status}
    return {status: result};
}
