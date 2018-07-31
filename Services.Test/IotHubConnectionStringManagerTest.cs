// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Storage;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test
{
    public class IotHubConnectionStringManagerTest
    {
        private readonly Mock<ILogger> logger;
        private readonly Mock<IFactory> factory;
        private readonly IServicesConfig config;
        private readonly IotHubConnectionStringManager target;
        private readonly Mock<IStorageRecords> mainStorage;

        public IotHubConnectionStringManagerTest()
        {
            this.mainStorage = new Mock<IStorageRecords>();
            this.logger = new Mock<ILogger>();
            this.config = new ServicesConfig();
            this.factory = new Mock<IFactory>();
            this.factory.Setup(x => x.Resolve<IStorageRecords>()).Returns(this.mainStorage.Object);

            this.target = new IotHubConnectionStringManager(this.config, this.factory.Object, this.logger.Object);
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
