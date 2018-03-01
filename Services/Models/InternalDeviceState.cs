// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public interface IInternalDeviceState
    {
        bool PropertyChanged { get; set; }
        Dictionary<string, object> GetProperties();
        bool HasProperty(string key);
        object GetProperty(string key);
        void SetProperty(string key, object value);
        Dictionary<string, object> GetState();
        void SetState(Dictionary<string, object> state);
        bool HasStateValue(string key);
        object GetStateValue(string key);
        void SetStateValue(string key, object value);
    }

    /// <summary>
    /// The InternalDeviceState manages the internal state of the simulated telemetry and
    /// device properties. The state is unique to each device.
    /// </summary>
    public class InternalDeviceState : IInternalDeviceState
    {
        public const string CALC_TELEMETRY = "CalculateRandomizedTelemetry";

        private ILogger log;

        // TODO https://github.com/Azure/device-simulation-dotnet/issues/171
        // consider using thread safe concurrent dictionary here in case multiple scripts
        // try to modify the state at the same time. 

        // The virtual state of the simulated device. The simulationState is
        // periodically updated using an external script.
        private Dictionary<string, object> simulationState;

        // The virtual state of the simulated device properties. The reported properties,
        // if changed, will be written to the IoT Hub.
        private Dictionary<string, object>  properties;

        // flag that indicates when the twin needs to be pushed to the IoT Hub
        public bool PropertyChanged { get; set; }

        public InternalDeviceState(ILogger log)
        {
            this.simulationState = new Dictionary<string, object>();

            this.PropertyChanged = false;
            this.properties = new Dictionary<string, object>();
            
            this.log = log;
        }

        public InternalDeviceState(DeviceModel deviceModel, ILogger log)
        {
            this.simulationState = this.SetupTelemetry(deviceModel);

            // by default push initial properties state to IoT Hub
            this.PropertyChanged = true;
            this.properties = this.SetupProperties(deviceModel);
            
            this.log = log;
        }

        /// <summary>
        /// Returns true if key exists
        /// </summary>
        public bool HasProperty(string key)
        {
            // lock properties as mulitple scripts may try to access key at the same time.
            lock (this.properties)
            {
                return this.properties.ContainsKey(key);
            }
        }

        /// <summary>
        /// Retrieve a device property from the internal device reported properties.
        /// </summary>
        public object GetProperty(string key)
        {
            // lock properties as mulitple scripts may try to access key at the same time.
            lock (this.properties)
            {
                return this.properties[key];
            }
        }

        /// <summary>
        /// Retrieve all device properties from the DeviceTwin reported properties as a read only dictionary
        /// </summary>
        public Dictionary<string, object> GetProperties()
        {
            // lock properties as mulitple scripts may try to access key at the same time.
            lock (this.properties)
            {
                // TODO investigate returning a read only dictionary to
                // enforce updates through the set property
                return new Dictionary<string, object>(this.properties);
            }
        }

        /// <summary>
        /// Set a property with the given key, to be updated in the IoT Hub reported properties.
        /// Adds new value if key does not exist. Sets the PropertiesUpdateNeeded flag to true.
        /// </summary>
        public void SetProperty(string key, object value)
        {
            // lock properties as mulitple scripts may try to access key at the same time.
            lock (this.properties)
            {
                if (this.properties.ContainsKey(key))
                {
                    this.properties[key] = value;
                    log.Debug("Updated device property", () => new { key, value });
                }
                else
                {
                    this.properties.Add(key, value);
                    log.Debug("Added new device property", () => new { key, value });
                }

                // mark the device for reported properties updates to the IoT Hub
                this.PropertyChanged = true;
            }
        }

        /// <summary>
        /// Retrieve all simulation state values as a read only dictionary
        /// </summary>
        public Dictionary<string, object> GetState()
        {
            // lock properties as mulitple scripts may try to access key at the same time.
            lock (this.simulationState)
            {
                // TODO investigate returning a read only dictionary to
                // enforce updates through the set property
                return new Dictionary<string, object>(this.simulationState);
            }
        }

        /// <summary>
        /// Update the current simulation state
        /// </summary>
        public void SetState(Dictionary<string, object> newState)
        {
            // lock properties as mulitple scripts may try to access key at the same time.
            lock (this.simulationState)
            {
                this.simulationState = newState;
            }
        }

        /// <summary>
        /// Returns true if key exists
        /// </summary>
        public bool HasStateValue(string key)
        {
            // lock properties as mulitple scripts may try to access key at the same time.
            lock (this.simulationState)
            {
                return this.simulationState.ContainsKey(key);
            }
        }

        /// <summary>
        /// Retrieve simulated device state value.
        /// </summary>
        public object GetStateValue(string key)
        {
            // lock properties as mulitple scripts may try to access key at the same time.
            lock (this.simulationState)
            {
                return this.simulationState[key];
            }
        }

        /// <summary>
        /// Set the simulation state value with the given key, adds new value if key does not exist.
        /// </summary>
        public void SetStateValue(string key, object value)
        {
            // lock properties as mulitple scripts may try to access key at the same time.
            lock (this.simulationState)
            {
                if (this.simulationState.ContainsKey(key))
                {
                    this.simulationState[key] = value;
                }
                else
                {
                    this.simulationState.Add(key, value);
                }
            }
        }

        private Dictionary<string, object> SetupTelemetry(DeviceModel deviceModel)
        {
            // put telemetry properties in state
            Dictionary<string, object> state = CloneObject(deviceModel.Simulation.InitialState);

            // Ensure the state contains the "online" key
            if (!state.ContainsKey("online"))
            {
                state["online"] = true;
            }

            // TODO:This is used to control whether telemetry is calculated in UpdateDeviceState.
            // methods can turn telemetry off/on; e.g. setting temp high- turnoff, set low, turn on
            // it would be better to do this at the telemetry item level - we should add this in the future
            state.Add(CALC_TELEMETRY, true);

            return state;
        }

        /// <summary>
        /// Initializes device twin with initial device twin properties from the device model.
        /// </summary>
        private Dictionary<string, object> SetupProperties(DeviceModel deviceModel)
        {

            Dictionary<string, object> result = new Dictionary<string, object>();

            // add device properties to the properties dictionary. These properties will be written
            // as reported properties on the IoT Hub.
            foreach (var property in deviceModel.Properties)
            {
                result.Add(property.Key, JToken.FromObject(property.Value));
            }

            return result;
        }

        /// <summary>Copy an object by value</summary>
        private static T CloneObject<T>(T source)
        {
            return JsonConvert.DeserializeObject<T>(
                JsonConvert.SerializeObject(source));
        }
    }
}
