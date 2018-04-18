// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global sleep*/
/*jslint node: true*/

"use strict";

/**
 * Entry point function called by the method.
 *
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 * @param previousProperties  The device properties since the last iteration
 */

/*jslint unparam: true*/
function main(context, previousState, previousProperties) {

    log("Executing JavaScript StartMoving method.");

    var state = {
        moving: true
    };
    log("Starting device movement.");
    updateState(state);

    log("'StartMoving' method simulation completed");

}
