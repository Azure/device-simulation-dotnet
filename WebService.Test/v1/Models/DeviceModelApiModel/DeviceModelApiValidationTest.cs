// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models
{
    public class DeviceModelApiValidationTest
    {
        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanBeSerializedAndDeserialized()
        {
            // Arrange
            var x = new DeviceModelApiValidation
            {
                Success = true,
                Messages = new List<string>
                {
                    RndText(),
                    RndText(),
                    RndText(),
                }
            };

            // Act
            var json = JsonConvert.SerializeObject(x);
            var y = JsonConvert.DeserializeObject<DeviceModelApiValidation>(json);

            // Assert
            Assert.Equal(x.Success, y.Success);
            Assert.Equal(x.Messages.Count, y.Messages.Count);

            foreach(var message in y.Messages)
            {
                Assert.Contains(message, x.Messages);
            }
        }

        private string RndText()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
