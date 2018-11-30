// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures
{
    public interface ISmartDictionary
    {
        IDictionary<string, object> GetAll();
        void SetAll(Dictionary<string, object> newState);
        bool Has(string key);
        object Get(string key);
        void Set(string key, object value, bool compareWithCurrent);
        bool Changed { get; }
        void ResetChanged();
    }

    /// <summary>
    /// Wrapper for a dictionary that supports concurrent reads and writes
    /// and tracks if any entries have been changed.
    /// Note: uses ~20Kb per instance, e.g. 200 MB for 20k devices
    /// </summary>
    public class SmartDictionary : ISmartDictionary
    {
        /// <summary>
        /// A collection of items that can support concurrent reads and writes.
        /// </summary>
        private IDictionary<string, object> dictionary;

        public bool Changed { get; private set; }

        public SmartDictionary()
        {
            // needs to support concurrent reads and writes
            this.dictionary = new ConcurrentDictionary<string, object>();
            this.Changed = false;
        }

        public SmartDictionary(IDictionary<string, object> dictionary)
        {
            this.dictionary = new ConcurrentDictionary<string, object>(dictionary ?? new Dictionary<string, object>());
            this.Changed = true;
        }

        /// <summary>
        /// Called when values have been synchronized. Resets 'changed' flag to false.
        /// </summary>
        public void ResetChanged()
        {
            this.Changed = false;
        }

        /// <param name="key"></param>
        /// <exception cref="KeyNotFoundException">
        /// thrown when the key specified does not match any key in the collection.
        /// </exception>
        public object Get(string key)
        {
            if (!this.dictionary.ContainsKey(key))
            {
                throw new KeyNotFoundException();
            }

            return this.dictionary[key];
        }

        public IDictionary<string, object> GetAll()
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
        public void Set(string key, object value, bool compareWithCurrent)
        {
            if (this.dictionary.ContainsKey(key))
            {
                // Nothing to do if the value hasn't changed
                // so, for instance, we avoid pushing twin changes
                if (compareWithCurrent && AreEquivalent(value, this.dictionary[key]))
                {
                    return;
                }

                this.dictionary[key] = value;
            }
            else
            {
                this.dictionary.Add(key, value);
            }

            this.Changed = true;
        }

        public void SetAll(Dictionary<string, object> newState)
        {
            this.dictionary = new ConcurrentDictionary<string, object>(newState);
            this.Changed = true;
        }

        // Poor man comparison, works well for simple types (int, double, bool, etc)
        // True  -> The objects' data is the same
        // False -> The objects seems to be different
        private static bool AreEquivalent(object x, object y)
        {
            var v1 = JsonConvert.SerializeObject(x);
            var v2 = JsonConvert.SerializeObject(y);
            return string.Equals(v1, v2, StringComparison.InvariantCulture);
        }
    }
}
