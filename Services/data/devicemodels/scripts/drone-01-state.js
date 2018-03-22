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

const GeoSpatialPrecision = 6;
const DecimalPrecision = 2;

const FlightPath = ["0", "1", "2", "1", "3", "1", "4"];

// Default state
var state = {
    online: true,
    temperature: 75.0,
    temperature_unit: "F",
    velocity: 0.0,
    velocity_unit: "mm/sec",
    acceleration: 0.0,
    acceleration_unit: "mm/sec^2",
    batteryStatus: "full",
    batteryLevel: battery_full,
    latitude: center_latitude,
    longitude: center_longitude,
    altitude: stable_altitude,
    flightStatus: "0",
    flightPosition: 0
};

/**
 * Restore the global state using data from the previous iteration.
 *
 * @param previousState The output of main() from the previous iteration
 */
function restoreState(previousState) {
    // If the previous state is null, force a default state
    if (previousState !== undefined && previousState !== null) {
        state = previousState;
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
    state.temperature = vary(75, 2, 60, 100).toFixed(DecimalPrecision);

    state.acceleration = vary(5.0, 5, 0.0, 9.9).toFixed(DecimalPrecision);

    state.velocity = vary(99.99, 5, 0.0, 199.99).toFixed(DecimalPrecision);

    // 0.5 +/- 5%, Min 0.5, Max 1.0
    state.batteryLevel = vary(0.5, 5, 0.1, battery_full).toFixed(DecimalPrecision);

    state.batteryStatus = getBatteryStatus(state.batteryLevel);

    // Calculate flight status using previous state
    state.flightStatus = getFlightStatus();

    var coords = varylocation(center_latitude, center_longitude, distance_vary);
    state.latitude = coords.latitude.toFixed(GeoSpatialPrecision);
    state.longitude = coords.longitude.toFixed(GeoSpatialPrecision);

    state.altitude = vary(stable_altitude, 5, stable_altitude - altitude_vary, stable_altitude + altitude_vary).toFixed(DecimalPrecision);

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
    var data = FlightPath[state.flightPosition++];
    if (state.flightPosition >= FlightPath.length) state.flightPosition = 0;
    return data;
}

//var getFlightStatus = (function () {
//    var flight_simulation = [0, 1, 2, 1, 3, 1, 4];
//    var currentIndex = 0;
//    var maxIndex = flight_simulation.length - 1;

//    return {
//        next: function () {
//            if (currentIndex < 0 || currentIndex > maxIndex) currentIndex = 0;
//            return flight_simulation[currentIndex++];
//        }
//    };
//}());


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
