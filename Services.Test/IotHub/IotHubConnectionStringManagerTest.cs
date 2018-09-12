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
        private readonly Mock<ILogger> logger;
        private readonly Mock<IDiagnosticsLogger> diagnosticsLogger;
        private readonly IServicesConfig config;
        private readonly Mock<IServicesConfig> mockConfig;
        private readonly Mock<IFactory> mockFactory;
        private readonly IotHubConnectionStringManager target;

        public IotHubConnectionStringManagerTest()
        {
            this.logger = new Mock<ILogger>();
            this.diagnosticsLogger = new Mock<IDiagnosticsLogger>();
            this.config = new ServicesConfig();
            this.mockConfig = new Mock<IServicesConfig>();
            this.mockFactory = new Mock<IFactory>();
            this.mockFactory.Setup(x => x.Resolve<IStorageRecords>()).Returns(new Mock<IStorageRecords>().Object);
            this.target = new IotHubConnectionStringManager(this.config, this.mockFactory.Object, this.diagnosticsLogger.Object, this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsOnInvalidConnStringFormat()
        {
            // Assert
            Assert.ThrowsAsync<InvalidIotHubConnectionStringFormatException>(
                    async () => await this.target.RedactAndStoreAsync("foobar"))
                .Wait(Constants.TEST_TIMEOUT);
        }
    }
}
