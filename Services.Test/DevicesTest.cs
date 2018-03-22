// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
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

        private Mock<IServicesConfig> config;
        private Mock<IIotHubConnectionStringManager> connectionStringManager;
        private Mock<ILogger> logger;
        private readonly Mock<IRegistryManager> registry;
        private readonly Devices target;

        public DevicesTest(ITestOutputHelper log)
        {
            this.log = log;

            this.config = new Mock<IServicesConfig>();
            this.connectionStringManager = new Mock<IIotHubConnectionStringManager>();
            this.registry = new Mock<IRegistryManager>();
            this.logger = new Mock<ILogger>();

            this.target = new Devices(
                this.config.Object,
                this.connectionStringManager.Object,
                this.registry.Object,
                this.logger.Object);

            this.connectionStringManager
                .Setup(x => x.GetIotHubConnectionString())
                .Returns("HostName=iothub-AAAA.azure-devices.net;SharedAccessKeyName=AAAA;SharedAccessKey=AAAA");
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
            // Case 1: the code uses async, and the exception surfaces explicitly
            
            // Arrange
            this.registry.Setup(x => x.AddDeviceAsync(It.IsAny<Device>())).Throws<TaskCanceledException>();

            // Act+Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                () => this.target.CreateAsync("a-device-id")).Wait();
            
            // Case 2: the code uses Wait(), and the exception is wrapped in AggregateException
            
            // Arrange
            var e = new AggregateException(new TaskCanceledException());
            this.registry.Setup(x => x.AddDeviceAsync(It.IsAny<Device>())).Throws(e);

            // Act+Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                () => this.target.CreateAsync("a-device-id")).Wait();
        }
    }
}
