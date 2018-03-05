// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public interface IInternalDeviceProperties
    {
        Dictionary<string, object> GetAll();
        void SetAll(Dictionary<string, object> newState);
        bool Has(string key);
        object Get(string key);
        void Set(string key, object value);
        bool Changed { get; }
        void ResetChanged();
    }

    public class InternalDeviceProperties : IInternalDeviceProperties
    {
        private IDictionary<string, object> dictionary;
        private bool changed;

        public bool Changed { get { return this.changed; } }

        public InternalDeviceProperties()
        {
            this.dictionary = new ConcurrentDictionary<string, object>();
            this.changed = false;
        }

        public InternalDeviceProperties(DeviceModel deviceModel)
        {
            var intitalProperties = this.SetupProperties(deviceModel);
            this.dictionary = new ConcurrentDictionary<string, object>(intitalProperties);
            this.changed = true;
        }

        /// <summary>
        /// Called when values have been synchronized. Resets 'changed' flag to false.
        /// </summary>
        public void ResetChanged()
        {
            this.changed = false;
        }

        public object Get(string key)
        {
            return this.dictionary[key];
        }

        public Dictionary<string, object> GetAll()
        {
            return new Dictionary<string, object>(this.dictionary);
        }

        public bool Has(string key)
        {
            return this.dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Set a property with the given key, adds new value if key does not exist.
        /// Sets the changed flag to true.
        /// </summary>
        public virtual void Set(string key, object value)
        {
            if (this.dictionary.ContainsKey(key))
            {
                this.dictionary[key] = value;
            }
            else
            {
                this.dictionary.Add(key, value);
            }

            this.changed = true;
        }

        public virtual void SetAll(Dictionary<string, object> newState)
        {
            this.dictionary = newState;
            this.changed = true;
        }

        /// <summary>
        /// Initializes device properties from the device model.
        /// </summary>
        private Dictionary<string, object> SetupProperties(DeviceModel deviceModel)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

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
