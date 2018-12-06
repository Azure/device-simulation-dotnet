// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Clustering;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage.CosmosDbSql;
using Moq;
using Newtonsoft.Json;
using Services.Test.helpers;
using Xunit;
using SimulationModel = Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation;

namespace Services.Test.Clustering
{
    public class DevicePartitionsTest
    {
        private const string PARTITIONS = "partitions";
        const string SIM_ID = "12345";

        private readonly DevicePartitions target;

        private readonly Mock<IClusteringConfig> clusteringConfig;
        private readonly Mock<IServicesConfig> config;
        private readonly Mock<ISimulations> simulations;
        private readonly Mock<IEngines> enginesFactory;
        private readonly Mock<ILogger> log;
        private readonly Mock<IEngine> partitionsStorage;
        private readonly Mock<IClusterNodes> clusterNodes;

        public DevicePartitionsTest()
        {
            this.clusteringConfig = new Mock<IClusteringConfig>();
            this.config = new Mock<IServicesConfig>();
            this.simulations = new Mock<ISimulations>();
            this.enginesFactory = new Mock<IEngines>();
            this.log = new Mock<ILogger>();
            this.clusterNodes = new Mock<IClusterNodes>();

            // Inject configuration settings with a collection name which is then used
            // to intercept the call to .InitAsync()
            this.config.SetupGet(x => x.PartitionsStorage)
                .Returns(new Config { CosmosDbSqlCollection = PARTITIONS });

            // Intercept the call to IStorageRecords.InitAsync() and return the right storage mock
            this.partitionsStorage = new Mock<IEngine>();
            this.partitionsStorage
                .Setup(x => x.Init(It.Is<Config>(c => c.CosmosDbSqlCollection == PARTITIONS)))
                .Returns(this.partitionsStorage.Object);
            this.partitionsStorage.Setup(x => x.BuildRecord(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string id, string json) => new DataRecord { Id = id, Data = json });

            // When IStorageRecords is instantiated, return the factory above
            this.enginesFactory.Setup(x => x.Build(It.IsAny<Config>())).Returns(this.partitionsStorage.Object);

            this.target = this.GetTargetInstance(1000);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesAPartitionContainingDevicesInTheSimulationLoadedFromStorage()
        {
            // Arrange
            const string MODEL1 = "foo-bar";
            const string MODEL2 = "bar-baz";
            const string DEVICE1 = "j1";
            const string DEVICE2 = "j2";
            const string DEVICE3 = "b3";
            var simulation = new SimulationModel { Id = SIM_ID, PartitioningComplete = false };
            var deviceIdsByModel = new Dictionary<string, List<string>>
            {
                { MODEL1, new List<string> { DEVICE1, DEVICE2 } },
                { MODEL2, new List<string> { DEVICE3 } }
            };
            this.simulations.Setup(x => x.GetAsync(SIM_ID)).ReturnsAsync(simulation);
            this.simulations.Setup(x => x.GetDeviceIdsByModel(simulation)).Returns(deviceIdsByModel);

            // Act
            this.target.CreateAsync(SIM_ID).CompleteOrTimeout();

            // Assert
            this.simulations.Verify(x => x.GetAsync(SIM_ID), Times.Once);
            this.simulations.Verify(x => x.GetDeviceIdsByModel(simulation), Times.Once);
            this.partitionsStorage.Verify(
                x => x.CreateAsync(It.Is<IDataRecord>(r => PartitionContainsModel(r.GetData(), MODEL1))),
                Times.Once);
            this.partitionsStorage.Verify(
                x => x.CreateAsync(It.Is<IDataRecord>(r => PartitionContainsModel(r.GetData(), MODEL2))),
                Times.Once);
            this.partitionsStorage.Verify(
                x => x.CreateAsync(It.Is<IDataRecord>(r => PartitionContainsDevice(r.GetData(), DEVICE1))),
                Times.Once);
            this.partitionsStorage.Verify(
                x => x.CreateAsync(It.Is<IDataRecord>(r => PartitionContainsDevice(r.GetData(), DEVICE2))),
                Times.Once);
            this.partitionsStorage.Verify(
                x => x.CreateAsync(It.Is<IDataRecord>(r => PartitionContainsDevice(r.GetData(), DEVICE3))),
                Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntRequireConnectionStringValidatinoWhenUpdatingASimulation()
        {
            // Arrange
            var simulation = new SimulationModel { Id = SIM_ID };
            this.simulations.Setup(x => x.GetAsync(SIM_ID)).ReturnsAsync(simulation);
            this.simulations.Setup(x => x.GetDeviceIdsByModel(simulation)).Returns(new Dictionary<string, List<string>>());

            // Act
            this.target.CreateAsync(SIM_ID).CompleteOrTimeout();

            // Assert
            this.simulations.Verify(x => x.UpsertAsync(It.IsAny<SimulationModel>(), false), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDeletesLeftoverPartitionsBeforeCreatingThem()
        {
            // Arrange
            this.SetupAGenericSimulationWithSomeDevices(SIM_ID);

            // Act
            this.target.CreateAsync(SIM_ID).CompleteOrTimeout();

            // Assert
            this.partitionsStorage.Verify(x => x.DeleteAsync(It.Is<string>(s => s.StartsWith(SIM_ID))), Times.Once());
            this.partitionsStorage.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Once());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItChangesTheSimulationStatusAfterCreatingThePartitions()
        {
            // Arrange
            this.SetupAGenericSimulationWithSomeDevices(SIM_ID);

            // Act
            this.target.CreateAsync(SIM_ID).CompleteOrTimeout();

            // Assert
            this.simulations.Verify(x => x.UpsertAsync(It.Is<SimulationModel>(s => s.Id == SIM_ID && s.PartitioningComplete), false), Times.Once);
            this.simulations.Verify(x => x.UpsertAsync(It.IsAny<SimulationModel>(), false), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntChangeTheSimulationStatusIfTheDeviceCreationFails()
        {
            // Arrange
            this.partitionsStorage.Setup(x => x.DeleteAsync(It.IsAny<string>())).Throws<SomeException>();
            this.SetupAGenericSimulationWithSomeDevices(SIM_ID);

            // Act
            Assert.ThrowsAsync<SomeException>(() => this.target.CreateAsync(SIM_ID)).CompleteOrTimeout();

            // Assert
            this.partitionsStorage.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.AtLeastOnce);
            this.simulations.Verify(x => x.UpsertAsync(It.IsAny<SimulationModel>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesTheRightNumberOfPartitions()
        {
            // Arrange
            var partitionSize = 3;
            var simulation = new SimulationModel { Id = SIM_ID, PartitioningComplete = false };
            var deviceIdsByModel = new Dictionary<string, List<string>>
            {
                { "m1", new List<string> { "d1", "d2", "d3" } },
                { "m2", new List<string> { "j3" } },
                { "m3", new List<string> { "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11" } }
            };
            this.simulations.Setup(x => x.GetAsync(SIM_ID)).ReturnsAsync(simulation);
            this.simulations.Setup(x => x.GetDeviceIdsByModel(simulation)).Returns(deviceIdsByModel);

            // Act
            var instance = this.GetTargetInstance(partitionSize);
            instance.CreateAsync(SIM_ID).CompleteOrTimeout();

            // Assert
            this.partitionsStorage.Verify(x => x.CreateAsync(It.IsAny<IDataRecord>()), Times.Exactly(4));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntPartitionASimulationAlreadyPartitioned()
        {
            // Arrange
            this.SetupAGenericSimulationWithSomeDevices(SIM_ID, true);

            // Act
            this.target.CreateAsync(SIM_ID).CompleteOrTimeout();

            // Assert
            this.partitionsStorage.Verify(x => x.CreateAsync(It.IsAny<IDataRecord>()), Times.Never);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItGetsAllPartitionsFromStorage()
        {
            // Arrange
            var list = new List<IDataRecord>
            {
                new DataRecord { Data = JsonConvert.SerializeObject(new DevicesPartition()) },
                new DataRecord { Data = JsonConvert.SerializeObject(new DevicesPartition()) },
                new DataRecord { Data = JsonConvert.SerializeObject(new DevicesPartition()) }
            };
            this.partitionsStorage.Setup(x => x.GetAllAsync())
                .ReturnsAsync(list);

            // Act
            IList<DevicesPartition> result = this.target.GetAllAsync().CompleteOrTimeout().Result;

            // Assert
            this.partitionsStorage.Verify(x => x.GetAllAsync(), Times.Once);
            Assert.Equal(3, result.Count);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanDeleteAListOfPartitionsFromStorage()
        {
            // Arrange
            var list = new List<string> { "1", "2", "3", "x" };

            // Act
            this.target.DeleteListAsync(list).CompleteOrTimeout();

            // Assert
            this.partitionsStorage.Verify(x => x.DeleteMultiAsync(list), Times.Once);
        }

        private DevicePartitions GetTargetInstance(int maxPartitionSize)
        {
            this.clusteringConfig.SetupGet(x => x.MaxPartitionSize)
                .Returns(maxPartitionSize);

            return new DevicePartitions(
                this.config.Object,
                this.clusteringConfig.Object,
                this.simulations.Object,
                this.clusterNodes.Object,
                this.enginesFactory.Object,
                this.log.Object);
        }

        private void SetupAGenericSimulationWithSomeDevices(string simulationId, bool complete = false)
        {
            var simulation = new SimulationModel
            {
                Id = simulationId,
                PartitioningComplete = complete
            };
            var deviceIdsByModel = new Dictionary<string, List<string>>
            {
                { "foo-bar", new List<string> { "d1" } },
                { "somemodel", new List<string> { "l1", "l2" } }
            };
            this.simulations.Setup(x => x.GetAsync(simulationId)).ReturnsAsync(simulation);
            this.simulations.Setup(x => x.GetDeviceIdsByModel(simulation)).Returns(deviceIdsByModel);
        }

        private static bool PartitionContainsModel(string json, string model)
        {
            var partition = JsonConvert.DeserializeObject<DevicesPartition>(json);
            return partition.DeviceIdsByModel.ContainsKey(model);
        }

        private static bool PartitionContainsDevice(string json, string deviceId)
        {
            var partition = JsonConvert.DeserializeObject<DevicesPartition>(json);
            return partition.DeviceIdsByModel.SelectMany(x => x.Value).Contains(deviceId);
        }
    }
}
