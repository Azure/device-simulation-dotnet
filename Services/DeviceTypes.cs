// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.Configuration;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services
{
    public interface IDeviceTypes
    {
        IEnumerable<DeviceType> GetList();
        DeviceType Get(string id);
    }

    public class DeviceTypes : IDeviceTypes
    {
        private const string Ext = ".devicetype";
        private readonly IServicesConfig config;

        private List<string> deviceTypeFiles;
        private List<DeviceType> deviceTypes;

        public DeviceTypes(IServicesConfig config)
        {
            this.config = config;
            this.deviceTypeFiles = null;
            this.deviceTypes = null;
        }

        public IEnumerable<DeviceType> GetList()
        {
            return new List<DeviceType>
            {
                new DeviceType { Name = "Chiller" },
                new DeviceType { Name = "Truck" }
            };
        }

        public DeviceType Get(string id)
        {
            return new DeviceType { Name = id };
        }

        private List<string> GetDeviceTypes()
        {
            this.deviceTypes = new List<DeviceType>();
            var files = this.GetDeviceTypeFiles();
            foreach (var file in files)
            {
                var config = ConfigurationFactory.ParseString(file);
            }

            return null; //this.deviceTypes;
        }

        private List<string> GetDeviceTypeFiles()
        {
            if (this.deviceTypeFiles != null) return this.deviceTypeFiles;

            var fileEntries = Directory.GetFiles(this.config.DataFolder);
            this.deviceTypeFiles = fileEntries.Where(fileName => fileName.EndsWith(Ext)).ToList();

            return this.deviceTypeFiles;
        }
    }
}
