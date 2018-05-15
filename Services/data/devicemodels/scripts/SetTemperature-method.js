// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*jslint node: true*/

"use strict";

/**
 * Entry point function called by the simulation engine.
 *
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 * @param previousProperties  The device properties since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState, previousProperties) {

    // General info indicating that this method is being called
    log("Executing 'SetTemperature' JavaScript method simulation.");

    // Input validation. Make sure that thew new
    // temperature is within the allowable range
    var newTemperature = context.Temperature;
    if (isNaN(newTemperature) || !newTemperature || newTemperature < 0 || newTemperature > 100) {
        throw new Error("Invalid temperature specified. Temperature must be betwee 0 and 100.");
    }

    log("Setting temperature to: " + newTemperature);

    var state = {
        temperature: newTemperature
    };
    updateState(state);

    log("'SetTemperature' JavaScript method simulation completed.");
}
