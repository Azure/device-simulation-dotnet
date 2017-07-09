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

    if (context.deviceId.indexOf("light")) {
        result = "on";
    } else if (context.deviceId.indexOf("engine")) {
        result = "off";
    }

    // Note: "status" is the key used in the message template, e.g. ${foo.status}
    return {
        status: result
    };
}
