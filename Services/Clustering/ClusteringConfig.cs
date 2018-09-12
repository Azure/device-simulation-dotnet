// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering
{
    public interface IClusteringConfig
    {
        // How often to check the list of nodes and partitions
        int CheckIntervalMsecs { get; set; }
    }

    public class ClusteringConfig : IClusteringConfig
    {
        private int checkIntervalMsecs;

        private const int DEFAULT_CHECK_INTERVAL_MSECS = 15000;
        private const int MIN_CHECK_INTERVAL_MSECS = 1000;
        private const int MAX_CHECK_INTERVAL_MSECS = 300000;

        public int CheckIntervalMsecs
        {
            get => this.checkIntervalMsecs;
            set
            {
                this.Validate("CheckIntervalMsecs", value, MIN_CHECK_INTERVAL_MSECS, MAX_CHECK_INTERVAL_MSECS);
                this.checkIntervalMsecs = value;
            }
        }

        public ClusteringConfig()
        {
            // Initialize object with default values
            this.CheckIntervalMsecs = DEFAULT_CHECK_INTERVAL_MSECS;
        }

        private void Validate(string name, int value, int min, int max)
        {
            if (value < min || value > max)
            {
                throw new InvalidConfigurationException(
                    name + " value is not valid. Use a value within `" + min + "` and `" + max + "`.");
            }
        }
    }
}
