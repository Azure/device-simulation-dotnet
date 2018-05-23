// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation
{
    public interface IScriptInterpreter
    {
        /// <summary>Invoke one of the device script files</summary>
        /// <param name="script">Name of the script</param>
        /// <param name="context">Context, e.g. current time, device Id, device Model</param>
        /// <param name="state">Current device state</param>
        /// <param name="properties">Current device properties</param>
        /// <remarks> Updates the internal device state and internal device properties</remarks>
        void Invoke(
            Script script,
            Dictionary<string, object> context,
            ISmartDictionary state,
            ISmartDictionary properties);
    }

    public class ScriptInterpreter : IScriptInterpreter
    {
        public const string INTERNAL_SCRIPT = "internal";
        public const string JAVASCRIPT_SCRIPT = "javascript";

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

        public void Invoke(
            Script script,
            Dictionary<string, object> context,
            ISmartDictionary state,
            ISmartDictionary properties)
        {
            switch (script.Type.ToLowerInvariant())
            {
                default:
                    this.log.Error("Unknown script type", () => new { script.Type });
                    throw new NotSupportedException($"Unknown script type `{script.Type}`.");

                case JAVASCRIPT_SCRIPT:
                    this.log.Debug("Invoking JS", () => new { script.Path, context, state });
                    this.jsInterpreter.Invoke(script.Path, context, state, properties);
                    this.log.Debug("JS invocation complete", () => { });
                    break;

                case INTERNAL_SCRIPT:
                    this.log.Debug("Invoking internal script", () => new { script.Path, context, state });
                    this.intInterpreter.Invoke(script.Path, script.Params, context, state, properties);
                    this.log.Debug("Internal script complete", () => { });
                    break;
            }
        }
    }
}
