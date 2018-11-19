﻿// Copyright (c) Microsoft. All rights reserved.

using System;
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
        private readonly Mock<IConnectionStrings> connectionStrings;
        private readonly Mock<IRegistryManager> registry;
        private readonly Mock<IDeviceClientWrapper> deviceClient;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IDiagnosticsLogger> diagnosticsLogger;

        public DevicesTest(ITestOutputHelper log)
        {
            this.log = log;

            this.config = new Mock<IServicesConfig>();
            this.connectionStrings = new Mock<IConnectionStrings>();
            this.registry = new Mock<IRegistryManager>();
            this.deviceClient = new Mock<IDeviceClientWrapper>();
            this.logger = new Mock<ILogger>();
            this.diagnosticsLogger = new Mock<IDiagnosticsLogger>();

            IInstance instance = new Instance(this.logger.Object);

            this.target = new Devices(
                this.config.Object,
                this.connectionStrings.Object,
                this.registry.Object,
                this.deviceClient.Object,
                this.logger.Object,
                this.diagnosticsLogger.Object,
                instance);

            this.connectionStrings
                .Setup(x => x.GetAsync())
                .ReturnsAsync("HostName=iothub-AAAA.azure-devices.net;SharedAccessKeyName=AAAA;SharedAccessKey=AAAA");

            this.target.InitAsync().Wait(Constants.TEST_TIMEOUT);
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
    }
}
