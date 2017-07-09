// Copyright (c) Microsoft. All rights reserved.

function main(context, previousResult) {

    if (typeof(previousResult) !== "undefined" && previousResult !== null) {
        log("Previous position of elevator " + context.deviceId + ": " + previousResult.value);
    }

    var result = Math.floor((Math.random() * 5) + 1);
    log("New position of elevator " + context.deviceId + ": " + result);

    return {
        value: result
    };
}
