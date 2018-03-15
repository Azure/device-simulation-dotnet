// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global sleep*/
/*jslint node: true*/

"use strict";

var center_latitude = 47.612514;
var center_longitude = -122.204184;
var stable_altitude = 90.2345;

var altitude_vary = 5.012;
var distance_vary = 10;
var battery_full = 1.0;

var flight_simulation = [0, 1, 2, 1, 3, 1, 4];
var current_status = 0;

// Default state
var state = {
    online: true,
    temperature: 75,
    temperature_unit: "F",
    velocity: 0.0,
    velocity_unit: "mm/sec",
    acceleration: 0.0,
    acceleration_unit: "mm/sec",
    flightStatus: 1,
    batteryStatus: "full",
    batteryLevel: battery_full,
    latitude: center_latitude,
    longitude: center_longitude,
    altitude: stable_altitude
};

/**
 * Restore the global state using data from the previous iteration.
 *
 * @param previousState The output of main() from the previous iteration
 */
function restoreState(previousState) {
    // If the previous state is null, force a default state
    if (previousState !== undefined && previousState !== null) {
        log("Using previous state...");
        state = previousState;
        log("Using previous state FAILED...");
    } else {
        log("Using default state");
    }
}

/**
 * Simple formula generating a random value around the average
 * in between min and max
 */
function vary(avg, percentage, min, max) {
    var value = avg * (1 + ((percentage / 100) * (2 * Math.random() - 1)));
    value = Math.max(value, min);
    value = Math.min(value, max);
    return value;
}

/**
 * Simple function to enforce emergency landing by sending a C2D message
 */
function decideCourse() {
    // Some overtly comlex algorithm to calculate weather conditions such
    // as wind velocity, resistance etc. and decide if it's safe to fly
    return Math.random() < 0.2 ? "keepFlying" : "land";
}


/**
 * Entry point function called by the simulation engine.
 *
 * @param context        The context contains current time, device model and id
 * @param previousState  The device state since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState) {

    // Restore the global state before generating the new telemetry, so that
    // the telemetry can apply changes using the previous function state.
    restoreState(previousState);

    // 75F +/- 5%,  Min 25F, Max 100F
    state.temperature = vary(75, 2, 60, 100);

    state.acceleration = vary(5.0, 5, 0.0, 9.9);

    state.velocity = vary(99.99, 5, 0.0, 199.99);

    // 0.5 +/- 5%, Min 0.5, Max 1.0
    state.batteryLevel = vary(0.5, 5, 0.2, battery_full);

    state.batteryStatus = getBatteryStatus(state.batteryLevel);

    // Hard coding for now
    state.flightStatus = getFlightStatus();

    var coords = varylocation(center_latitude, center_longitude, distance_vary);
    state.latitude = coords.latitude;
    state.longitude = coords.longitude;

    state.altitude = vary(stable_altitude, 5, stable_altitude - altitude_vary, stable_altitude + altitude_vary);

    return state;
}

/**
 * Generate a random geolocation at some distance (in miles)
 * from a given location
 */
function varylocation(latitude, longitude, distance) {
    // Convert to meters, use Earth radius, convert to radians
    var radians = (distance * 1609.344 / 6378137) * (180 / Math.PI);
    return {
        latitude: latitude + radians,
        longitude: longitude + radians / Math.cos(latitude * Math.PI / 180)
    };
}

function getFlightStatus() {
    // Using state vars such as speed, acceleration and altitude, verify status
    // hard coding for now
    if (current_status >= flight_simulation.length) current_status = 0;
    return flight_simulation[current_status++];
}

function getBatteryStatus(level) {
    var status;
    switch (true) {
        case level == 0.0:
            status = "dead";
            break;
        case level >= 0.1 && level <= 0.3:
            status = "critical";
            break;
        case level >= 0.4 && level <= 0.6:
            status = "low";
            break;
        case level >= 0.7 && level <= 0.9:
            status = "high";
            break;
        case level == 1.0:
            status = "full";
            break;
    }

    return status;
}
