// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net;
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
        private readonly Mock<IDiagnosticsLogger> diagnosticsLogger;
        public DiagnosticsLoggerTest()
        {
            this.diagnosticsLogger = new Mock<IDiagnosticsLogger>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async void ShouldSendDiagnosticsEventsToBackEnd()
        {
            //Arrange
            IHttpResponse response = null;
            
            //Act
            response = await this.diagnosticsLogger.Object.LogDiagnosticsData("Error", "");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
