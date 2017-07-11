// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation
{
    public interface IJavascriptInterpreter
    {
        Dictionary<string, object> Invoke(
            string filename,
            Dictionary<string, object> context,
            Dictionary<string, object> state);
    }

    public class JavascriptInterpreter : IJavascriptInterpreter
    {
        private readonly ILogger log;
        private readonly string folder;

        public JavascriptInterpreter(
            IServicesConfig config,
            ILogger logger)
        {
            this.folder = config.DeviceTypesScriptsFolder;
            this.log = logger;
        }

        /// <summary>
        /// Load a JS file and execute the main() function, passing in
        /// context information and the output from the previous execution.
        /// Returns a map of values.
        /// </summary>
        public Dictionary<string, object> Invoke(
            string filename,
            Dictionary<string, object> context,
            Dictionary<string, object> state)
        {
            var engine = new Engine();

            // Inject the logger in the JS context, to allow the JS function
            // logging into the service logs
            engine.SetValue("log", new Action<object>(this.JsLog));

            var sourceCode = this.LoadScript(filename);
            var result = new Dictionary<string, object>();
            try
            {
                this.log.Debug("Executing JS function", () => new { filename });

                JsValue output = engine.Execute(sourceCode).Invoke("main", context, state);;
                this.log.Debug("JS function output", () => new
                {
                    output.GetType().FullName,
                    ToObject = output.ToObject()
                });

                result = (Dictionary<string, object>) output.ToObject();

                this.log.Debug("JS function success", () => new { filename, result });
            }
            catch (Exception e)
            {
                this.log.Error("JS function failure", () => new { e.Message, e.GetType().FullName });
            }

            return result;
        }

        private string LoadScript(string filename)
        {
            var filePath = this.folder + filename;
            if (!File.Exists(filePath))
            {
                this.log.Error("Javascript file not found", () => new { filePath });
                throw new FileNotFoundException($"File {filePath} not found.");
            }

            return File.ReadAllText(filePath);
        }

        private void JsLog(object data)
        {
            this.log.Debug("Log from JS", () => new { data });
        }
    }
}
