﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Moq;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;
using SdkClient = Microsoft.Azure.Devices.Client.DeviceClient;

namespace Services.Test
{
    public class DevicePropertiesRequestTest
    {
        private const string DEVICE_ID = "01";
        private const string KEY1 = "Key1";
        private const string KEY2 = "Key2";
        private const string VALUE1 = "Value1";
        private const string VALUE2 = "Value2";

        private Mock<IDeviceClient> client;
        private SdkClient sdkClient;
        private Mock<ILogger> logger;

        private IDevicePropertiesRequest target;

        public DevicePropertiesRequestTest(ITestOutputHelper log)
        {
            this.sdkClient = GetSdkClient();

            this.client = new Mock<IDeviceClient>();
            this.logger = new Mock<ILogger>();

            this.target = new DevicePropertiesRequest(sdkClient, this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void SmartDictionary_Should_Update_When_DesiredPropertiesChange()
        {
            // Arrange
            const string NEW_VALUE = "new value";

            ISmartDictionary reportedProps = GetTestProperties();
            this.target.RegisterDevicePropertiesUpdateAsync(DEVICE_ID, reportedProps);

            TwinCollection desiredProps = new TwinCollection();
            desiredProps[KEY1] = NEW_VALUE;

            // Act
            this.target.OnPropertyUpdateRequested(desiredProps, null);
            var result = reportedProps.Get(KEY1);

            // Assert
            Assert.Equal(result, NEW_VALUE);
        }


        private SdkClient GetSdkClient()
        {
            var connectionString = $"HostName=somehost.azure-devices.net;DeviceId=" + DEVICE_ID + ";SharedAccessKeyName=iothubowner;SharedAccessKey=Test123+Test123456789+TestTestTestTestTest1=";

            SdkClient sdkClient = SdkClient.CreateFromConnectionString(connectionString, TransportType.Mqtt_Tcp_Only);
            sdkClient.SetRetryPolicy(new NoRetry());

            return sdkClient;
        }

        private ISmartDictionary GetTestProperties()
        {
            SmartDictionary properties = new SmartDictionary();

            properties.Set(KEY1, VALUE1);
            properties.Set(KEY2, VALUE2);

            return properties;
        }
    }
}
