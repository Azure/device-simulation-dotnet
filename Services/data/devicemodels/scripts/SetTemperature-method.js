// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global updateProperty*/
/*global sleep*/
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
    log("Executing JavaScript SetTemperature method.");

    // Input validation. Make sure that thew new
    // temperature is within the allowable range
    var newTemperature = context.CargoTemperature;
    if (isNaN(newTemperature)) {
        newTemperature = 0;
    }
    newTemperature = Math.max(newTemperature, 0);
    newTemperature = Math.min(newTemperature, 100);

    log("Setting temperature to: " + newTemperature);

    var state = {
        cargotemperature: newTemperature
    };
    updateState(state);

    log("'SetTemperature' method simulation completed.");

}
