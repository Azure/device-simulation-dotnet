// Copyright (c) Microsoft. All rights reserved.

using System;
using Jint;
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
        private Engine engine;

        public JavascriptInterpreter(
            ILogger logger)
        {
            this.log = logger;
            this.engine = new Engine();
        }

        public object Invoke(string functionPath, string deviceId, DateTimeOffset utcNow)
        {
            this.log.Debug("Executing JS function", () => new { functionPath });

            return null;
        }
    }
}
