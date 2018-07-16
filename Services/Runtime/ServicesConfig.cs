// Copyright (c) Microsoft. All rights reserved.

using System.IO;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    public class StorageConfig
    {
        public string StorageType { get; set; }
        public string DocumentDbConnString { get; set; }
        public string DocumentDbDatabase { get; set; }
        public string DocumentDbCollection { get; set; }
        public int DocumentDbRUs { get; set; }
    }

    public interface IServicesConfig
    {
        string DeviceModelsFolder { get; }
        string DeviceModelsScriptsFolder { get; }
        string IoTHubConnString { get; }
        uint? IoTHubSdkDeviceClientTimeout { get; set; }
        bool TwinReadWriteEnabled { get; }

        StorageConfig MainStorage { get; }
        StorageConfig NodesStorage { get; }
        StorageConfig SimulationsStorage { get; }
        StorageConfig DevicesStorage { get; }
        StorageConfig PartitionsStorage { get; }
    }

    // TODO: test Windows/Linux folder separator
    //       https://github.com/Azure/device-simulation-dotnet/issues/84
    public class ServicesConfig : IServicesConfig
    {
        public const string USE_DEFAULT_IOTHUB = "default";

        private string dtf;
        private string dtbf;

        public ServicesConfig()
        {
            this.dtf = string.Empty;
            this.dtbf = string.Empty;
        }

        public string DeviceModelsFolder
        {
            get { return this.dtf; }
            set { this.dtf = this.NormalizePath(value); }
        }

        public string DeviceModelsScriptsFolder
        {
            get { return this.dtbf; }
            set { this.dtbf = this.NormalizePath(value); }
        }

        public string IoTHubConnString { get; set; }

        public uint? IoTHubSdkDeviceClientTimeout { get; set; }

        public bool TwinReadWriteEnabled { get; set; }

        public StorageConfig MainStorage { get; set; }
        public StorageConfig NodesStorage { get; set; }
        public StorageConfig SimulationsStorage { get; set; }
        public StorageConfig DevicesStorage { get; set; }
        public StorageConfig PartitionsStorage { get; set; }

        private string NormalizePath(string path)
        {
            return path
                       .TrimEnd(Path.DirectorySeparatorChar)
                       .Replace(
                           Path.DirectorySeparatorChar + "." + Path.DirectorySeparatorChar,
                           Path.DirectorySeparatorChar.ToString())
                   + Path.DirectorySeparatorChar;
        }
    }
}
