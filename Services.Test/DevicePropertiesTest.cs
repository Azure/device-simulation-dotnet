// Copyright (c) Microsoft. All rights reserved.

using System.Reflection;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.IotHub;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Moq;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;
using SdkClient = Microsoft.Azure.Devices.Client.DeviceClient;

namespace Services.Test
{
    public class DevicePropertiesTest
    {
        private const string DEVICE_ID = "01";
        private const string KEY1 = "Key1";
        private const string KEY2 = "Key2";
        private const string VALUE1 = "Value1";
        private const string VALUE2 = "Value2";

        private readonly IDevicePropertiesRequest target;
        private readonly Mock<IDeviceClientWrapper> sdkClient;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IServicesConfig> servicesConfig;

        public DevicePropertiesTest(ITestOutputHelper log)
        {
            this.sdkClient = new Mock<IDeviceClientWrapper>();
            this.logger = new Mock<ILogger>();
            this.servicesConfig = new Mock<IServicesConfig>();
            this.servicesConfig.SetupGet(x => x.DeviceTwinEnabled).Returns(true);

            this.target = new DeviceProperties(this.servicesConfig.Object, this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void SmartDictionary_Should_UpdateValue_When_DesiredPropertiesChange()
        {
            // Arrange
            const string NEW_VALUE = "new value";

            ISmartDictionary reportedProps = this.GetTestProperties();
            this.target.RegisterChangeUpdateAsync(this.sdkClient.Object, DEVICE_ID, reportedProps).CompleteOrTimeout();

            TwinCollection desiredProps = new TwinCollection();
            desiredProps[KEY1] = NEW_VALUE;

            // Act
            // Use reflection to invoke private callback
            MethodInfo methodInfo = this.target.GetType().GetMethod("OnChangeCallback", BindingFlags.Instance | BindingFlags.NonPublic);
            methodInfo.Invoke(this.target, new object[] { desiredProps, null });

            var result = reportedProps.Get(KEY1);

            // Assert
            Assert.Equal(result, NEW_VALUE);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void SmartDictionary_Should_HaveNewItem_When_NewDesiredPropertyAdded()
        {
            // Arrange
            const string NEW_KEY = "new key";
            const string NEW_VALUE = "new value";

            ISmartDictionary reportedProps = this.GetTestProperties();
            this.target.RegisterChangeUpdateAsync(this.sdkClient.Object, DEVICE_ID, reportedProps).CompleteOrTimeout();

            TwinCollection desiredProps = new TwinCollection();
            desiredProps[NEW_KEY] = NEW_VALUE;

            // Act
            // Use reflection to invoke private callback
            MethodInfo methodInfo = this.target.GetType().GetMethod("OnChangeCallback", BindingFlags.Instance | BindingFlags.NonPublic);
            methodInfo.Invoke(this.target, new object[] { desiredProps, null });

            var result = reportedProps.Get(NEW_KEY);

            // Assert
            Assert.Equal(result, NEW_VALUE);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void SmartDictionary_Should_Not_Update_When_DesiredPropertiesValueIsTheSame()
        {
            // Arrange
            ISmartDictionary reportedProps = this.GetTestProperties();
            reportedProps.ResetChanged();
            Assert.False(reportedProps.Changed);

            this.target.RegisterChangeUpdateAsync(this.sdkClient.Object, DEVICE_ID, reportedProps).CompleteOrTimeout();

            TwinCollection desiredProps = new TwinCollection
            {
                [KEY1] = VALUE1 // This should be the same value in props
            };

            // Act
            // Use reflection to invoke private callback
            MethodInfo methodInfo = this.target.GetType().GetMethod("OnChangeCallback", BindingFlags.Instance | BindingFlags.NonPublic);
            methodInfo.Invoke(this.target, new object[] { desiredProps, null });

            // Assert
            Assert.False(reportedProps.Changed);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItDoesntRegisterWhenTheFeatureIsDisabled()
        {
            // Arrange
            this.servicesConfig.SetupGet(x => x.DeviceTwinEnabled).Returns(false);
            var target2 = new DeviceProperties(this.servicesConfig.Object, this.logger.Object);

            // Act
            target2.RegisterChangeUpdateAsync(this.sdkClient.Object, DEVICE_ID, null).CompleteOrTimeout();

            // Assert
            this.sdkClient.Verify(x => x.SetDesiredPropertyUpdateCallbackAsync(
                It.IsAny<DesiredPropertyUpdateCallback>(), It.IsAny<object>()), Times.Never);
        }

        private ISmartDictionary GetTestProperties()
        {
            SmartDictionary properties = new SmartDictionary();

            properties.Set(KEY1, VALUE1, false);
            properties.Set(KEY2, VALUE2, false);

            return properties;
        }
    }
}
