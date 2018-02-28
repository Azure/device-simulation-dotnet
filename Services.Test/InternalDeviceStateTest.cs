// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using System.Text;
using Moq;
using Newtonsoft.Json;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

namespace Services.Test
{
    public class InternalDeviceStateTest
    {
        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper log;

        private readonly Mock<ILogger> logger;

        private InternalDeviceState target;

        public InternalDeviceStateTest(ITestOutputHelper log)
        {
            this.log = log;

            this.logger = new Mock<ILogger>();

            // Initialize device state with properties and telemetry
            this.target = this.GetTestDeviceState();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetPropertiesWithNoPropertiesIsEmpty()
        {
            // Arrange
            this.target = this.GetEmptyDeviceState();

            // Act
            var props = this.target.GetProperties();

            // Assert
            Assert.Empty(props);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetStateWithNoValuesIsEmpty()
        {
            // Arrange
            this.target = this.GetEmptyDeviceState();

            // Act
            var state = this.target.GetState();

            // Assert
            Assert.Empty(state);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetPropertiesIsNotNullAndHasExpectedCount()
        {
            // Arrange
            this.target = this.GetTestDeviceState();
            var expectedCount = this.GetTestChillerModel().Properties.Count;

            // Act
            var props = this.target.GetProperties();

            // Assert
            Assert.NotEmpty(props);
            Assert.Equal(props.Count, expectedCount);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetStateIsNotNullAndHasExpectedCount()
        {
            // Arrange
            this.target = this.GetTestDeviceState();
            var initialState = this.GetTestChillerModel().Simulation.InitialState;

            // DeviceStateActor adds the CALC_TELEMETRY key and value automatically to 
            // control whether telemetry is calculated in UpdateDeviceState. Add one to expected count.
            var expectedCount = initialState.Count + 1;

            // Act
            var state = this.target.GetState();

            // Assert
            Assert.NotEmpty(state);
            Assert.Equal(state.Count, expectedCount);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetPropertyThrowsIfKeyDoesNotExist()
        {
            // Arrange
            this.target = this.GetTestDeviceState();
            var key = "KeyThatDoesNotExist";

            // Act and Assert
            Assert.Throws<KeyNotFoundException>(() => this.target.GetProperty(key));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetStateValueThrowsIfKeyDoesNotExist()
        {
            // Arrange
            this.target = this.GetTestDeviceState();
            var key = "KeyThatDoesNotExist";

            // Act and Assert
            Assert.Throws<KeyNotFoundException>(() => this.target.GetStateValue(key));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetPropertyReturnsValue()
        {
            // Arrange
            this.target = this.GetTestDeviceState();
            var key = "TestPropKey";
            var value = "TestPropValue";

            // Act
            var result = this.target.GetProperty(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result, value);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetStateValueReturnsValue()
        {
            // Arrange
            this.target = this.GetTestDeviceState();
            var key = "testKey";
            var value = "testValue";

            // Act
            var result = this.target.GetStateValue(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result, value);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void SetPropertyTest()
        {
            // Arrange
            this.target = this.GetEmptyDeviceState();
            var key = "testSetKey";
            var value = "testSetValue";

            // Act
            this.target.SetProperty(key, value);
            var result = this.target.GetProperty(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result, value);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void SetStateValueTest()
        {
            // Arrange
            this.target = this.GetEmptyDeviceState();
            var key = "testSetKey";
            var value = "testSetValue";
            this.target.SetStateValue(key, value);

            // Act
            var result = this.target.GetStateValue(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result, value);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void HasPropertyReturnsTrueForProperty()
        {
            // Arrange
            this.target = this.GetEmptyDeviceState();
            var key = "testHasKey";
            var value = "testHasValue";
            this.target.SetProperty(key, value);

            // Act
            var result = this.target.HasProperty(key);

            // Assert
            Assert.True(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void HasStateValueReturnsTrueForKey()
        {
            // Arrange
            this.target = this.GetEmptyDeviceState();
            var key = "testHasKey";
            var value = "testHasValue";
            this.target.SetStateValue(key, value);

            // Act
            var result = this.target.HasStateValue(key);

            // Assert
            Assert.True(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void HasPropertyReturnsFalseIfNotThere()
        {
            // Arrange
            this.target = this.GetEmptyDeviceState();
            var key = "KeyThatDoesNotExist";

            // Act
            var result = this.target.HasProperty(key);

            // Assert
            Assert.False(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void HasStateValueReturnsFalseIfNotThere()
        {
            // Arrange
            this.target = this.GetEmptyDeviceState();
            var key = "KeyThatDoesNotExist";

            // Act
            var result = this.target.HasStateValue(key);

            // Assert
            Assert.False(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void PropertyChangedIsSetToTrueForNewProperty()
        {
            // Arrange
            this.target = this.GetEmptyDeviceState();
            this.target.PropertyChanged = false;

            var key = "testKey";
            var value = "testValue";

            // Act
            this.target.SetProperty(key, value);

            // Assert
            Assert.True(this.target.PropertyChanged);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReturnedStateIsReadOnly()
        {
            // Arrange
            this.target = this.GetTestDeviceState();

            // Act
            var result = this.target.GetState();

            // Assert
            Assert.True(this.target.PropertyChanged);
        }

        private InternalDeviceState GetEmptyDeviceState()
        {
            return new InternalDeviceState(this.logger.Object);
        }

        private InternalDeviceState GetTestDeviceState()
        {
            return new InternalDeviceState(GetTestChillerModel(), this.logger.Object);
        }

        /// <summary>
        /// Returns the a test chiller model
        /// </summary>
        private DeviceModel GetTestChillerModel()
        {
            return new DeviceModel()
            {
                Id = "TestChiller01",
                Properties = new Dictionary<string, object>()
                {
                    { "TestPropKey", "TestPropValue" },
                    { "Type", "TestChiller" },
                    { "Firmware", "1.0" },
                    { "Model", "TestCH101" },
                    { "Location", "TestBuilding 2" },
                    { "Latitude", 47.640792 },
                    { "Longitude", -122.126258 }
                },
                Simulation = new StateSimulation()
                {
                    InitialState = new Dictionary<string, object>()
                    {
                        { "testKey", "testValue" },
                        { "online", true },
                        { "temperature", 75.0 },
                        { "temperature_unit", "F" },
                        { "humidity", 70.0 },
                        { "humidity_unit", "%" },
                        { "pressure", 150.0 },
                        { "pressure_unit", "psig" },
                        { "simulation_state", "normal_pressure" }
                    },
                    Interval = TimeSpan.Parse("00:00:10")
                }
            };
        }
    }
}
