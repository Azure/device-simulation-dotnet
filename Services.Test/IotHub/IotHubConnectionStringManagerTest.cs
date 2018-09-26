// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.IotHub
{
    public class IotHubConnectionStringManagerTest
    {
        private readonly Mock<ILogger> mockLogger;
        private readonly Mock<IDiagnosticsLogger> mockDiagnosticsLogger;
        private readonly Mock<IFactory> mockFactory;
        private readonly IServicesConfig config;
        private readonly IotHubConnectionStringManager target;

        public IotHubConnectionStringManagerTest()
        {
            this.mockLogger = new Mock<ILogger>();
            this.mockDiagnosticsLogger = new Mock<IDiagnosticsLogger>();
            this.mockFactory = new Mock<IFactory>();
            this.mockFactory.Setup(x => x.Resolve<IStorageRecords>()).Returns(new Mock<IStorageRecords>().Object);

            this.config = new ServicesConfig();
            this.target = new IotHubConnectionStringManager(this.config, this.mockFactory.Object, this.mockDiagnosticsLogger.Object, this.mockLogger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsOnInvalidConnStringFormat()
        {
            // Assert
            Assert.ThrowsAsync<InvalidIotHubConnectionStringFormatException>(
                    async () => await this.target.RedactAndSaveAsync("foobar"))
                .Wait(Constants.TEST_TIMEOUT);
        }
    }
}
