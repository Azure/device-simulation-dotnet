// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceReplay;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Statistics;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    public interface ISimulationManager
    {
        Task InitAsync(
            Simulation simulation,
            ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors,
            ConcurrentDictionary<string, IDeviceConnectionActor> deviceConnectionActors,
            ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors,
            ConcurrentDictionary<string, IDevicePropertiesActor> devicePropertiesActors,
            ConcurrentDictionary<string, IDeviceReplayActor> deviceReplayActors);

        // === BEGIN - Executed by Agent.RunAsync

        // Ensure the partitions assigned to this node remain assigned
        Task HoldAssignedPartitionsAsync();

        // Check if there are new partitions
        Task AssignNewPartitionsAsync();

        // Check if there are partitions changes and act accordingly
        Task HandleAssignedPartitionChangesAsync();

        // Check if the cluster size has changed and act accordingly
        Task UpdateThrottlingLimitsAsync();

        Task SaveStatisticsAsync();

        void PrintStats();

        // === END - Executed by Agent.RunAsync

        // Stop all the actors and delete them - executed by Agent.StopInactiveSimulations
        void TearDown();

        // Executed by DeviceConnectionTask.RunAsync
        void NewConnectionLoop();

        // Executed by UpdatePropertiesTask.RunAsync
        void NewPropertiesLoop();
    }

    public class SimulationManager : ISimulationManager
    {
        // Used to access the actors dictionaries, which contain data about other simulations
        private const string ACTOR_PREFIX_SEPARATOR = "//";
        private const string ACTOR_TELEMETRY_PREFIX_SEPARATOR = "#";

        private readonly ISimulationContext simulationContext;
        private readonly IDevicePartitions devicePartitions;
        private readonly IClusterNodes clusterNodes;
        private readonly IDeviceModels deviceModels;
        private readonly IFactory factory;
        private readonly ISimulationStatistics simulationStatistics;
        private readonly ILogger log;
        private readonly IInstance instance;
        private readonly ISimulations simulations;
        private readonly int maxDevicePerNode;

        // Data shared with other simulations
        private ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors;
        private ConcurrentDictionary<string, IDeviceConnectionActor> deviceConnectionActors;
        private ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors;
        private ConcurrentDictionary<string, IDevicePropertiesActor> devicePropertiesActors;
        private ConcurrentDictionary<string, IDeviceReplayActor> deviceReplayActors;

        // List of the device partitions assigned to this node, including their content
        // in case they disappear from storage, used also to reduce storage lookups
        private readonly ConcurrentDictionary<string, DevicesPartition> assignedPartitions;

        private Simulation simulation;
        private int nodeCount;
        private int deviceCount;

        public SimulationManager(
            ISimulationContext simulationContext,
            IDevicePartitions devicePartitions,
            IClusterNodes clusterNodes,
            IDeviceModels deviceModels,
            IFactory factory,
            IClusteringConfig clusteringConfig,
            ILogger logger,
            IInstance instance,
            ISimulationStatistics simulationStatistics,
            ISimulations simulations)
        {
            this.simulationContext = simulationContext;
            this.devicePartitions = devicePartitions;
            this.clusterNodes = clusterNodes;
            this.deviceModels = deviceModels;
            this.simulationStatistics = simulationStatistics;
            this.factory = factory;
            this.log = logger;
            this.instance = instance;
            this.maxDevicePerNode = clusteringConfig.MaxDevicesPerNode;
            this.simulations = simulations;

            this.assignedPartitions = new ConcurrentDictionary<string, DevicesPartition>();
            this.nodeCount = 1;
            this.deviceCount = 0;
        }

        public async Task InitAsync(
            Simulation simulation,
            ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors,
            ConcurrentDictionary<string, IDeviceConnectionActor> deviceConnectionActors,
            ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors,
            ConcurrentDictionary<string, IDevicePropertiesActor> devicePropertiesActors,
            ConcurrentDictionary<string, IDeviceReplayActor> deviceReplayActors)
        {
            this.instance.InitOnce();

            this.simulation = simulation;
            await this.simulationContext.InitAsync(simulation);

            this.deviceStateActors = deviceStateActors;
            this.deviceConnectionActors = deviceConnectionActors;
            this.deviceTelemetryActors = deviceTelemetryActors;
            this.devicePropertiesActors = devicePropertiesActors;
            this.deviceReplayActors = deviceReplayActors;

            this.instance.InitComplete();
        }

        public async Task HoldAssignedPartitionsAsync()
        {
            this.instance.InitRequired();

            var partitionsToRelease = new List<DevicesPartition>();

            foreach (var partition in this.assignedPartitions)
            {
                try
                {
                    if (!await this.devicePartitions.TryToKeepPartitionAsync(partition.Value.Id))
                    {
                        partitionsToRelease.Add(partition.Value);
                    }
                }
                catch (ResourceNotFoundException)
                {
                    // A partition might be deleted when deleting a simulation, so this is not always an error
                    this.log.Warn("Partition not found, assignment cannot continue", () => new { partition.Value.Id });
                    partitionsToRelease.Add(partition.Value);
                }
            }

            foreach (DevicesPartition partition in partitionsToRelease)
            {
                this.log.Info("Removing partition from this node", () => new { partition.Id });
                var actorsCount = this.DeleteActorsForPartition(partition);
                this.assignedPartitions.TryRemove(partition.Id, out _);
                this.log.Info("Partition and actors removed", () => new { partition.Id, actorsCount });
            }
        }

        public async Task AssignNewPartitionsAsync()
        {
            this.instance.InitRequired();

            if (this.deviceCount > this.maxDevicePerNode) return;

            this.log.Debug("Searching for unassigned partitions...");
            var unassignedPartitions = await this.devicePartitions.GetUnassignedAsync(this.simulation.Id);
            this.log.Debug(() => new { UnassignedPartitions = unassignedPartitions.Count });

            // Randomize the list, to reduce the probability of contention with other nodes
            unassignedPartitions.Shuffle();

            /**
             * Important: after assigning a partition, the following operations could fail, in which case
             * the partition should be (eventually) released, and the actors should be removed.
             */
            foreach (DevicesPartition partition in unassignedPartitions)
            {
                // Stop adding partitions as soon as the current node has enough work to do
                if (this.deviceCount >= this.maxDevicePerNode)
                {
                    this.log.Info("Maximum number of devices per node reached", () => new { this.deviceCount, maxDevicePerNode = this.maxDevicePerNode });
                    return;
                }

                // Try to lock partition
                if (await this.devicePartitions.TryToAssignPartitionAsync(partition.Id))
                {
                    this.assignedPartitions[partition.Id] = partition;
                    this.log.Debug("Partition assigned", () => new { SimulationId = this.simulation.Id, PartitionId = partition.Id });

                    if (await this.TryToCreateActorsForPartitionAsync(partition))
                    {
                        this.log.Debug("Device actors started", () => new { SimulationId = this.simulation.Id, PartitionId = partition.Id });
                    }
                }
            }
        }

        public Task HandleAssignedPartitionChangesAsync()
        {
            // TODO: Support changing the devices in a running simulation.
            //       Remote Monitoring requires the ability to add devices to a running simulation. 
            //       We currently don't support this, and will need to design this feature before
            //       support is reinstated. 
            return Task.CompletedTask;
        }

        public void PrintStats()
        {
            this.instance.InitRequired();

            var connectedCount = this.deviceConnectionActors.Count(x => x.Value.Connected);

            this.log.Info("Simulation stats",
                () => new
                {
                    SimulationId = this.simulation.Id,
                    PartitionsInThisNode = this.assignedPartitions.Count,
                    DevicesInThisNode = this.deviceCount,
                    ConnectedDevicesInThisNode = connectedCount,
                    NodesInTheCluster = this.nodeCount,
                    RateLimitMessagesThroughput = this.simulationContext.RateLimiting.GetThroughputForMessages(),
                    RateLimitClusterSize = this.simulationContext.RateLimiting.ClusterSize
                });
        }

        public async Task SaveStatisticsAsync()
        {
            try
            {
                var prefix = this.GetDictKey(string.Empty);
                var telemetryActors = this.deviceTelemetryActors.Where(a => a.Key.StartsWith(prefix)).ToList();
                var connectionActors = this.deviceConnectionActors.Where(a => a.Key.StartsWith(prefix)).ToList();
                var propertiesActors = this.devicePropertiesActors.Where(a => a.Key.StartsWith(prefix)).ToList();
                var stateActors = this.deviceStateActors.Where(a => a.Key.StartsWith(prefix)).ToList();
                var replayActors = this.deviceReplayActors.Where(a => a.Key.StartsWith(prefix)).ToList();

                var simulationModel = new SimulationStatisticsModel
                {
                    ActiveDevices = stateActors.Count(a => a.Value.IsDeviceActive),
                    TotalMessagesSent = telemetryActors.Sum(a => a.Value.TotalMessagesCount),
                    FailedMessages = telemetryActors.Sum(a => a.Value.FailedMessagesCount),
                    FailedDeviceConnections = connectionActors.Sum(a => a.Value.FailedDeviceConnectionsCount),
                    FailedDevicePropertiesUpdates = propertiesActors.Sum(a => a.Value.FailedTwinUpdatesCount),
                    /* TODO: Add replay actors stats */
                };

                await this.simulationStatistics.CreateOrUpdateAsync(this.simulation.Id, simulationModel);
            }
            catch (Exception e)
            {
                // Log and do not rethrow
                this.log.Error("Error saving simulation statistics", () => new { this.simulation.Id, e });
            }
        }

        // Stop all the actors and delete them
        public void TearDown()
        {
            this.instance.InitRequired();

            this.simulationContext?.Dispose();

            this.DeleteAllStateActors();
            this.DeleteAllConnectionActors();
            this.DeleteAllTelemetryActors();
            this.DeleteAllPropertiesActors();
            this.DeleteAllReplayActors();
        }

        // Check if the cluster size has changed and act accordingly
        public async Task UpdateThrottlingLimitsAsync()
        {
            this.instance.InitRequired();

            var countChanged = await this.UpdateNodeCountAsync();

            // Update the rating limits accordingly to the number of nodes
            if (countChanged)
            {
                this.log.Info("The number of nodes has changed, updating rating limits...");
                this.simulationContext.RateLimiting.ChangeClusterSize(this.nodeCount);
            }
        }

        private async Task<bool> UpdateNodeCountAsync()
        {
            // TODO: for now we assume that all the nodes are participating, so we just count the number of nodes
            //       however when running few devices in a big cluster, the number of nodes is 1
            //       and when running on multiple nodes the load might be unbalanced

            try
            {
                int newCount = (await this.clusterNodes.GetSortedIdListAsync()).Count;
                if (newCount > 0 && newCount != this.nodeCount)
                {
                    this.log.Info("The number of nodes has changed", () => new { this.nodeCount, newCount });
                    this.nodeCount = newCount;
                    return true;
                }
            }
            catch (Exception e)
            {
                this.log.Error("Unexpected error while counting the nodes", e);
            }

            return false;
        }

        public void NewConnectionLoop()
        {
            this.simulationContext.ConnectionLoopSettings.NewLoop();
        }

        public void NewPropertiesLoop()
        {
            this.simulationContext.PropertiesLoopSettings.NewLoop();
        }

        private void DeleteAllStateActors()
        {
            var prefix = this.GetDictKey(string.Empty);

            var toRemove = new List<string>();
            foreach (var actor in this.deviceStateActors)
            {
                // TODO: make this simpler, e.g. store the list of keys
                if (actor.Key.StartsWith(prefix))
                {
                    toRemove.Add(actor.Key);
                }
            }

            toRemove.ForEach(x => this.deviceStateActors.Remove(x, out _));
        }

        private void DeleteAllConnectionActors()
        {
            var prefix = this.GetDictKey(string.Empty);

            var toRemove = new List<string>();
            foreach (var actor in this.deviceConnectionActors)
            {
                // TODO: make this simpler, e.g. store the list of keys
                if (actor.Key.StartsWith(prefix))
                {
                    actor.Value.Stop();
                    toRemove.Add(actor.Key);
                }
            }

            toRemove.ForEach(x => this.deviceConnectionActors.Remove(x, out _));
        }

        private void DeleteAllTelemetryActors()
        {
            var prefix = this.GetDictKey(string.Empty);

            var toRemove = new List<string>();
            foreach (var actor in this.deviceTelemetryActors)
            {
                // TODO: make this simpler, e.g. store the list of keys
                if (actor.Key.StartsWith(prefix))
                {
                    actor.Value.Stop();
                    toRemove.Add(actor.Key);
                }
            }

            toRemove.ForEach(x => this.deviceTelemetryActors.Remove(x, out _));
        }

        private void DeleteAllPropertiesActors()
        {
            var prefix = this.GetDictKey(string.Empty);

            var toRemove = new List<string>();
            foreach (var actor in this.devicePropertiesActors)
            {
                // TODO: make this simpler, e.g. store the list of keys
                if (actor.Key.StartsWith(prefix))
                {
                    actor.Value.Stop();
                    toRemove.Add(actor.Key);
                }
            }

            toRemove.ForEach(x => this.devicePropertiesActors.Remove(x, out _));
        }

        private void DeleteAllReplayActors()
        {
            var prefix = this.GetDictKey(string.Empty);

            var toRemove = new List<string>();
            foreach (var actor in this.deviceReplayActors)
            {
                // TODO: make this simpler, e.g. store the list of keys
                if (actor.Key.StartsWith(prefix))
                {
                    actor.Value.Stop();
                    toRemove.Add(actor.Key);
                }
            }

            toRemove.ForEach(x => this.deviceReplayActors.Remove(x, out _));
        }

        private int DeleteActorsForPartition(DevicesPartition partition)
        {
            var count = 0;

            foreach (var x in partition.DeviceIdsByModel)
            {
                List<string> deviceIds = x.Value;
                foreach (var deviceId in deviceIds)
                {
                    this.DeleteDeviceActors(deviceId);
                    this.deviceCount--;
                    count++;
                }
            }

            return count;
        }

        private void DeleteDeviceActors(string deviceId)
        {
            this.log.Debug("Deleting device actors...", () => new { deviceId });

            var dictKey = this.GetDictKey(deviceId);

            var telemetryDictPrefix = this.GetTelemetryDictKey(dictKey, string.Empty);

            this.deviceStateActors.TryRemove(dictKey, out _);
            this.deviceConnectionActors.TryRemove(dictKey, out _);
            this.devicePropertiesActors.TryRemove(dictKey, out _);
            this.deviceReplayActors.TryRemove(dictKey, out _);

            var toRemove = new List<string>();
            foreach (var actor in this.deviceTelemetryActors)
            {
                // TODO: make this simpler, e.g. store the list of keys
                if (actor.Key.StartsWith(telemetryDictPrefix))
                {
                    actor.Value.Stop();
                    toRemove.Add(actor.Key);
                }
            }
        }

        private async Task<bool> TryToCreateActorsForPartitionAsync(DevicesPartition partition)
        {
            var count = 0;

            // Prepare the models first, without creating the actors yet, to handle failures before creating actors
            var deviceModelsData = new List<Tuple<DeviceModel, List<string>>>();
            foreach (var x in partition.DeviceIdsByModel)
            {
                string deviceModelId = x.Key;
                List<string> deviceIds = x.Value;

                try
                {
                    DeviceModel deviceModel = await this.deviceModels.GetWithOverrideAsync(deviceModelId, this.simulation);
                    deviceModelsData.Add(new Tuple<DeviceModel, List<string>>(deviceModel, deviceIds));
                }
                catch (ResourceNotFoundException)
                {
                    // Log and continue ignoring the device model
                    this.log.Error("The device model doesn't exist", () => new { deviceModelId });
                }
                catch (Exception e)
                {
                    this.log.Error("Unexpected error while preparing the device model or starting device actors", () => new { deviceModelId, e });
                    return false;
                }
            }

            // Create all the actors, no exceptions to handle here
            foreach (Tuple<DeviceModel, List<string>> deviceModelData in deviceModelsData)
            {
                DeviceModel deviceModel = deviceModelData.Item1;
                List<string> deviceIds = deviceModelData.Item2;

                foreach (var deviceId in deviceIds)
                {
                    this.CreateActorsForDevice(deviceId, deviceModel, this.deviceCount);
                    this.deviceCount++;
                    count++;

                    // Set ActualStartTime if required
                    if (!this.simulation.ActualStartTime.HasValue)
                    {
                        this.simulation.ActualStartTime = DateTimeOffset.UtcNow;
                        await this.simulations.UpsertAsync(this.simulation, false);
                    }
                }
            }

            this.log.Info("Devices added to the node and started",
                () => new { Simulation = this.simulation.Id, DeviceAddedCount = count, TotalDeviceCount = this.deviceCount });

            return true;
        }

        /**
         * For each device create one actor to periodically update the internal state,
         * one actor to manage the connection to the hub, and one actor for each
         * telemetry message to send.   ......................
         */
        private void CreateActorsForDevice(string deviceId, DeviceModel deviceModel, int deviceCounter)
        {
            this.log.Debug("Creating device actors...",
                () => new { deviceId, ModelName = deviceModel.Name, ModelId = deviceModel.Id, deviceCounter });

            var dictKey = this.GetDictKey(deviceId);

            // Create device actors for either replay or non-replay simulations
            if (string.IsNullOrEmpty(this.simulation.ReplayFileId))
            {
                // Create one state actor for each device
                var deviceStateActor = this.factory.Resolve<IDeviceStateActor>();
                deviceStateActor.Init(this.simulationContext, deviceId, deviceModel, deviceCounter);
                this.deviceStateActors.AddOrUpdate(dictKey, deviceStateActor, (k, v) => deviceStateActor);

                // Create one connection actor for each device
                var deviceContext = this.factory.Resolve<IDeviceConnectionActor>();
                deviceContext.Init(this.simulationContext, deviceId, deviceModel, deviceStateActor, this.simulationContext.ConnectionLoopSettings);
                this.deviceConnectionActors.AddOrUpdate(dictKey, deviceContext, (k, v) => deviceContext);

                // Create one device properties actor for each device
                var devicePropertiesActor = this.factory.Resolve<IDevicePropertiesActor>();
                devicePropertiesActor.Init(this.simulationContext, deviceId, deviceStateActor, deviceContext, this.simulationContext.PropertiesLoopSettings);
                this.devicePropertiesActors.AddOrUpdate(dictKey, devicePropertiesActor, (k, v) => devicePropertiesActor);

                // Create one telemetry actor for each telemetry message to be sent
                var i = 0;
                foreach (var message in deviceModel.Telemetry)
                {
                    // Skip telemetry without an interval set
                    if (message.Interval.TotalMilliseconds <= 0)
                    {
                        this.log.Warn("Skipping telemetry with interval = 0",
                            () => new { model = deviceModel.Id, message });
                        continue;
                    }

                    var deviceTelemetryActor = this.factory.Resolve<IDeviceTelemetryActor>();
                    deviceTelemetryActor.Init(this.simulationContext, deviceId, deviceModel, message, deviceStateActor, deviceContext);

                    var actorKey = this.GetTelemetryDictKey(dictKey, (i++).ToString());
                    this.deviceTelemetryActors.AddOrUpdate(actorKey, deviceTelemetryActor, (k, v) => deviceTelemetryActor);
                }
            }
            else {
                // Create one state actor for each device
                var deviceStateActor = this.factory.Resolve<IDeviceStateActor>();
                deviceStateActor.Init(this.simulationContext, deviceId, deviceModel, deviceCounter);
                this.deviceStateActors.AddOrUpdate(dictKey, deviceStateActor, (k, v) => deviceStateActor);

                // Create one connection actor for each device
                var deviceContext = this.factory.Resolve<IDeviceConnectionActor>();
                deviceContext.Init(this.simulationContext, deviceId, deviceModel, deviceStateActor, this.simulationContext.ConnectionLoopSettings);
                this.deviceConnectionActors.AddOrUpdate(dictKey, deviceContext, (k, v) => deviceContext);

                // Create one device replay actor for each device
                var deviceReplayActor = this.factory.Resolve<IDeviceReplayActor>();
                deviceReplayActor.Init(this.simulationContext, deviceId, deviceModel, deviceContext);
                this.deviceReplayActors.AddOrUpdate(dictKey, deviceReplayActor, (k, v) => deviceReplayActor);
            }
        }

        private string GetDictKey(string deviceId)
        {
            return this.simulation.Id + ACTOR_PREFIX_SEPARATOR + deviceId;
        }

        private string GetTelemetryDictKey(string deviceDictPrefix, string messageId)
        {
            return deviceDictPrefix + ACTOR_TELEMETRY_PREFIX_SEPARATOR + messageId;
        }
    }
}
