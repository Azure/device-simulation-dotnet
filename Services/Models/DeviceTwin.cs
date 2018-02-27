// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class DeviceTwin
    {
        // Simulated devices are marked with a tag "IsSimulated = Y"
        public const string SIMULATED_TAG_KEY = "IsSimulated";

        public const string SIMULATED_TAG_VALUE = "Y";

        public string ETag { get; set; }
        public string DeviceId { get; set; }
        public bool IsSimulated { get; set; }
        public Dictionary<string, object> DesiredProperties { get; set; }
        public Dictionary<string, object> ReportedProperties { get; set; }
        public Dictionary<string, object> Tags { get; set; }

        public DeviceTwin(Twin twin)
        {
            if (twin != null)
            {
                this.ETag = twin.ETag;
                this.DeviceId = twin.DeviceId;
                this.Tags = TwinCollectionToDictionary(twin.Tags);
                this.DesiredProperties = TwinCollectionToDictionary(twin.Properties.Desired);
                this.ReportedProperties = TwinCollectionToDictionary(twin.Properties.Reported);
                this.IsSimulated = this.Tags.ContainsKey(SIMULATED_TAG_KEY) && this.Tags[SIMULATED_TAG_KEY].ToString() == SIMULATED_TAG_VALUE;
            }
        }

        private static Dictionary<string, object> TwinCollectionToDictionary(TwinCollection collection)
        {
            var result = new Dictionary<string, object>();

            if (collection == null) return result;

            foreach (KeyValuePair<string, object> twin in collection)
            {
                try
                {
                    result.Add(twin.Key, twin.Value);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            return result;
        }

        private static TwinCollection DictionaryToTwinCollection(Dictionary<string, object> dictionary)
        {
            var result = new TwinCollection();

            if (dictionary == null) return result;

            foreach (KeyValuePair<string, object> item in dictionary)
            {
                try
                {
                    result[item.Key] = item.Value;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            return result;
        }
    }
}
