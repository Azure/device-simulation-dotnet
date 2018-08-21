// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test
{
    public class DiagnosticsLoggerTest
    {
        private const string DIAGNOSTICS_SERVICE_URL = @"http://diagnostics";

        private readonly Mock<IHttpClient> mockHttpClient;
        
        public DiagnosticsLoggerTest()
        {
            this.mockHttpClient = new Mock<IHttpClient>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ShouldSendDiagnosticsEventsToBackEnd()
        {
            //Arrange
            var response = new HttpResponse();

            DiagnosticsLogger diagnosticsLogger = new DiagnosticsLogger(
                this.mockHttpClient.Object,
                new ServicesConfig
                {
                    DiagnosticsEndpointUrl = DIAGNOSTICS_SERVICE_URL
                });

            this.mockHttpClient
                .Setup(x => x.PostAsync(It.IsAny<IHttpRequest>()))
                .ReturnsAsync(response);

            //Act
            var result = diagnosticsLogger.LogDiagnosticsData("ServiceError", "").Result;

            //Assert
            Assert.False(result);
        }
    }
}
