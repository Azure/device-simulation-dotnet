// Copyright (c) Microsoft. All rights reserved.

/*global log*/
/*global updateState*/
/*global updateProperty*/
/*global sleep*/
/*jslint node: true*/

"use strict";

// Position control
const DefaultLatitude = 47.476075;
const DefaultLongitude = -122.192026;

// Altitude control
const DefaultAltitude = 0.0;
const AverageAltitude = 499.99;
const AltitudeVariation = 5;

// Battery control
const MinBatteryLevel = 0.1;
const MaxBatteryLevel = 1.0;
const BatteryVariation = 2;

// Temperature control
const AverageTemperature = 75.00;
const MinTemperature = 60.00;
const MaxTemperature = 120.00;
const TemperatureVariation = 15;

// Velocity control
const AverageVelocity = 60.00;
const MinVelocity = 20.00;
const MaxVelocity = 120.00;
const VelocityVariation = 5;

// Acceleration control
const AverageAcceleration = 2.50;
const MinAcceleration = 0.01;
const MaxAcceleration = 9.99;
const AccelerationVariation = 1;

// Display control for position and other attributes
const GeoSpatialPrecision = 6;
const DecimalPrecision = 2;

// TODO: Introduce multiple flightpaths to spike up the app
const FlightPath = [
    ["0", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "2", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "3", "1", "1", "1", "1", "1", "1", "1", "1", "1", "4"],
    ["0", "1", "1", "1", "1", "1", "2", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "3", "1", "1", "1", "1", "1", "1", "1", "1", "1", "4"],
    ["0", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "2", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "3", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "4"],
    ["0", "1", "1", "1", "2", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "3", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "4"],
    ["0", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "2", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "3", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1", "4"]
];
// Default state
var state = {
    online: true,
    temperature: AverageTemperature,
    temperature_unit: "F",
    velocity: 0.0,
    velocity_unit: "mm/sec",
    acceleration: 0.0,
    acceleration_unit: "mm/sec^2",
    batteryStatus: "full",
    batteryLevel: MaxBatteryLevel,
    latitude: DefaultLatitude,
    longitude: DefaultLongitude,
    altitude: DefaultAltitude,
    flightStatus: "0",
    flightPosition: 0,
    flightIndex: 0,
    deliveryId: ""
};

// Default device properties
var properties = {};

/**
 * Restore the global state using data from the previous iteration.
 *
 * @param previousState device state from the previous iteration
 * @param previousProperties device properties from the previous iteration
 */
function restoreSimulation(previousState, previousProperties) {
    // If the previous state is null, force a default state
    if (previousState) {
        state = previousState;
    } else {
        log("Using default state");
    }

    if (previousProperties) {
        properties = previousProperties;
    } else {
        log("Using default properties");
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
 * Returns updated simulation state.
 * Device property updates must call updateProperties() to persist.
 *
 * @param context             The context contains current time, device model and id
 * @param previousState       The device state since the last iteration
 * @param previousProperties  The device properties since the last iteration
 */
/*jslint unparam: true*/
function main(context, previousState, previousProperties) {

    // Restore the global state before generating the new telemetry, so that
    // the telemetry can apply changes using the previous function state.
    restoreSimulation(previousState, previousProperties);

    state.temperature = vary(AverageTemperature, TemperatureVariation, MinTemperature, MaxTemperature).toFixed(DecimalPrecision);
    state.acceleration = vary(AverageAcceleration, AccelerationVariation, MinAcceleration, MaxAcceleration).toFixed(DecimalPrecision);
    state.velocity = vary(AverageVelocity, VelocityVariation, MinVelocity, MaxVelocity).toFixed(DecimalPrecision);

    // Generate steadily decreasing battery levels
    state.batteryLevel = vary(Number(state.batteryLevel), BatteryVariation, MinBatteryLevel, Number(state.batteryLevel)).toFixed(DecimalPrecision);
    state.batteryStatus = getBatteryStatus(Number(state.batteryLevel));

    if (state.flightPosition == 0) {
        state.deliveryId = createUUID();
    }

    // Calculate flight status using previous state
    state.flightStatus = getFlightStatus();

    // Between -1.5 and 1.5 miles around start location
    var distance = roundTo(vary(0.05, 2500, -1.5, 1.5), 2);

    // Use the last coordinates to calculate the next set with a given variation
    var coords = varylocation(Number(state.latitude), Number(state.longitude), distance);
    state.latitude = Number(coords.latitude).toFixed(GeoSpatialPrecision);
    state.longitude = Number(coords.longitude).toFixed(GeoSpatialPrecision);

    // Fluctuate altitude between given variation constant by more or less
    state.altitude = vary(AverageAltitude, AltitudeVariation, AverageAltitude - AltitudeVariation, AverageAltitude + AltitudeVariation).toFixed(DecimalPrecision);

    updateState(state);
}

/**
 * Generate a random geolocation at some distance (in miles)
 * from a given location
 */
function varylocation(latitude, longitude, distance) {
    // Convert to meters, use Earth radius, convert to radians
    var radians = (distance * 1609.344 / 6378137) * (180 / Math.PI);
    return {
        latitude: roundTo((latitude + radians), GeoSpatialPrecision),
        longitude: roundTo((longitude + radians / Math.cos(latitude * Math.PI / 180)), GeoSpatialPrecision)
    };
}

function roundTo(n, digits) {
    var negative = false;
    if (digits === undefined) {
        digits = 0;
    }
    if (n < 0) {
        negative = true;
        n = n * -1;
    }
    var multiplicator = Math.pow(10, digits);
    n = parseFloat((n * multiplicator).toFixed(11));
    n = (Math.round(n) / multiplicator).toFixed(digits);
    if (negative) {
        n = (n * -1).toFixed(digits);
    }
    return n;
}

function getFlightStatus() {
    var data = FlightPath[state.flightIndex][state.flightPosition++];
    if (state.flightPosition >= FlightPath[state.flightIndex].length) {
        state.flightPosition = 0;
        state.flightIndex = Math.floor(Math.random() * FlightPath.length);
    }

    return data;
}

function createUUID() {
    var dt = new Date().getTime();
    var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = (dt + Math.random() * 16) % 16 | 0;
        dt = Math.floor(dt / 16);
        return (c == 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });

    return uuid;
}

function getBatteryStatus(level) {
    var status;

    switch (true) {
        case level == 0.00:
            status = "dead";
            break;
        case level >= 0.01 && level <= 0.10:
            status = "critical";
            break;
        case level >= 0.11 && level <= 0.30:
            status = "low";
            break;
        case level >= 0.31 && level <= 0.50:
            status = "medium";
            break;
        case level >= 0.51 && level <= 0.99:
            status = "high";
            break;
        case level == 1.0:
            status = "full";
            break;
    }

    return status;
}
