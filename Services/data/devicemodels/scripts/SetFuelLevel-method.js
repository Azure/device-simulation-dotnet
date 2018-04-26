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
    log("Executing 'SetFuelLevel' JavaScript method simulation.");

    // Input validation. Make sure that thew new
    // fuel level is within the allowable range
    var newFuelLevel = context.FuelLevel;
    if (isNaN(newFuelLevel) || !newFuelLevel || newFuelLevel < 0 || newFuelLevel > 70) {
        throw new Error("Invalid fuel level specified. Fuel level must be betwee 0 and 70.");
    }

    log("Setting fuel level to: " + newFuelLevel);

    var state = {
        fuellevel: newFuelLevel
    };
    updateState(state);

    log("'SetFuelLevel' JavaScript method simulation completed.");
}