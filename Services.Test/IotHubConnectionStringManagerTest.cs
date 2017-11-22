// Copyright (c) Microsoft. All rights reserved.

using System.IO;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test
{
    public class IotHubConnectionStringManagerTest
    {
        private const string CONNSTRING_FILE_PATH = @"custom_iothub_key.txt";

        private readonly Mock<ILogger> logger;
        private readonly IServicesConfig config;
        private readonly IotHubConnectionStringManager target;

        public IotHubConnectionStringManagerTest()
        {
            this.logger = new Mock<ILogger>();
            this.config = new ServicesConfig();
            this.target = new IotHubConnectionStringManager(this.config, this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsOnInvalidConnStringFormat()
        {
            // Arrange
            if (File.Exists(CONNSTRING_FILE_PATH))
            {
                File.Delete(CONNSTRING_FILE_PATH);
            }

            // Assert
            Assert.Throws<InvalidIotHubConnectionStringFormatException>(() => this.target.StoreAndRedact("foobar"));

            // Clean Up
            if (File.Exists(CONNSTRING_FILE_PATH))
            {
                File.Delete(CONNSTRING_FILE_PATH);
            }
        }
    }
}
