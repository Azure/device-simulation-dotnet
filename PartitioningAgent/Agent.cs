// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;

namespace Microsoft.Azure.IoTSolutions.DeviceSimulation.PartitioningAgent
{
    public interface IPartitioningAgent
    {
        Task StartAsync();
        void Stop();
    }

    public class Agent : IPartitioningAgent
    {
        private readonly IClusterNodes clusterNodesNodes;
        private readonly IDevicePartitions partitions;
        private readonly ISimulations simulations;
        private readonly ILogger log;
        private readonly IThreadWrapper thread;
        private readonly IFactory factory;
        private readonly int checkIntervalMsecs;
        private bool running;

        public Agent(
            IClusterNodes clusterNodesNodes,
            IDevicePartitions partitions,
            ISimulations simulations,
            IThreadWrapper thread,
            IClusteringConfig clusteringConfig,
            IFactory factory,
            ILogger logger)
        {
            this.clusterNodesNodes = clusterNodesNodes;
            this.partitions = partitions;
            this.simulations = simulations;
            this.thread = thread;
            this.factory = factory;
            this.log = logger;
            this.running = true;
            this.checkIntervalMsecs = clusteringConfig.CheckIntervalMsecs;
        }

        public async Task StartAsync()
        {
            this.log.Info("Partitioning agent started", () => new { Node = this.clusterNodesNodes.GetCurrentNodeId() });

            // Repeat until the agent is stopped
            while (this.running)
            {
                await this.clusterNodesNodes.KeepAliveNodeAsync(); // #1

                // Only one process in the network becomes a master, and the master
                // is responsible for running tasks not meant to run in parallel, like
                // creating devices and partitions, and other tasks
                var isMaster = await this.clusterNodesNodes.SelfElectToMasterNodeAsync(); // #2
                if (isMaster)
                {
                    // #4 : Load all simulations to have fresh status and new simulations
                    IList<Simulation> activeSimulations = (await this.simulations.GetListAsync())
                        .Where(x => x.IsActiveNow).ToList();
                    this.log.Debug("Active simulations loaded", () => new { activeSimulations.Count });

                    // Note: do not run in parallel to avoid conflicting db writes and retries
                    await this.clusterNodesNodes.RemoveStaleNodesAsync(); // #3
                    await this.CreateDevicesAsync(activeSimulations);
                    await this.CreatePartitionsAsync(activeSimulations); // #5 #6 #7
                    await this.DeletePartitionsAsync(activeSimulations); // #8
                }

                this.thread.Sleep(this.checkIntervalMsecs);
            }
        }

        public void Stop()
        {
            this.running = false;
        }

        // =========================================================================================================
        // =========================================================================================================
        // =========================================================================================================

        private async Task CreateDevicesAsync(IList<Simulation> activeSimulations)
        {
            if (activeSimulations.Count == 0) return;

            var simulationsWithDevicesToCreate = activeSimulations.Where(x => x.DeviceCreationRequired).ToList();

            if (simulationsWithDevicesToCreate.Count == 0)
            {
                this.log.Debug("No simulations require device creation");
                return;
            }

            foreach (var simulation in simulationsWithDevicesToCreate)
            {
                await this.CreateIoTHubDevicesAsync(simulation);
            }
        }

        private async Task CreateIoTHubDevicesAsync(Simulation simulation)
        {
            var creationFailed = false;

            // Check if the device creation is complete
            if (simulation.DevicesCreationStarted)
            {
                this.log.Info("Checking if the device creation is complete...", () => new { SimulationId = simulation.Id });

                var deviceService = this.factory.Resolve<IDevices>();
                await deviceService.InitAsync(simulation);

                if (await deviceService.IsJobCompleteAsync(simulation.DeviceCreationJobId, () => { creationFailed = true; }))
                {
                    this.log.Info("All devices have been created, updating the simulation record", () => new { SimulationId = simulation.Id });

                    if (await this.simulations.TryToSetDeviceCreationCompleteAsync(simulation.Id))
                    {
                        this.log.Debug("Simulation record updated");
                    }
                    else
                    {
                        this.log.Warn("Failed to update the simulation record, will retry later");
                    }
                }
                else
                {
                    this.log.Info("Device creation is still in progress", () => new { SimulationId = simulation.Id });
                }
            }

            // Start the job to import the devices
            if ((!simulation.DevicesCreationStarted && !simulation.DevicesCreationComplete) || creationFailed)
            {
                this.log.Debug("Starting devices creation", () => new { SimulationId = simulation.Id });

                var deviceService = this.factory.Resolve<IDevices>();
                await deviceService.InitAsync(simulation);

                if (await this.simulations.TryToStartDevicesCreationAsync(simulation.Id, deviceService))
                {
                    this.log.Info("Device creation started");
                }
                else
                {
                    this.log.Warn("Failed to start device creation, will retry later");
                }
            }
        }

        private async Task CreatePartitionsAsync(IList<Simulation> activeSimulations)
        {
            if (activeSimulations.Count == 0) return;

            var simulationsToPartition = activeSimulations.Where(x => x.PartitioningRequired).ToList();

            if (simulationsToPartition.Count == 0)
            {
                this.log.Debug("No simulations to be partitioned");
                return;
            }

            foreach (Simulation sim in simulationsToPartition)
            {
                await this.partitions.CreateAsync(sim.Id);
            }
        }

        private async Task DeletePartitionsAsync(IList<Simulation> activeSimulations)
        {
            if (activeSimulations.Count == 0) return;

            this.log.Debug("Searching partitions to delete...");

            var allPartitions = await this.partitions.GetAllAsync();
            var simulationIds = new HashSet<string>(activeSimulations.Select(x => x.Id));
            var partitionIds = new List<string>();
            foreach (var partition in allPartitions)
            {
                if (!simulationIds.Contains(partition.SimulationId))
                {
                    partitionIds.Add(partition.Id);
                }
            }

            if (partitionIds.Count == 0)
            {
                this.log.Debug("No partitions to delete");
                return;
            }

            // TODO: partitions should be deleted only after its actors are down
            this.log.Debug("Deleting partitions...", () => new { partitionIds.Count });
            await this.partitions.DeleteListAsync(partitionIds);
        }
    }
}
