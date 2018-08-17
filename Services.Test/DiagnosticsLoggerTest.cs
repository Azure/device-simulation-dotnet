// Copyright (c) Microsoft. All rights reserved.

using System.Net;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test
{
    public class DiagnosticsLoggerTest
    {
        private readonly Mock<ILogger> logger;
        
        public DiagnosticsLoggerTest()
        {
            this.logger = new Mock<ILogger>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async void ShouldSendDiagnosticsEventsToBackEnd()
        {
            //Arrange
            var diagnosticsLogger = new DiagnosticsLogger(this.logger.Object);
            
            //Act
            var response = await diagnosticsLogger.LogDiagnosticsData("Error", "");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
