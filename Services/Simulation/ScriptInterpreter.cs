// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation
{
    public interface IScriptInterpreter
    {
        Dictionary<string, object> Invoke(
            Script script,
            Dictionary<string, object> context,
            Dictionary<string, object> state);
    }

    public class ScriptInterpreter : IScriptInterpreter
    {
        private readonly IJavascriptInterpreter jsInterpreter;
        private readonly IInternalInterpreter intInterpreter;
        private readonly ILogger log;

        public ScriptInterpreter(
            IJavascriptInterpreter jsInterpreter,
            IInternalInterpreter intInterpreter,
            ILogger logger)
        {
            this.jsInterpreter = jsInterpreter;
            this.intInterpreter = intInterpreter;
            this.log = logger;
        }

        public Dictionary<string, object> Invoke(
            Script script,
            Dictionary<string, object> context,
            Dictionary<string, object> state)
        {
            switch (script.Type.ToLowerInvariant())
            {
                default:
                    this.log.Error("Unknown script type", () => new { script.Type });
                    throw new NotSupportedException($"Unknown script type `{script.Type}`.");

                case "javascript":
                    this.log.Debug("Invoking JS", () => new { script.Path, context, state });
                    var jsResult = this.jsInterpreter.Invoke(script.Path, context, state);
                    this.log.Debug("JS result", () => new { result = jsResult });
                    return jsResult;

                case "internal":
                    this.log.Debug("Invoking internal script", () => new { script.Path, context, state });
                    var intResult = this.intInterpreter.Invoke(script.Path, script.Params, context, state);
                    this.log.Debug("Internal script result", () => new { intresult = intResult });
                    return intResult;
            }
        }
    }
}
