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
        private readonly ServicesConfig config;
        private readonly Mock<IConnectionStringValidation> connectionStringValidation;
        private readonly Mock<ILogger> mockLogger;
        private readonly Mock<IDiagnosticsLogger> mockDiagnosticsLogger;
        private readonly Mock<IEngine> mainStorage;
        private readonly Mock<IEngines> enginesFactory;
        private readonly ConnectionStrings target;

        public ConnectionStringsTest()
        {
            this.config = new ServicesConfig();
            this.connectionStringValidation = new Mock<IConnectionStringValidation>();
            this.mockLogger = new Mock<ILogger>();
            this.mockDiagnosticsLogger = new Mock<IDiagnosticsLogger>();
            this.mainStorage = new Mock<IEngine>();
            this.enginesFactory = new Mock<IEngines>();
            this.enginesFactory.Setup(x => x.Build(It.IsAny<Config>())).Returns(this.mainStorage.Object);

            this.target = new ConnectionStrings(
                this.config,
                this.connectionStringValidation.Object,
                this.enginesFactory.Object,
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
                    async () => await this.target.SaveAsync(CS, true))
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
                    async () => await this.target.SaveAsync(CS, true))
                .CompleteOrTimeout();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItTestsTheConnectionStringOnlyIfRequested()
        {
            // Arrange
            const string CS1 = "1111";
            this.connectionStringValidation.Setup(x => x.IsEmptyOrDefault(CS1)).Returns(false);

            // Act
            this.connectionStringValidation.Invocations.Clear();
            this.target.SaveAsync(CS1, false).CompleteOrTimeout();

            // Assert
            this.connectionStringValidation.Verify(x => x.TestAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

            // Act
            this.connectionStringValidation.Invocations.Clear();
            this.target.SaveAsync(CS1, true).CompleteOrTimeout();

            // Assert
            this.connectionStringValidation.Verify(x => x.TestAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);

            // Arrange
            const string CS2 = "2222";
            this.config.IoTHubConnString = CS2;
            this.connectionStringValidation.Setup(x => x.IsEmptyOrDefault(CS2)).Returns(true);

            // Act
            this.connectionStringValidation.Invocations.Clear();
            this.target.SaveAsync(CS2, false).CompleteOrTimeout();

            // Assert
            this.connectionStringValidation.Verify(x => x.TestAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

            // Act
            this.connectionStringValidation.Invocations.Clear();
            this.target.SaveAsync(CS2, true).CompleteOrTimeout();

            // Assert
            this.connectionStringValidation.Verify(x => x.TestAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
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
                    async () => await this.target.SaveAsync(CS, true))
                .CompleteOrTimeout();
        }
    }
}
