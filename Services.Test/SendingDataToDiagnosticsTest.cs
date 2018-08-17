// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.StorageAdapter;
using Moq;
using Newtonsoft.Json;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace Services.Test
{
    public class SendingDataToDiagnosticsTest
    {
        private readonly Mock<ILogger> logger;
        
        public SendingDataToDiagnosticsTest()
        {
            this.logger = new Mock<ILogger>();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public async void SendingMesagesToDiagnostics()
        {
            //Arrange
            var sendDataToDiagnostics = new SendDataToDiagnostics(this.logger.Object);
            
            //Act
            var response = await sendDataToDiagnostics.SendDiagnosticsData("Error", "");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
