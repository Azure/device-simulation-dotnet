// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.AzureManagementAdapter;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test
{
    public class AzureManagementAdapterTest
    {
        public AzureManagementAdapterTest()
        {
            this.httpClient = new Mock<IHttpClient>();
            this.config = new Mock<IServicesConfig>();
            this.deploymentConfig = new Mock<IDeploymentConfig>();
            this.logger = new Mock<ILogger>();
            this.diagnosticsLogger = new Mock<IDiagnosticsLogger>();

            this.target = new AzureManagementAdapter(
                this.httpClient.Object,
                this.config.Object,
                this.deploymentConfig.Object,
                this.logger.Object,
                this.diagnosticsLogger.Object);
        }

        private readonly Mock<IHttpClient> httpClient;
        private readonly Mock<IServicesConfig> config;
        private readonly Mock<IDeploymentConfig> deploymentConfig;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IDiagnosticsLogger> diagnosticsLogger;
        private readonly AzureManagementAdapter target;

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsExteneralExceptionWhenFailedFetchIothubMetrics()
        {
            // Arrange
            this.config.Setup(x => x.AzureManagementAdapterApiUrl).Returns("http://management.azure.com");
            this.config.Setup(x => x.AzureManagementAdapterApiTimeout).Returns(60000);
            this.deploymentConfig.Setup(x => x.AzureIothubName).Returns("iothub");
            this.deploymentConfig.Setup(x => x.AzureResourceGroup).Returns("RG");
            this.deploymentConfig.Setup(x => x.AzureSubscriptionId).Returns("subscriptionId");
            this.httpClient.Setup(x => x.PostAsync(It.IsAny<HttpRequest>()))
                .ReturnsAsync(new HttpResponse());

            // Act & Assert
            Assert.ThrowsAsync<ExternalDependencyException>(
                    async () => await this.target.PostAsync(It.IsAny<MetricsRequestListModel>()))
                .Wait(Constants.TEST_TIMEOUT);
        }
    }
}
