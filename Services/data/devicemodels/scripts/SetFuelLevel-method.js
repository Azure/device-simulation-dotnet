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
    log("Executing JavaScript SetFuelLevel method.");

    // Input validation. Make sure that thew new
    // fuel level is within the allowable range
    var newFuelLevel = context.FuelLevel;
    if (isNaN(newFuelLevel)) {
        newFuelLevel = 0;
    }
    newFuelLevel = Math.max(newFuelLevel, 0);
    newFuelLevel = Math.min(newFuelLevel, 70);

    log("Setting fuel level to: " + newFuelLevel);

    var state = {
        fuellevel: newFuelLevel
    };
    updateState(state);

    log("'SetFuelLevel' method simulation completed.");

}
