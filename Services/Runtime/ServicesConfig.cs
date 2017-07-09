// Copyright (c) Microsoft. All rights reserved.

using System.IO;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime
{
    public interface IServicesConfig
    {
        string DeviceTypesFolder { get; set; }
        string DeviceTypesBehaviorFolder { get; set; }
        string IoTHubManagerApiUrl { get; set; }
        int IoTHubManagerApiTimeout { get; set; }
    }

    // TODO: test Windows/Linux folder separator
    public class ServicesConfig : IServicesConfig
    {
        private string dtf = string.Empty;
        private string dtbf = string.Empty;

        public string DeviceTypesFolder
        {
            get { return this.dtf; }
            set { this.dtf = this.NormalizePath(value); }
        }

        public string DeviceTypesBehaviorFolder
        {
            get { return this.dtbf; }
            set { this.dtbf = this.NormalizePath(value); }
        }

        public string IoTHubManagerApiUrl { get; set; }
        public int IoTHubManagerApiTimeout { get; set; }

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
