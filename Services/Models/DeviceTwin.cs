// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

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
        public Dictionary<string, JToken> DesiredProperties { get; set; }
        public Dictionary<string, JToken> ReportedProperties { get; set; }
        public Dictionary<string, JToken> Tags { get; set; }

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

        private static Dictionary<string, JToken> TwinCollectionToDictionary(TwinCollection x)
        {
            var result = new Dictionary<string, JToken>();

            if (x == null) return result;

            foreach (KeyValuePair<string, JToken> twin in x)
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

        private static TwinCollection DictionaryToTwinCollection(Dictionary<string, JToken> x)
        {
            var result = new TwinCollection();

            if (x == null) return result;

            foreach (KeyValuePair<string, JToken> item in x)
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
