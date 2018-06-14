// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.DeviceModelApiModel;
using Newtonsoft.Json;
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
