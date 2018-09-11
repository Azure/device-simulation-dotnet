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
        private DiagnosticsLogger target;

        public DiagnosticsLoggerTest()
        {
            this.mockHttpClient = new Mock<IHttpClient>();

            this.target = new DiagnosticsLogger(
                            this.mockHttpClient.Object,
                            new ServicesConfig
                            {
                                DiagnosticsEndpointUrl = DIAGNOSTICS_SERVICE_URL
                            });
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ShouldLogServiceStart()
        {
            // Act
            target.LogServiceStartAsync("test").Wait(Constants.TEST_TIMEOUT);

            // Assert - Checking if the httpcall is made just once
            this.mockHttpClient.Verify(x => x.PostAsync(It.IsAny<HttpRequest>()), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ShouldLogServiceHeartbeat()
        {
            // Act
            target.LogServiceHeartbeatAsync().Wait(Constants.TEST_TIMEOUT);

            // Assert - Checking if the httpcall is made just once
            this.mockHttpClient.Verify(x => x.PostAsync(It.IsAny<HttpRequest>()), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ShouldLogServiceError()
        {
            // Act
            // Logging service error sending just a message string
            target.LogServiceErrorAsync("testmessage").Wait(Constants.TEST_TIMEOUT);
            // Logging service error along with an exception
            target.LogServiceErrorAsync("testmessage", new System.Exception().Message).Wait(Constants.TEST_TIMEOUT);
            // Logging service error along with an object
            target.LogServiceErrorAsync("testmessage", new { Test = "test" }).Wait(Constants.TEST_TIMEOUT);

            // Assert - Checking if the httpcall is made exactly 3 times one for each type of service error
            this.mockHttpClient.Verify(x => x.PostAsync(It.IsAny<HttpRequest>()), Times.Exactly(3));
        }
    }
}
