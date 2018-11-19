// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Text;
using Microsoft.Azure.Devices;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.IotHub
{
    public class ConnectionStringValidationTest
    {
        private readonly ConnectionStringValidation target;
        private readonly Mock<IFactory> factory;
        private readonly Mock<IDiagnosticsLogger> diagnosticsLogger;
        private readonly Mock<ILogger> log;

        private const string HOST = "example.azure-devices.net";
        private const string POLICY = "iothubowner";

        private string ValidKey => Convert.ToBase64String(Encoding.UTF8.GetBytes("123"));
        private string InvalidKey => Convert.ToBase64String(Encoding.UTF8.GetBytes("123")) + "x";
        private string ValidConnString => $"HostName={HOST};SharedAccessKeyName={POLICY};SharedAccessKey={this.ValidKey}";
        private string ConnStringWithInvalidKey => $"HostName={HOST};SharedAccessKeyName={POLICY};SharedAccessKey={this.InvalidKey}";
        private string ConnStringWithEmptyKey => $"HostName={HOST};SharedAccessKeyName={POLICY};SharedAccessKey=";
        private string IncompleteConnString => $"HostName={HOST};SharedAccessKeyName={POLICY}";
        private string InvalidConnString => this.ConnStringWithInvalidKey;

        public ConnectionStringValidationTest()
        {
            this.factory = new Mock<IFactory>();
            this.diagnosticsLogger = new Mock<IDiagnosticsLogger>();
            this.log = new Mock<ILogger>();
            this.target = new ConnectionStringValidation(
                this.factory.Object,
                this.diagnosticsLogger.Object,
                this.log.Object);

            // Use a real instance for better coverage
            this.factory.Setup(x => x.Resolve<IRegistryManager>())
                .Returns(new RegistryManagerWrapper(new Instance(this.log.Object)));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItRecognizeDefaultConnStrings()
        {
            // Assert - Valid values
            Assert.True(this.target.IsEmptyOrDefault(null));
            Assert.True(this.target.IsEmptyOrDefault(""));
            Assert.True(this.target.IsEmptyOrDefault(string.Empty));
            Assert.True(this.target.IsEmptyOrDefault(ServicesConfig.USE_DEFAULT_IOTHUB));
            Assert.True(this.target.IsEmptyOrDefault(" " + ServicesConfig.USE_DEFAULT_IOTHUB + " "));
            Assert.True(this.target.IsEmptyOrDefault(ServicesConfig.USE_DEFAULT_IOTHUB.ToLowerInvariant()));
            Assert.True(this.target.IsEmptyOrDefault(ServicesConfig.USE_DEFAULT_IOTHUB.ToUpperInvariant()));

            // Assert - Invalid values
            Assert.False(this.target.IsEmptyOrDefault("HostName=;SharedAccessKeyName=;SharedAccessKey="));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItParsesValidConnectionStrings()
        {
            // Act
            var result = this.target.Parse(this.ValidConnString, true);

            // Assert
            Assert.Equal(HOST, result.host);
            Assert.Equal(POLICY, result.keyName);
            Assert.Equal(this.ValidKey, result.keyValue);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsWhenParsingAnInvalidConnectionString()
        {
            // Assert - fails because it's empty
            Assert.Throws<InvalidIotHubConnectionStringFormatException>(() => this.target.Parse(string.Empty, true));
            Assert.Throws<InvalidIotHubConnectionStringFormatException>(() => this.target.Parse(null, true));

            // Assert - fails because it's not complete
            Assert.Throws<InvalidIotHubConnectionStringFormatException>(() => this.target.Parse(this.IncompleteConnString, true));

            // Assert - fails because key is empty
            Assert.Throws<InvalidIotHubConnectionStringFormatException>(() => this.target.Parse(this.ConnStringWithEmptyKey, false));

            // Assert - fails because key is not valid
            Assert.Throws<InvalidIotHubConnectionStringFormatException>(() => this.target.Parse(this.ConnStringWithInvalidKey, true));
            Assert.Throws<InvalidIotHubConnectionStringFormatException>(() => this.target.Parse(this.ConnStringWithInvalidKey, false));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItParsesConnStringsWithoutKeysInTwoModes()
        {
            // Act - Assert Ok
            var result = this.target.Parse(this.ConnStringWithEmptyKey, true);
            Assert.Equal(HOST, result.host);
            Assert.Equal(POLICY, result.keyName);
            Assert.Equal("", result.keyValue);

            // Act - Assert fail
            Assert.Throws<InvalidIotHubConnectionStringFormatException>(() => this.target.Parse(this.ConnStringWithEmptyKey, false));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntFailAndDoesntTestTheDefaultConnString()
        {
            // Act - no fail
            this.target.TestAsync(ServicesConfig.USE_DEFAULT_IOTHUB, false).CompleteOrTimeout();
            this.target.TestAsync(string.Empty, false).CompleteOrTimeout();

            // Assert
            this.factory.Verify(x => x.Resolve<IServiceClient>(), Times.Never);
            this.factory.Verify(x => x.Resolve<IRegistryManager>(), Times.Never);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsWhenTestingConnStringsWithoutKeyIfKeyRequired()
        {
            // Act - Emtpy key allowed - No exception
            this.target.TestAsync(this.ConnStringWithEmptyKey, true).CompleteOrTimeout();

            // Act - Empty key not allowed
            Assert.ThrowsAsync<InvalidIotHubConnectionStringFormatException>(
                    async () => await this.target.TestAsync(this.ConnStringWithEmptyKey, false))
                .CompleteOrTimeout();

            // Assert
            this.factory.Verify(x => x.Resolve<IServiceClient>(), Times.Never);
            this.factory.Verify(x => x.Resolve<IRegistryManager>(), Times.Never);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsWhenTestingAnInvalidConnString()
        {
            // Arrange

            // Act
            Assert.ThrowsAsync<InvalidIotHubConnectionStringFormatException>(
                    async () => await this.target.TestAsync(this.InvalidConnString, false))
                .CompleteOrTimeout();

            // Assert - The service client is not used because the conn string is not valid
            this.factory.Verify(x => x.Resolve<IServiceClient>(), Times.Never);

            // Assert - The registry manager is used once to validate the conn string format
            this.factory.Verify(x => x.Resolve<IRegistryManager>(), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsWhenTestingAConnStringWithoutServicePerms()
        {
            // Arrange
            var newServiceClient = new Mock<IServiceClient>();
            this.factory.Setup(x => x.Resolve<IServiceClient>()).Returns(newServiceClient.Object);
            newServiceClient.Setup(x => x.GetServiceStatisticsAsync()).Throws<SomeException>();

            // Act
            Assert.ThrowsAsync<IotHubConnectionException>(
                    async () => await this.target.TestAsync(this.ValidConnString, false))
                .CompleteOrTimeout();

            // Assert - The service client is used once to test the conn string
            this.factory.Verify(x => x.Resolve<IServiceClient>(), Times.Once);

            // Assert - The registry manager is used once to validate the conn string format
            this.factory.Verify(x => x.Resolve<IRegistryManager>(), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsWhenTestingAConnStringWithoutRegistryPerms()
        {
            // Arrange
            var newServiceClient = new Mock<IServiceClient>();
            this.factory.Setup(x => x.Resolve<IServiceClient>()).Returns(newServiceClient.Object);
            newServiceClient.Setup(x => x.GetServiceStatisticsAsync()).ReturnsAsync(new ServiceStatistics());

            var newRegistry = new Mock<IRegistryManager>();
            this.factory.Setup(x => x.Resolve<IRegistryManager>()).Returns(newRegistry.Object);
            newRegistry.Setup(x => x.RemoveDeviceAsync(It.IsAny<string>())).Throws<SomeException>();

            // Act
            Assert.ThrowsAsync<IotHubConnectionException>(
                    async () => await this.target.TestAsync(this.ValidConnString, false))
                .CompleteOrTimeout();

            // Assert - The service client is used once to test the conn string
            this.factory.Verify(x => x.Resolve<IServiceClient>(), Times.Once);

            // Assert - The registry manager is used once to validate the conn string format
            // and once to check the permission
            this.factory.Verify(x => x.Resolve<IRegistryManager>(), Times.Exactly(2));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntFailWhenTestingTheRegistryWithARandomNonExistingDeviceId()
        {
            // Note: the SDK has 2 DeviceNotFoundException types, we test both

            // Arrange
            var newServiceClient = new Mock<IServiceClient>();
            this.factory.Setup(x => x.Resolve<IServiceClient>()).Returns(newServiceClient.Object);
            newServiceClient.Setup(x => x.GetServiceStatisticsAsync()).ReturnsAsync(new ServiceStatistics());

            // Arrange - Exception 1
            var newRegistry = new Mock<IRegistryManager>();
            this.factory.Setup(x => x.Resolve<IRegistryManager>()).Returns(newRegistry.Object);
            newRegistry.Setup(x => x.RemoveDeviceAsync(It.IsAny<string>()))
                .Throws(new Microsoft.Azure.Devices.Client.Exceptions.DeviceNotFoundException(""));

            // Act
            this.target.TestAsync(this.ValidConnString, false).CompleteOrTimeout();

            // Assert - The registry manager is used once to validate the conn string format
            // and once to check the permission
            this.factory.Verify(x => x.Resolve<IRegistryManager>(), Times.Exactly(2));

            // Arrange - Exception 2
            this.factory.Setup(x => x.Resolve<IRegistryManager>()).Returns(newRegistry.Object);
            newRegistry.Setup(x => x.RemoveDeviceAsync(It.IsAny<string>()))
                .Throws(new Microsoft.Azure.Devices.Common.Exceptions.DeviceNotFoundException(""));

            // Act
            this.target.TestAsync(this.ValidConnString, false).CompleteOrTimeout();

            // Assert - The registry manager is used once to validate the conn string format
            // and once to check the permission
            this.factory.Verify(x => x.Resolve<IRegistryManager>(), Times.Exactly(4));
        }
    }
}
