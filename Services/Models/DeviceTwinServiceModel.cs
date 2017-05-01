// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models
{
    public class DeviceTwinServiceModel
    {
        public string Etag { get; set; }
        public string DeviceId { get; set; }
        public bool IsSimulated { get; set; }
        public Dictionary<string, JToken> DesiredProperties { get; set; }
        public Dictionary<string, JToken> ReportedProperties { get; set; }
        public Dictionary<string, JToken> Tags { get; set; }

        public DeviceTwinServiceModel(
            string etag,
            string deviceId,
            Dictionary<string, JToken> desiredProperties,
            Dictionary<string, JToken> reportedProperties,
            Dictionary<string, JToken> tags,
            bool isSimulated)
        {
            this.Etag = etag;
            this.DeviceId = deviceId;
            this.DesiredProperties = desiredProperties;
            this.ReportedProperties = reportedProperties;
            this.Tags = tags;
            this.IsSimulated = isSimulated;
        }

        public DeviceTwinServiceModel(Twin twin)
        {
            if (twin != null)
            {
                this.Etag = twin.ETag;
                this.DeviceId = twin.DeviceId;
                this.Tags = TwinCollectionToDictionary(twin.Tags);
                this.DesiredProperties = TwinCollectionToDictionary(twin.Properties.Desired);
                this.ReportedProperties = TwinCollectionToDictionary(twin.Properties.Reported);
                this.IsSimulated = this.Tags.ContainsKey("IsSimulated") && this.Tags["IsSimulated"].ToString() == "Y";
            }
        }

        public Twin ToAzureModel()
        {
            //TODO: to complete
            var tags = new TwinCollection();
            var properties = new TwinProperties
            {
                Desired = new TwinCollection()
            };

            return new Twin(this.DeviceId)
            {
                ETag = this.Etag,
                Tags = tags,
                Properties = properties
            };
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

            foreach (KeyValuePair<string, object> foo in x)
            {
                try
                {
                    result.Add(foo.Key, (JToken)foo.Value);
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
