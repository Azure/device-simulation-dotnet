// Copyright (c) Microsoft. All rights reserved.

using System.IO;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.WebService.v1.Models.Helpers;
using WebService.Test.helpers;
using Xunit;

namespace WebService.Test.v1.Models.Helpers
{
    public class IotHubConnectionStringManagerTest
    {
        private const string CONNSTRING_FILE_PATH = @"keys\user_iothub_key.txt";

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItThrowsOnInvalidConnStringFormat()
        {
            // Arrange
            if (File.Exists(CONNSTRING_FILE_PATH))
            {
                File.Delete(CONNSTRING_FILE_PATH);
            }

            // Assert
            Assert.Throws<InvalidIotHubConnectionStringFormatException>(() => IotHubConnectionStringManager.StoreAndRedact("foobar"));

            // Clean Up
            if (File.Exists(CONNSTRING_FILE_PATH))
            {
                File.Delete(CONNSTRING_FILE_PATH);
            }
        }
    }
}
