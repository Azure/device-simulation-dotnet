// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Moq;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

namespace Services.Test
{
    public class InternalDeviceStateTest
    {
        private InternalDeviceState target;

        public InternalDeviceStateTest(ITestOutputHelper log)
        {
            // Initialize device state
            this.target = this.GetTestDeviceState();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetStateWithNoValuesIsEmpty()
        {
            // Arrange
            this.target = this.GetEmptyDeviceState();

            // Act
            var state = this.target.GetAll();

            // Assert
            Assert.Empty(state);
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
            var state = this.target.GetAll();

            // Assert
            Assert.NotEmpty(state);
            Assert.Equal(state.Count, expectedCount);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetStateValueThrowsIfKeyDoesNotExist()
        {
            // Arrange
            this.target = this.GetTestDeviceState();
            var key = "KeyThatDoesNotExist";

            // Act and Assert
            Assert.Throws<KeyNotFoundException>(() => this.target.Get(key));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetStateValueReturnsValue()
        {
            // Arrange
            this.target = this.GetTestDeviceState();
            var key = "testKey";
            var value = "testValue";

            // Act
            var result = this.target.Get(key);

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
            this.target.Set(key, value);

            // Act
            var result = this.target.Get(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result, value);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void HasStateValueReturnsTrueForKey()
        {
            // Arrange
            this.target = this.GetEmptyDeviceState();
            var key = "testHasKey";
            var value = "testHasValue";
            this.target.Set(key, value);

            // Act
            var result = this.target.Has(key);

            // Assert
            Assert.True(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void HasStateValueReturnsFalseIfNotThere()
        {
            // Arrange
            this.target = this.GetEmptyDeviceState();
            var key = "KeyThatDoesNotExist";

            // Act
            var result = this.target.Has(key);

            // Assert
            Assert.False(result);
        }

        private InternalDeviceState GetEmptyDeviceState()
        {
            return new InternalDeviceState();
        }

        private InternalDeviceState GetTestDeviceState()
        {
            return new InternalDeviceState(GetTestChillerModel());
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
