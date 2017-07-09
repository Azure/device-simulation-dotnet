// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation
{
    public interface IJavascriptInterpreter
    {
        object Invoke(string functionPath, string deviceId, DateTimeOffset utcNow);
    }

    public class JavascriptInterpreter : IJavascriptInterpreter
    {
        private readonly ILogger log;

        public JavascriptInterpreter(
            ILogger logger)
        {
            this.log = logger;
        }

        public object Invoke(string functionPath, string deviceId, DateTimeOffset utcNow)
        {
            throw new NotImplementedException();
        }
    }
}
