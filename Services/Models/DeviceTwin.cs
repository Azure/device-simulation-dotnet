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

        public string Etag { get; set; }
        public string DeviceId { get; set; }
        public bool IsSimulated { get; set; }
        public Dictionary<string, JToken> DesiredProperties { get; set; }
        public Dictionary<string, JToken> ReportedProperties { get; set; }
        public Dictionary<string, JToken> Tags { get; set; }

        public DeviceTwin(Twin twin)
        {
            if (twin != null)
            {
                this.Etag = twin.ETag;
                this.DeviceId = twin.DeviceId;
                this.Tags = TwinCollectionToDictionary(twin.Tags);
                this.DesiredProperties = TwinCollectionToDictionary(twin.Properties.Desired);
                this.ReportedProperties = TwinCollectionToDictionary(twin.Properties.Reported);
                this.IsSimulated = this.Tags.ContainsKey(SIMULATED_TAG_KEY) && this.Tags[SIMULATED_TAG_KEY].ToString() == SIMULATED_TAG_VALUE;
            }
        }

        /*
        JValue:  string, integer, float, boolean
        JArray:  list, array
        JObject: dictionary, object

        JValue:     JToken, IEquatable<JValue>, IFormattable, IComparable, IComparable<JValue>, IConvertible
        JArray:     JContainer, IList<JToken>, ICollection<JToken>, IEnumerable<JToken>, IEnumerable
        JObject:    JContainer, IDictionary<string, JToken>, ICollection<KeyValuePair<string, JToken>>, IEnumerable<KeyValuePair<string, JToken>>, IEnumerable, INotifyPropertyChanged, ICustomTypeDescriptor, INotifyPropertyChanging
        JContainer: JToken, IList<JToken>, ICollection<JToken>, IEnumerable<JToken>, IEnumerable, ITypedList, IBindingList, IList, ICollection, INotifyCollectionChanged
        JToken:     IJEnumerable<JToken>, IEnumerable<JToken>, IEnumerable, IJsonLineInfo, ICloneable, IDynamicMetaObjectProvider
        */
        private static Dictionary<string, JToken> TwinCollectionToDictionary(TwinCollection x)
        {
            var result = new Dictionary<string, JToken>();

            if (x == null) return result;

            foreach (KeyValuePair<string, object> twin in x)
            {
                try
                {
                    if (twin.Value is JToken)
                    {
                        result.Add(twin.Key, (JToken) twin.Value);
                    }
                    else
                    {
                        result.Add(twin.Key, JToken.Parse(twin.Value.ToString()));
                    }
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
