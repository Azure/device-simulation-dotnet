// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jint;
using Jint.Native;
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
        private Dictionary<string, object> deviceState;

        public JavascriptInterpreter(
            IServicesConfig config,
            ILogger logger)
        {
            this.folder = config.DeviceModelsScriptsFolder;
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
            this.deviceState = state;

            var engine = new Engine();

            // Inject the logger in the JS context, to allow the JS function
            // logging into the service logs
            engine.SetValue("log", new Action<object>(this.JsLog));

            //register callback for state updates 
            engine.SetValue("updateState", new Action<JsValue>(this.UpdateState));

            var sourceCode = this.LoadScript(filename);
            this.log.Debug("Executing JS function", () => new { filename });

            try
            {
                var output = engine.Execute(sourceCode).Invoke("main", context, this.deviceState);
                var result = this.JsValueToDictionary(output);
                this.log.Debug("JS function success", () => new { filename, result });
                return result;
            }
            catch (Exception e)
            {
                this.log.Error("JS function failure", () => new { e.Message, e.GetType().FullName });
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Depending on the syntax used in the Javascript function, the object
        /// returned by Jint can be either a Dictionary or a
        /// Jint.Native.ObjectInstance, each with a different parsing logic.
        /// </summary>
        private Dictionary<string, object> JsValueToDictionary(JsValue data)
        {
            Dictionary<string, object> result;

            try
            {
                // Manage output as a Dictionary
                result = data.ToObject() as Dictionary<string, object>;
                if (result != null)
                {
                    this.log.Debug("JS function output", () => new
                    {
                        Type = "Dictionary",
                        data.GetType().FullName,
                        result
                    });

                    return result;
                }

                // Manage output as a Jint.Native.ObjectInstance
                result = new Dictionary<string, object>();
                var properties = data.AsObject().GetOwnProperties().ToArray();

                foreach (KeyValuePair<string, PropertyDescriptor> p in properties)
                {
                    result.Add(p.Key, p.Value.Value.ToObject());
                }

                this.log.Debug("JS function output", () => new
                {
                    Type = "ObjectInstance",
                    data.GetType().FullName,
                    result
                });

                return result;
            }
            catch (Exception e)
            {
                this.log.Error("JsValue parsing failure",
                    () => new { e.Message, e.GetType().FullName });

                return new Dictionary<string, object>();
            }
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

        private void Sleep(int timeInMs)
        {
            //TODO:Implement sleep function and pass it into Javascript files for use in simulating reboot, etc.
            //System.Threading.Tasks.thread
            //Thread.Sleep(timeInMs);
        }

        private void UpdateState(JsValue data)
        {
            string key;
            object value;
            Dictionary<string, object> stateChanges;

            this.log.Debug("Updating state from the script", () => new { data, this.deviceState});

            stateChanges = JsValueToDictionary((JsValue)data);
            
            //Update device state with the script data passed
            lock (this.deviceState)
            {
                for (int i = 0;i<stateChanges.Count;i++)
                {
                    key = stateChanges.Keys.ElementAt(i);
                    value = stateChanges.Values.ElementAt(i);
                    if (deviceState.ContainsKey(key))
                    {
                        if(deviceState[key] != value)
                        { 
                            this.log.Debug("state change", () => new { key, value });
                            deviceState[key] = value;
                        }
                    }
                }
            }
        }

    }
}
