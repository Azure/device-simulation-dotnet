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
    public class ConnectionStringsTest
    {
        private readonly IServicesConfig config;
        private readonly Mock<IConnectionStringValidation> connectionStringValidation;
        private readonly Mock<ILogger> mockLogger;
        private readonly Mock<IDiagnosticsLogger> mockDiagnosticsLogger;
        private readonly Mock<IStorageRecords> mainStorage;
        private readonly Mock<IFactory> mockFactory;
        private readonly ConnectionStrings target;

        public ConnectionStringsTest()
        {
            this.config = new ServicesConfig();
            this.connectionStringValidation = new Mock<IConnectionStringValidation>();
            this.mockLogger = new Mock<ILogger>();
            this.mockDiagnosticsLogger = new Mock<IDiagnosticsLogger>();
            this.mainStorage = new Mock<IStorageRecords>();
            this.mockFactory = new Mock<IFactory>();
            this.mockFactory.Setup(x => x.Resolve<IStorageRecords>()).Returns(this.mainStorage.Object);

            this.target = new ConnectionStrings(
                this.config,
                this.connectionStringValidation.Object,
                this.mockFactory.Object,
                this.mockDiagnosticsLogger.Object,
                this.mockLogger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsOnInvalidConnStringFormat()
        {
            // Arrange
            const string CS = "foobar";
            this.connectionStringValidation.Setup(x => x.IsEmptyOrDefault(CS)).Returns(false);
            this.connectionStringValidation.Setup(x => x.TestAsync(CS, true)).Throws<InvalidIotHubConnectionStringFormatException>();

            // Assert
            Assert.ThrowsAsync<InvalidIotHubConnectionStringFormatException>(
                    async () => await this.target.SaveAsync(CS))
                .CompleteOrTimeout();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsOnHubTestFailure()
        {
            // Arrange
            const string CS = "foobar";
            this.connectionStringValidation.Setup(x => x.IsEmptyOrDefault(CS)).Returns(false);
            this.connectionStringValidation.Setup(x => x.TestAsync(CS, true)).Throws<IotHubConnectionException>();

            // Assert
            Assert.ThrowsAsync<IotHubConnectionException>(
                    async () => await this.target.SaveAsync(CS))
                .CompleteOrTimeout();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsIfSecretIsMissingInCustomConnStringAndInStorage()
        {
            // Arrange
            const string CS = "foobar";
            const string SECRET = "";
            this.connectionStringValidation.Setup(x => x.IsEmptyOrDefault(CS)).Returns(false);
            this.connectionStringValidation.Setup(x => x.Parse(CS, true)).Returns(("h", "k", SECRET));
            this.mainStorage.Setup(x => x.GetAsync(It.IsAny<string>())).Throws<ResourceNotFoundException>();

            // Assert
            Assert.ThrowsAsync<IotHubConnectionException>(
                    async () => await this.target.SaveAsync(CS))
                .CompleteOrTimeout();
        }
    }
}
