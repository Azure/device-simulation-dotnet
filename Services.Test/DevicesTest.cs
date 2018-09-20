// Copyright (c) Microsoft. All rights reserved.

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
        /// <summary>The test mockLogger</summary>
        private readonly ITestOutputHelper log;

        private readonly Devices target;
        private readonly Mock<IServicesConfig> mockServicesConfig;
        private readonly Mock<IIotHubConnectionStringManager> mockConnectionStringManager;
        private readonly Mock<IRegistryManager> mockRegistryManager;
        private readonly Mock<IDeviceClientWrapper> mockDeviceClient;
        private readonly Mock<ILogger> mockLogger;
        private readonly Mock<IDiagnosticsLogger> mockDiagnosticsLogger;
        private readonly Mock<IInstance> mockInstance;

        public DevicesTest(ITestOutputHelper log)
        {
            this.log = log;

            this.mockServicesConfig = new Mock<IServicesConfig>();
            this.mockConnectionStringManager = new Mock<IIotHubConnectionStringManager>();
            this.mockRegistryManager = new Mock<IRegistryManager>();
            this.mockDeviceClient = new Mock<IDeviceClientWrapper>();
            this.mockLogger = new Mock<ILogger>();
            this.mockDiagnosticsLogger = new Mock<IDiagnosticsLogger>();
            this.mockInstance = new Mock<IInstance>();

            this.target = new Devices(
                this.mockServicesConfig.Object,
                this.mockConnectionStringManager.Object,
                this.mockRegistryManager.Object,
                this.mockDeviceClient.Object,
                this.mockLogger.Object,
                this.mockDiagnosticsLogger.Object,
                this.mockInstance.Object);

            this.mockConnectionStringManager
                .Setup(x => x.GetIotHubConnectionStringAsync())
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
            // Case 1: the code uses async, and the exception surfaces explicitly

            // Arrange
            this.mockRegistryManager.Setup(x => x.AddDeviceAsync(It.IsAny<Device>())).Throws<TaskCanceledException>();

            // Act+Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.CreateAsync("a-device-id"))
                .Wait(Constants.TEST_TIMEOUT);

            // Case 2: the code uses Wait(), and the exception is wrapped in AggregateException

            // Arrange
            var e = new AggregateException(new TaskCanceledException());
            this.mockRegistryManager.Setup(x => x.AddDeviceAsync(It.IsAny<Device>())).Throws(e);

            // Act+Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.CreateAsync("a-device-id"))
                .Wait(Constants.TEST_TIMEOUT);
        }
    }
}
