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
        private const string MAIN = "main";
        
        private readonly IotHubConnectionStringManager target;

        private readonly Mock<IServicesConfig> config;
        private readonly Mock<IFactory> factory;
        private readonly Mock<IDiagnosticsLogger> diagnosticsLogger;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IStorageRecords> mainStorage;

        public IotHubConnectionStringManagerTest()
        {
            this.config = new Mock<IServicesConfig>();
            this.factory = new Mock<IFactory>();
            this.diagnosticsLogger = new Mock<IDiagnosticsLogger>();
            this.logger = new Mock<ILogger>();
            this.mainStorage = new Mock<IStorageRecords>();

            // Inject configuration settings with a collection name which is then used
            // to intercept the call to .Init()
            this.config.SetupGet(x => x.MainStorage)
                .Returns(new StorageConfig { DocumentDbCollection = MAIN });

            // Intercept the call to IStorageRecords.Init() and return the right storage mock
            var storageMockFactory = new Mock<IStorageRecords>();
            storageMockFactory
                .Setup(x => x.Init(It.Is<StorageConfig>(c => c.DocumentDbCollection == MAIN)))
                .Returns(this.mainStorage.Object);

            // When IStorageRecords is instantiated, return the factory above
            this.factory.Setup(x => x.Resolve<IStorageRecords>()).Returns(storageMockFactory.Object);

            this.target = new IotHubConnectionStringManager(
                this.config.Object,
                this.factory.Object,
                this.diagnosticsLogger.Object,
                this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsOnInvalidConnStringFormat()
        {
            // Assert
            Assert.ThrowsAsync<InvalidIotHubConnectionStringFormatException>(
                    async () => await this.target.RedactAndSaveAsync("foobar"))
                .CompleteOrTimeout();
        }
    }
}
