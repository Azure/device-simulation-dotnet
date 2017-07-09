// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Simulation
{
    public interface IJavascriptInterpreter
    {
        Dictionary<string, string> Invoke(
            string filename,
            string deviceId,
            DateTimeOffset utcNow,
            Dictionary<string, string> previousResult);
    }

    public class JavascriptInterpreter : IJavascriptInterpreter
    {
        private const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";

        private readonly ILogger log;
        private readonly string folder;

        public JavascriptInterpreter(
            IServicesConfig config,
            ILogger logger)
        {
            this.folder = config.DeviceTypesBehaviorFolder;
            this.log = logger;
        }

        /// <summary>
        /// Load a JS file and execute the main() function, passing in
        /// context information and the output from the previous execution.
        /// Returns a map of values.
        /// </summary>
        public Dictionary<string, string> Invoke(
            string filename,
            string deviceId,
            DateTimeOffset utcNow,
            Dictionary<string, string> previousResult)
        {
            var engine = new Engine();

            // Inject the logger in the JS context, to allow the JS function
            // logging into the service logs
            engine.SetValue("log", new Action<object>(this.JsLog));

            var context = new Dictionary<string, string>
            {
                { "deviceId", deviceId },
                { "currentTime", DateTimeOffset.UtcNow.ToString(DateFormat) }
            };

            var sourceCode = this.LoadScript(filename);
            var result = new Dictionary<string, string>();
            try
            {
                this.log.Debug("Executing JS function", () => new { filename });

                engine.Execute(sourceCode);
                var jsResult = previousResult == null ?
                    engine.Invoke("main", context) :
                    engine.Invoke("main", context, previousResult);

                var output = jsResult.AsObject().GetOwnProperties();
                result = output.ToDictionary(x => x.Key, x => x.Value.Value.ToString());
                this.log.Debug("JS success", () => new { filename, result });
            }
            catch (Exception e)
            {
                this.log.Error("JS failure", () => new { e.Message, e.GetType().FullName });
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
