// Copyright (c) Microsoft. All rights reserved.

function main() {
    return {
        voltage: 110 + Math.random() * 0.1,
        voltage_unit: "V",
        power: 1 + Math.random() * 0.2,
        power_unit: "A"
    };
}
