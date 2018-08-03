// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test
{
    public class DeviceClientTest
    {
        private readonly DeviceClient target;
        private readonly Mock<IDeviceClientWrapper> client;
        private readonly Mock<IDeviceMethods> deviceMethods;
        private readonly Mock<ILogger> log;

        public DeviceClientTest()
        {
            this.client = new Mock<IDeviceClientWrapper>();
            this.deviceMethods = new Mock<IDeviceMethods>();
            this.log = new Mock<ILogger>();

            this.target = new DeviceClient(
                "x",
                IoTHubProtocol.AMQP,
                this.client.Object,
                this.deviceMethods.Object,
                this.log.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ConnectsToIoTHub()
        {
            // Act (connect twice, the second call should be ignored)
            this.target.ConnectAsync().Wait(Constants.TEST_TIMEOUT);
            this.target.ConnectAsync().Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.client.Verify(x=>x.OpenAsync(), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void AuthFailureOnConnectRaiseException()
        {
            // Arrange
            this.client.Setup(x => x.OpenAsync()).Throws(new UnauthorizedException(""));
            
            // Act + Assert
            Assert.ThrowsAsync<DeviceAuthFailedException>(
                async () => await this.target.ConnectAsync()).Wait(Constants.TEST_TIMEOUT);
        }
    }
}
