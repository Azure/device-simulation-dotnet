// Copyright (c) Microsoft. All rights reserved.

/**
 * Simulate a Drone
 */

/**
 * Entry point function called by the simulation engine.
 *
 * @param context        The context contains current time, device type and id
 * @param previousState  The device state since the last iteration
 */
function main(context, previousState) {

    // Fly in a circle, once a minute
    var now = new Date(context.currentTime);
    var twopi = (Math.PI * 2);
    var rad = (now.getSeconds() / 60) * twopi;

    return {
        vertical_speed: 0.0,
        horizontal_speed: 10.0,
        compass: 360 * rad / twopi,
        latitude: 44.898556 + (0.01 * Math.sin(rad)),
        longitude: 10.043592 + (0.01 * Math.sin(rad)),
        altitude: 10.0
    };
}
