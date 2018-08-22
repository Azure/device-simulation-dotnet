// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Http;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.Diagnostics
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
            // Logging service error sending just a message string
            IHttpResponse logging_service_error_message = diagnosticsLogger.LogServiceErrorAsync("testmessage").Result;
            // Logging service error along with an exception
            IHttpResponse logging_service_error_message_and_exception = diagnosticsLogger.LogServiceErrorAsync("testmessage", new System.Exception()).Result;
            // Logging service error along with an object
            IHttpResponse logging_service_error_message_and_object = diagnosticsLogger.LogServiceErrorAsync("testmessage", new { Test = "test" }).Result;

            // Assert - Testing to see if the logic in the function is working fine. 
            // So, asserting if the expected response and actual responses are similar.
            Assert.Equal(response, logging_service_error_message);
            Assert.Equal(response, logging_service_error_message_and_exception);
            Assert.Equal(response, logging_service_error_message_and_object);
            //Assert.Equal(response, result3);
        }
    }
}
