// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.AzureManagementAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test
{
    public class IotHubMetricsTest
    {
        private readonly Mock<IAzureManagementAdapterClient> azureManagementAdapterClient;
        private readonly IotHubMetrics target;

        public IotHubMetricsTest()
        {
            this.azureManagementAdapterClient = new Mock<IAzureManagementAdapterClient>();
            this.target = new IotHubMetrics(this.azureManagementAdapterClient.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItInvokesAzureManagementAdapterOnceWhenQueryIothubMetrics()
        {
            // Act
            this.target.GetIothubMetricsAsync(It.IsAny<MetricsRequestListModel>())
                .Wait(Constants.TEST_TIMEOUT);

            // Assert
            this.azureManagementAdapterClient.Verify(
                x => x.PostAsync(It.IsAny<MetricsRequestListModel>()),
                Times.Once);
        }
    }
}
