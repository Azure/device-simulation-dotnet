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

    log("Starting 'EmergencyValveRelease' JavaScript method simulation.");

    var state = {   
        simulation_state: "normal_pressure",
        pressure: 150
    };
    updateState(state);

    log("'Emergency Valve Release' JavaScript method simulation completed.");
}
