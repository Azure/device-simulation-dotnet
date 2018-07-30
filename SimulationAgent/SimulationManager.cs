// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DeviceModels;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceConnection;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceProperties;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceTelemetry;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent
{
    public interface ISimulationManager
    {
        Task InitAsync(
            Simulation simulation,
            ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors,
            ConcurrentDictionary<string, IDeviceConnectionActor> deviceConnectionActors,
            ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors,
            ConcurrentDictionary<string, IDevicePropertiesActor> devicePropertiesActors);
        
        // Check if the cluster size has changed and act accordingly
        Task SyncClusterSizeAsync();

        // Check if there are new partitions or changes, and act accordingly
        Task ManageDevicePartitionsAsync();

        // Stop all the actors and delete them
        void TearDown();

        void NewConnectionLoop();
        void NewPropertiesLoop();
    }

    public class SimulationManager : ISimulationManager
    {
        // Used to acess the actors dictionaries, which contain data about other simulations
        private const string ACTOR_PREFIX_SEPARATOR = "//";
        private const string ACTOR_TELEMETRY_PREFIX_SEPARATOR = "#";

        private readonly ISimulationContext simulationContext;
        private readonly IDevicePartitions devicePartitions;
        private readonly ICluster cluster;
        private readonly IDeviceModels deviceModels;
        private readonly IFactory factory;
        private readonly ILogger log;
        private readonly IInstance instance;
        private readonly int maxDeviceCount;

        // Data shared with other simulations
        private ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors;
        private ConcurrentDictionary<string, IDeviceConnectionActor> deviceConnectionActors;
        private ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors;
        private ConcurrentDictionary<string, IDevicePropertiesActor> devicePropertiesActors;

        // List of the device partitions assigned to this node, including their content
        // in case they disappear from storage and to reduce storage lookups
        private readonly Dictionary<string, DevicesPartition> assignedPartitions;

        private Simulation simulation;
        private int nodeCount;
        private int deviceCount;

        public SimulationManager(
            ISimulationContext simulationContext,
            IDevicePartitions devicePartitions,
            ICluster cluster,
            IDeviceModels deviceModels,
            IFactory factory,
            IClusteringConfig clusteringConfig,
            ILogger logger,
            IInstance instance)
        {
            this.simulationContext = simulationContext;
            this.devicePartitions = devicePartitions;
            this.cluster = cluster;
            this.deviceModels = deviceModels;
            this.factory = factory;
            this.log = logger;
            this.instance = instance;
            this.maxDeviceCount = clusteringConfig.MaxDevicesPerNode;

            this.assignedPartitions = new Dictionary<string, DevicesPartition>();
            this.nodeCount = 1;
            this.deviceCount = 0;
        }

        public async Task InitAsync(
            Simulation simulation,
            ConcurrentDictionary<string, IDeviceStateActor> deviceStateActors,
            ConcurrentDictionary<string, IDeviceConnectionActor> deviceConnectionActors,
            ConcurrentDictionary<string, IDeviceTelemetryActor> deviceTelemetryActors,
            ConcurrentDictionary<string, IDevicePropertiesActor> devicePropertiesActors)
        {
            this.instance.InitOnce();

            this.simulation = simulation;
            await this.simulationContext.InitAsync(simulation);

            this.deviceStateActors = deviceStateActors;
            this.deviceConnectionActors = deviceConnectionActors;
            this.deviceTelemetryActors = deviceTelemetryActors;
            this.devicePropertiesActors = devicePropertiesActors;

            this.instance.InitComplete();
        }

        // Check if the cluster size has changed and act accordingly
        public async Task SyncClusterSizeAsync()
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

        // Check if there are new partitions or changes, and act accordingly
        // and renew partition locks
        public async Task ManageDevicePartitionsAsync()
        {
            this.instance.InitRequired();

            await this.RenewPartitionAssignmentsAsync();
            await this.HandleUnassignedPartitionsAsync();
            // TODO: await this.HandleAssignedPartitionChangesAsync();
        }

        // Stop all the actors and delete them
        public void TearDown()
        {
            this.instance.InitRequired();

            this.DeleteAllStateActors();
            this.DeleteAllConnectionActors();
            this.DeleteAllTelemetryActors();
            this.DeleteAllPropertiesActors();
        }

        // Reset the connection counters
        public void NewConnectionLoop()
        {
            this.instance.InitRequired();
            this.simulationContext.NewConnectionLoop();
        }

        // Reset the device properties counters
        public void NewPropertiesLoop()
        {
            this.instance.InitRequired();
            this.simulationContext.NewPropertiesLoop();
        }

        // Count the nodes running this simulation
        private async Task<bool> UpdateNodeCountAsync()
        {
            // TODO: for now we assume that all the nodes are participating, so we just count the number of nodes
            //       however when running few devices in a big cluster, the number of nodes is 1
            //       and when running on multiple nodes the load might be unbalanced

            try
            {
                int newCount = (await this.cluster.GetSortedListAsync()).Count;
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

        private async Task RenewPartitionAssignmentsAsync()
        {
            var partitionsToUnload = new List<DevicesPartition>();

            foreach (var partition in this.assignedPartitions)
            {
                var partitionId = partition.Value.Id;

                try
                {
                    if (!await this.devicePartitions.TryToRenewPartitionAssignmentAsync(partitionId))
                    {
                        partitionsToUnload.Add(partition.Value);
                    }
                }
                catch (ResourceNotFoundException)
                {
                    this.log.Error("Partition not found, lock cannot be renewed", () => new { partitionId });
                    partitionsToUnload.Add(partition.Value);
                }
            }

            foreach (DevicesPartition partition in partitionsToUnload)
            {
                this.log.Debug("Removing partition from this node", () => new { partition.Id });
                var actorsCount = this.DeleteActorsForPartition(partition);
                this.assignedPartitions.Remove(partition.Id);
                this.log.Debug("Partition and actors removed", () => new { partition.Id, actorsCount });
            }
        }

        private async Task HandleUnassignedPartitionsAsync()
        {
            if (this.deviceCount >= this.maxDeviceCount) return;

            this.log.Debug("Searching for unassigned partitions...");
            var unassignedPartitions = await this.devicePartitions.GetUnassignedPartitionsAsync(this.simulation.Id);

            // Randomize the list, to reduce the probability of contention with other nodes
            unassignedPartitions.Shuffle();

            /**
             * Important: after assigning a partition, the following operations could fail, in which case
             * the partition should be (eventually) released, and the actors should be removed.
             */
            foreach (DevicesPartition partition in unassignedPartitions)
            {
                // Stop adding partitions as soon as the current node has enough work to do
                if (this.deviceCount >= this.maxDeviceCount)
                {
                    this.log.Debug("Maximum number of devices per node reached", () => new { this.deviceCount, this.maxDeviceCount });
                    return;
                }

                // Try to lock partition
                if (await this.devicePartitions.TryToAssignPartitionAsync(partition.Id))
                {
                    var success = await this.CreateActorsForPartitionAsync(partition);
                    if (success)
                    {
                        this.assignedPartitions.Add(partition.Id, partition);
                        this.log.Debug("Partition assigned", () => new { SimulationId = this.simulation.Id, PartitionId = partition.Id });

                        // TODO: handle exceptions
                        await this.CreateDevicesAsync(partition);
                    }
                    else
                    {
                        this.log.Error("Unexpected error while adding partition, will try to unassign it, or eventually release by lock expiration",
                            () => new { SimulationId = this.simulation.Id, PartitionId = partition.Id });

                        try
                        {
                            await this.devicePartitions.TryToReleasePartitionAsync(partition.Id);
                        }
                        catch (Exception e)
                        {
                            this.log.Error("Unexpected error while releasing the partition. The partition is assigned to this node but there are no actors running its devices.",
                                () => new { SimulationId = this.simulation.Id, PartitionId = partition.Id, e });
                        }
                    }
                }
                else
                {
                    this.log.Debug("Unable to acquire lock on partition", () => new { SimulationId = this.simulation.Id, PartitionId = partition.Id });
                }
            }
        }

        private async Task CreateDevicesAsync(DevicesPartition partition)
        {
            var deviceIds = partition.DeviceIdsByModel.SelectMany(x => x.Value).ToList();
            this.log.Debug("Creating devices...", () => new { SimulationId = this.simulation.Id, PartitionId = partition.Id, deviceIds.Count });
            await this.simulationContext.Devices.CreateListAsync(deviceIds);
            this.log.Debug("Devices created", () => new { SimulationId = this.simulation.Id, PartitionId = partition.Id, deviceIds.Count });
        }

        private async Task<bool> CreateActorsForPartitionAsync(DevicesPartition partition)
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
                }
            }

            this.log.Info("Devices added to the node and started",
                () => new { Simulation = this.simulation.Id, DeviceAddedCount = count, TotalDeviceCount = this.deviceCount });

            return true;
        }

        /**
         * For each device create one actor to periodically update the internal state,
         * one actor to manage the connection to the hub, and one actor for each
         * telemetry message to send.
         */
        private void CreateActorsForDevice(string deviceId, DeviceModel deviceModel, int deviceCounter)
        {
            this.log.Debug("Creating device actors...",
                () => new { deviceId, ModelName = deviceModel.Name, ModelId = deviceModel.Id, deviceCounter });

            var dictKey = this.GetDictKey(deviceId);

            // Create one state actor for each device
            var deviceStateActor = this.factory.Resolve<IDeviceStateActor>();
            deviceStateActor.Init(this.simulationContext, deviceId, deviceModel, deviceCounter);
            this.deviceStateActors.AddOrUpdate(dictKey, deviceStateActor, (k, v) => deviceStateActor);

            // Create one connection actor for each device
            var deviceConnectionActor = this.factory.Resolve<IDeviceConnectionActor>();
            deviceConnectionActor.Init(this.simulationContext, deviceId, deviceModel, deviceStateActor, this.simulationContext.ConnectionLoopSettings);
            this.deviceConnectionActors.AddOrUpdate(dictKey, deviceConnectionActor, (k, v) => deviceConnectionActor);

            // Create one device properties actor for each device
            var devicePropertiesActor = this.factory.Resolve<IDevicePropertiesActor>();
            devicePropertiesActor.Init(this.simulationContext, deviceId, deviceStateActor, deviceConnectionActor, this.simulationContext.PropertiesLoopSettings);
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
                deviceTelemetryActor.Init(this.simulationContext, deviceId, deviceModel, message, deviceStateActor, deviceConnectionActor);

                var actorKey = this.GetTelemetryDictKey(dictKey, (i++).ToString());
                this.deviceTelemetryActors.AddOrUpdate(actorKey, deviceTelemetryActor, (k, v) => deviceTelemetryActor);
            }
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

        private Task HandleAssignedPartitionChangesAsync()
        {
            throw new NotImplementedException();
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
