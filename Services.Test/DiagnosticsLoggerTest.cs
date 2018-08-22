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
        public void ShouldLogServiceStart()
        {
            // Arrange
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

            // Act
            IHttpResponse result = diagnosticsLogger.LogServiceStartAsync().Result;

            // Assert - Testing to see if the logic in the function is working fine. 
            // So, asserting if the expected response and actual responses are similar.
            Assert.Equal(response, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ShouldLogServiceHeartbeat()
        {
            // Arrange
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

            // Act
            IHttpResponse result = diagnosticsLogger.LogServiceHeartbeatAsync().Result;

            // Assert - Testing to see if the logic in the function is working fine. 
            // So, asserting if the expected response and actual responses are similar.
            Assert.Equal(response, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ShouldLogServiceError()
        {
            // Arrange
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

            // Act
            IHttpResponse result1 = diagnosticsLogger.LogServiceErrorAsync("testmessage").Result;
            IHttpResponse result2 = diagnosticsLogger.LogServiceErrorAsync("testmessage", new System.Exception()).Result;
            IHttpResponse result3 = diagnosticsLogger.LogServiceErrorAsync("testmessage", new { Test = "test"}).Result;

            // Assert - Testing to see if the logic in the function is working fine. 
            // So, asserting if the expected response and actual responses are similar.
            Assert.Equal(response, result1);
            Assert.Equal(response, result2);
            Assert.Equal(response, result3);
            //Assert.Equal(response, result3);
        }
    }
}
