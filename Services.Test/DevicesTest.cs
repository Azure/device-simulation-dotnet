// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Moq;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace Services.Test
{
    public class DevicesTest
    {
        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper log;

        private readonly Devices target;
        private readonly Mock<IServicesConfig> config;
        private readonly Mock<IIotHubConnectionStringManager> connectionStringManager;
        private readonly Mock<IRegistryManager> registry;
        private readonly Mock<IDeviceClientWrapper> deviceClient;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IDiagnosticsLogger> diagnosticsLogger;
        private readonly Mock<IInstance> instance;

        public DevicesTest(ITestOutputHelper log)
        {
            this.log = log;

            this.config = new Mock<IServicesConfig>();
            this.connectionStringManager = new Mock<IIotHubConnectionStringManager>();
            this.registry = new Mock<IRegistryManager>();
            this.deviceClient = new Mock<IDeviceClientWrapper>();
            this.logger = new Mock<ILogger>();
            this.diagnosticsLogger = new Mock<IDiagnosticsLogger>();
            this.instance = new Mock<IInstance>();

            this.target = new Devices(
                this.config.Object,
                this.connectionStringManager.Object,
                this.registry.Object,
                this.deviceClient.Object,
                this.logger.Object,
                this.diagnosticsLogger.Object,
                this.instance.Object);

            this.connectionStringManager
                .Setup(x => x.GetConnectionStringAsync())
                .ReturnsAsync("HostName=iothub-AAAA.azure-devices.net;SharedAccessKeyName=AAAA;SharedAccessKey=AAAA");
        }

        /** 
         * Any exception while creating a device needs to be rethrown
         * so that the simulation will retry. Do not return null, otherwise
         * the device actors will assume a device object is ready to use
         * and get into an invalid state.
         */
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsWhenCreationTimesOut()
        {
            // Arrange
            var simulation = new Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.Simulation();
            this.target.InitAsync(simulation).CompleteOrTimeout();

            // Case 1: the code uses async, and the exception surfaces explicitly

            // Arrange
            this.registry.Setup(x => x.AddDeviceAsync(It.IsAny<Device>())).Throws<TaskCanceledException>();

            // Act+Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.CreateAsync("a-device-id"))
                .Wait(Constants.TEST_TIMEOUT);

            // Case 2: the code uses Wait(), and the exception is wrapped in AggregateException

            // Arrange
            var e = new AggregateException(new TaskCanceledException());
            this.registry.Setup(x => x.AddDeviceAsync(It.IsAny<Device>())).Throws(e);

            // Act+Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.CreateAsync("a-device-id"))
                .Wait(Constants.TEST_TIMEOUT);
        }

        [Fact(Skip = "needs some refactoring in Devices.cs to allow mocking the Storage classes"), Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCreatesDevicesUsingAJob()
        {
            // TODO: needs some refactoring in Devices.cs to allow mocking the Storage classes

            // Arrange
            var list = new List<string> { "AA", "BB", "" };

            // Act
            var result = this.target.CreateListUsingJobsAsync(list).CompleteOrTimeout().Result;

            // Assert
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItReportsIfAJobIsComplete()
        {
            // Arrange
            var failures1 = 0;
            var failures2 = 0;
            var jobId1 = Guid.NewGuid().ToString();
            var jobId2 = Guid.NewGuid().ToString();
            var jobId3 = Guid.NewGuid().ToString();
            var jobId4 = Guid.NewGuid().ToString();
            this.registry.Setup(x => x.GetJobAsync(jobId1))
                .ReturnsAsync(new JobProperties { Status = JobStatus.Unknown });
            this.registry.Setup(x => x.GetJobAsync(jobId2))
                .ReturnsAsync(new JobProperties { Status = JobStatus.Running });
            this.registry.Setup(x => x.GetJobAsync(jobId3))
                .ReturnsAsync(new JobProperties { Status = JobStatus.Completed });
            this.registry.Setup(x => x.GetJobAsync(jobId4))
                .ReturnsAsync(new JobProperties { Status = JobStatus.Failed });

            // Act
            var result1 = this.target.IsJobCompleteAsync(jobId1, () => { failures1++; }).CompleteOrTimeout().Result;
            var result2 = this.target.IsJobCompleteAsync(jobId2, () => { failures1++; }).CompleteOrTimeout().Result;
            var result3 = this.target.IsJobCompleteAsync(jobId3, () => { failures1++; }).CompleteOrTimeout().Result;
            var result4 = this.target.IsJobCompleteAsync(jobId4, () => { failures2++; }).CompleteOrTimeout().Result;

            // Assert
            Assert.False(result1);
            Assert.False(result2);
            Assert.True(result3);
            Assert.False(result4);
            Assert.Equal(0, failures1);
            Assert.Equal(1, failures2);
        }
    }
}
