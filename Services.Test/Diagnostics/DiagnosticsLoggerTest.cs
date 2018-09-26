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

        private readonly DiagnosticsLogger target;
        private readonly Mock<IHttpClient> mockHttpClient;
        private readonly Mock<ILogger> mockLogger;

        public DiagnosticsLoggerTest()
        {
            this.mockHttpClient = new Mock<IHttpClient>();
            this.mockLogger = new Mock<ILogger>();

            this.target = new DiagnosticsLogger(
                this.mockHttpClient.Object,
                new ServicesConfig
                {
                    DiagnosticsEndpointUrl = DIAGNOSTICS_SERVICE_URL
                },
                this.mockLogger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ShouldLogServiceStart()
        {
            // Act
            this.target.LogServiceStart("test");

            // Assert - Checking if the http call is made just once
            this.mockHttpClient.Verify(x => x.PostAsync(It.IsAny<HttpRequest>()), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ShouldLogServiceHeartbeat()
        {
            // Act
            this.target.LogServiceHeartbeat();

            // Assert - Checking if the http call is made just once
            this.mockHttpClient.Verify(x => x.PostAsync(It.IsAny<HttpRequest>()), Times.Once);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ShouldLogServiceError()
        {
            // Act
            // Logging service error sending just a message string
            this.target.LogServiceError("testmessage");
            
            // Logging service error along with an exception
            this.target.LogServiceError("testmessage", new System.Exception().Message);
            
            // Logging service error along with an object
            this.target.LogServiceError("testmessage", new { Test = "test" });

            // Assert - Checking if the http call is made exactly 3 times one for each type of service error
            this.mockHttpClient.Verify(x => x.PostAsync(It.IsAny<HttpRequest>()), Times.Exactly(3));
        }
    }
}
