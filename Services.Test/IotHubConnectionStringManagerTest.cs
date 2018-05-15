// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
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
        public async Task ItThrowsOnInvalidConnStringFormat()
        {
            // Assert
            await Assert.ThrowsAsync<InvalidIotHubConnectionStringFormatException>(() => this.target.RedactAndStoreAsync("foobar"));
        }
    }
}
