// Copyright (c) Microsoft. All rights reserved.

using System.IO;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    public interface IServicesConfig
    {
        string DeviceModelsFolder { get; set; }
        string DeviceModelsScriptsFolder { get; set; }
        string IoTHubConnString { get; set; }
        string StorageAdapterApiUrl { get; set; }
        int StorageAdapterApiTimeout { get; set; }
        IRateLimitingConfiguration RateLimiting { get; set; }
    }

    // TODO: test Windows/Linux folder separator
    //       https://github.com/Azure/device-simulation-dotnet/issues/84
    public class ServicesConfig : IServicesConfig
    {
        private string dtf;
        private string dtbf;

        public ServicesConfig()
        {
            this.dtf = string.Empty;
            this.dtbf = string.Empty;
            this.RateLimiting = new RateLimitingConfiguration();
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

        public string StorageAdapterApiUrl { get; set; }

        public int StorageAdapterApiTimeout { get; set; }

        public IRateLimitingConfiguration RateLimiting { get; set; }

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
