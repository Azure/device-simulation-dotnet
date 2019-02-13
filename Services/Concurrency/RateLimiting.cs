// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency
{
    public interface IRateLimiting
    {
        int ClusterSize { get; }
        bool HasExceededMessagingQuota { get; }
        bool HasExceededDeviceQuota { get; }

        void Init(IRateLimitingConfig config);
        long GetPauseForNextConnection();
        long GetPauseForNextRegistryOperation();
        long GetPauseForNextTwinRead();
        long GetPauseForNextTwinWrite();
        long GetPauseForNextMessage();

        // Change the rating behavior to 'normal' when quota is not exceeded
        void MessagingQuotaNotExceeded();

        // Change the messaging rating behavior when quota is exceeded
        void MessagingQuotaExceeded();

        // Return true only once a minute
        bool CanProbeMessagingQuota(long now);

        // Change the device creation behavior to 'normal' when quota is not exceeded
        void DeviceQuotaNotExceeded();

        // Change the creation rating behavior when quota is exceeded
        void DeviceQuotaExceeded();

        // Return true only once a minute
        bool CanProbeDeviceQuota(long now);

        // Get message throughput (messages per second)
        double GetThroughputForMessages();

        // Change the number of VMs, which affects the rating speed
        void ChangeClusterSize(int currentCount);
    }

    // TODO: https://github.com/Azure/device-simulation-dotnet/issues/80
    public class RateLimiting : IRateLimiting
    {
        // When reaching a quota limit, probe the hub once a minute
        public const long PAUSE_FOR_QUOTA_MSECS = 60 * 1000;

        private readonly ILogger log;
        private readonly IInstance instance;
        private readonly object messagingQuotaLock;
        private readonly object deviceQuotaLock;

        // Cluster size, used to calculate the rate for each node
        private int clusterSize;

        // Messaging quota variables
        private bool messagingQuotaExceeded;
        private long messagingQuotaNextProbe;

        // Messaging quota variables
        private bool deviceQuotaExceeded;
        private long deviceQuotaNextProbe;

        // Use separate objects to reduce internal contentions in the lock statement
        private PerSecondCounter connections;
        private PerMinuteCounter registryOperations;
        private PerSecondCounter twinReads;
        private PerSecondCounter twinWrites;
        private PerSecondCounter messaging;

        public int ClusterSize => this.clusterSize;
        public bool HasExceededMessagingQuota => this.messagingQuotaExceeded;
        public bool HasExceededDeviceQuota => this.deviceQuotaExceeded;

        // Note: this class should be used only via the simulation context
        // to ensure that concurrent simulations don't share data
        public RateLimiting(
            ILogger log,
            IInstance instance)
        {
            this.log = log;
            this.clusterSize = 1;
            this.instance = instance;

            // Messaging quota variables
            this.messagingQuotaLock = new object();
            this.messagingQuotaExceeded = false;
            this.messagingQuotaNextProbe = 0;

            // Device quota variables
            this.deviceQuotaLock = new object();
            this.deviceQuotaExceeded = false;
            this.deviceQuotaNextProbe = 0;
        }

        public void Init(IRateLimitingConfig config)
        {
            this.instance.InitOnce();

            this.connections = new PerSecondCounter(
                config.ConnectionsPerSecond, "Device connections", this.log);

            this.registryOperations = new PerMinuteCounter(
                config.RegistryOperationsPerMinute, "Registry operations", this.log);

            this.twinReads = new PerSecondCounter(
                config.TwinReadsPerSecond, "Twin reads", this.log);

            this.twinWrites = new PerSecondCounter(
                config.TwinWritesPerSecond, "Twin writes", this.log);

            this.messaging = new PerSecondCounter(
                config.DeviceMessagesPerSecond, "Device msg/sec", this.log);

            this.instance.InitComplete();
        }

        public long GetPauseForNextConnection()
        {
            return this.connections.GetPause();
        }

        public long GetPauseForNextRegistryOperation()
        {
            return this.registryOperations.GetPause();
        }

        public long GetPauseForNextTwinRead()
        {
            return this.twinReads.GetPause();
        }

        public long GetPauseForNextTwinWrite()
        {
            return this.twinWrites.GetPause();
        }

        public long GetPauseForNextMessage()
        {
            return this.messaging.GetPause();
        }

        // Change the rating behavior to 'normal' when quota is not exceeded
        public void MessagingQuotaNotExceeded()
        {
            if (!this.messagingQuotaExceeded) return;

            lock (this.messagingQuotaLock)
            {
                // Ensure the following code is executed only once
                if (!this.messagingQuotaExceeded) return;

                this.messagingQuotaExceeded = false;
                this.messagingQuotaNextProbe = 0;
            }

            this.log.Warn("Messaging backoff disabled");
        }

        // Change the messaging rating behavior when quota is exceeded
        public void MessagingQuotaExceeded()
        {
            if (this.messagingQuotaExceeded) return;

            lock (this.messagingQuotaLock)
            {
                // Ensure the following code is executed only once
                if (this.messagingQuotaExceeded) return;

                this.messagingQuotaExceeded = true;

                // In 1 minute allow the simulation to send a message to see if the daily quota has been reset
                this.messagingQuotaNextProbe = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                               + PAUSE_FOR_QUOTA_MSECS;
            }

            this.log.Warn("Messaging backoff enabled");
        }

        // Return true only once a minute
        public bool CanProbeMessagingQuota(long now)
        {
            if (now < this.messagingQuotaNextProbe) return false;

            lock (this.messagingQuotaLock)
            {
                // Ensure the following code is executed only once
                if (now < this.messagingQuotaNextProbe) return false;

                // In 1 minute allow the simulation to send one message, to see if the quota has been reset
                this.messagingQuotaNextProbe = now + PAUSE_FOR_QUOTA_MSECS;
            }

            return true;
        }

        public void DeviceQuotaNotExceeded()
        {
            if (!this.deviceQuotaExceeded) return;

            lock (this.deviceQuotaLock)
            {
                // Ensure the following code is executed only once
                if (!this.deviceQuotaExceeded) return;

                this.deviceQuotaExceeded = false;
                this.deviceQuotaNextProbe = 0;
            }

            this.log.Warn("Device registration backoff disabled");
        }

        public void DeviceQuotaExceeded()
        {
            if (this.deviceQuotaExceeded) return;

            lock (this.deviceQuotaLock)
            {
                // Ensure the following code is executed only once
                if (this.deviceQuotaExceeded) return;

                this.deviceQuotaExceeded = true;

                // In 1 minute allow the simulation to send a message to see if the daily quota has been reset
                this.deviceQuotaNextProbe = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                            + PAUSE_FOR_QUOTA_MSECS;
            }

            this.log.Warn("Device registration backoff enabled");
        }

        public bool CanProbeDeviceQuota(long now)
        {
            if (now < this.deviceQuotaNextProbe) return false;

            lock (this.deviceQuotaLock)
            {
                // Ensure the following code is executed only once
                if (now < this.deviceQuotaNextProbe) return false;

                // In 1 minute allow the simulation to create one device, to see if the quota has been reset
                this.deviceQuotaNextProbe = now + PAUSE_FOR_QUOTA_MSECS;
            }

            return true;
        }

        // Get message throughput (messages per second)
        public double GetThroughputForMessages()
        {
            return this.messaging.GetThroughputForMessages();
        }

        // Change the number of VMs, which affects the rating speed
        public void ChangeClusterSize(int count)
        {
            this.log.Info("Updating rating limits to the new cluster size",
                () => new { previousSize = this.clusterSize, newSize = count });

            this.clusterSize = count;
            this.connections.ChangeConcurrencyFactor(count);
            this.registryOperations.ChangeConcurrencyFactor(count);
            this.twinReads.ChangeConcurrencyFactor(count);
            this.twinWrites.ChangeConcurrencyFactor(count);
            this.messaging.ChangeConcurrencyFactor(count);
        }
    }
}
