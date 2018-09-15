// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering
{
    public interface IClusteringConfig
    {
        // How often to check the list of nodes and partitions
        int CheckIntervalMsecs { get; }

        // Age of a node before being considered stale and removed
        int NodeRecordMaxAgeSecs { get; }

        // When a node is elected to master via a lock, this is the max age of the lock
        // before it automatically expires, allowing another node to become master, for
        // example in case the current master crashed
        int MasterLockDurationSecs { get; }

        // Max device partition size
        int MaxPartitionSize { get; }
    }

    public class ClusteringConfig : IClusteringConfig
    {
        private const int DEFAULT_CHECK_INTERVAL_MSECS = 15000;
        private const int MIN_CHECK_INTERVAL_MSECS = 1000;
        private const int MAX_CHECK_INTERVAL_MSECS = 300000;

        private const int DEFAULT_NODE_RECORD_MAX_AGE_MSECS = 60000;
        private const int MIN_NODE_RECORD_MAX_AGE_MSECS = 10000;
        private const int MAX_NODE_RECORD_MAX_AGE_MSECS = 600000;

        private const int DEFAULT_MASTER_LOCK_DURATION_MSECS = 120000;
        private const int MIN_MASTER_LOCK_DURATION_MSECS = 10000;
        private const int MAX_MASTER_LOCK_DURATION_MSECS = 300000;

        private const int DEFAULT_PARTITION_SIZE = 1000;
        private const int MIN_MAX_PARTITION_SIZE = 1;
        private const int MAX_MAX_PARTITION_SIZE = 10000;

        private int checkIntervalMsecs;
        private int nodeRecordMaxAgeMsecs;
        private int masterLockDurationMsecs;
        private int maxPartitionSize;

        public int CheckIntervalMsecs
        {
            get => this.checkIntervalMsecs;
            set
            {
                this.Validate("CheckIntervalMsecs", value, MIN_CHECK_INTERVAL_MSECS, MAX_CHECK_INTERVAL_MSECS);
                this.checkIntervalMsecs = value;
            }
        }

        public int NodeRecordMaxAgeSecs => (int) Math.Ceiling((double) this.NodeRecordMaxAgeMsecs / 1000);

        public int NodeRecordMaxAgeMsecs
        {
            get => this.nodeRecordMaxAgeMsecs;
            set
            {
                this.Validate("NodeRecordMaxAgeMsecs", value, MIN_NODE_RECORD_MAX_AGE_MSECS, MAX_NODE_RECORD_MAX_AGE_MSECS);
                this.nodeRecordMaxAgeMsecs = value;
            }
        }

        public int MasterLockDurationSecs => (int) Math.Ceiling((double) this.MasterLockDurationMsecs / 1000);

        public int MasterLockDurationMsecs
        {
            get => this.masterLockDurationMsecs;
            set
            {
                this.Validate("MasterLockDurationMsecs", value, MIN_MASTER_LOCK_DURATION_MSECS, MAX_MASTER_LOCK_DURATION_MSECS);
                this.masterLockDurationMsecs = value;
            }
        }

        public int MaxPartitionSize
        {
            get => this.maxPartitionSize;
            set
            {
                this.Validate("MaxPartitionSize", value, MIN_MAX_PARTITION_SIZE, MAX_MAX_PARTITION_SIZE);
                this.maxPartitionSize = value;
            }
        }

        public ClusteringConfig()
        {
            // Initialize object with default values
            this.CheckIntervalMsecs = DEFAULT_CHECK_INTERVAL_MSECS;
            this.NodeRecordMaxAgeMsecs = DEFAULT_NODE_RECORD_MAX_AGE_MSECS;
            this.MasterLockDurationMsecs = DEFAULT_MASTER_LOCK_DURATION_MSECS;
            this.MaxPartitionSize = DEFAULT_PARTITION_SIZE;
        }

        private void Validate(string name, int value, int min, int max)
        {
            if (value < min || value > max)
            {
                throw new InvalidConfigurationException(
                    $"{name} value [{value}] is not valid. Use a value within `{min}` and `{max}`.");
            }
        }
    }
}
