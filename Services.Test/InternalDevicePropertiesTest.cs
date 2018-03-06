// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

namespace Services.Test
{
    public class InternalDevicePropertiesTest
    {
        private InternalDeviceProperties target;

        public InternalDevicePropertiesTest(ITestOutputHelper log)
        {
            // Initialize device properties
            this.target = this.GetTestProperties();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetWithNoPropertiesIsEmpty()
        {
            // Arrange
            this.target = this.GetEmptyProperties();

            // Act
            var props = this.target.GetAll();

            // Assert
            Assert.Empty(props);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetPropertiesIsNotNullAndHasExpectedCount()
        {
            // Arrange
            this.target = this.GetTestProperties();
            var expectedCount = this.GetTestChillerModel().Properties.Count;

            // Act
            var props = this.target.GetAll();

            // Assert
            Assert.NotEmpty(props);
            Assert.Equal(props.Count, expectedCount);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetPropertyThrowsIfKeyDoesNotExist()
        {
            // Arrange
            this.target = this.GetTestProperties();
            var key = "KeyThatDoesNotExist";

            // Act and Assert
            Assert.Throws<KeyNotFoundException>(() => this.target.Get(key));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void GetPropertyReturnsValue()
        {
            // Arrange
            this.target = this.GetTestProperties();
            var key = "TestPropKey";
            var value = "TestPropValue";

            // Act
            var result = this.target.Get(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result, value);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void SetPropertyTest()
        {
            // Arrange
            this.target = this.GetEmptyProperties();
            var key = "testSetKey";
            var value = "testSetValue";

            // Act
            this.target.Set(key, value);
            var result = this.target.Get(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result, value);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void SetStateValueTest()
        {
            // Arrange
            this.target = this.GetEmptyProperties();
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
        public void HasPropertyReturnsTrueForProperty()
        {
            // Arrange
            this.target = this.GetEmptyProperties();
            var key = "testHasKey";
            var value = "testHasValue";
            this.target.Set(key, value);

            // Act
            var result = this.target.Has(key);

            // Assert
            Assert.True(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void HasPropertyReturnsFalseIfNotThere()
        {
            // Arrange
            this.target = this.GetEmptyProperties();
            var key = "KeyThatDoesNotExist";

            // Act
            var result = this.target.Has(key);

            // Assert
            Assert.False(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ChangedIsSetToTrueForNewProperty()
        {
            // Arrange
            this.target = this.GetEmptyProperties();
            Assert.False(this.target.Changed);

            var key = "testKey";
            var value = "testValue";

            // Act
            this.target.Set(key, value);

            // Assert
            Assert.True(this.target.Changed);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ResetChangedIsFalse()
        {
            // Arrange
            this.target = this.GetTestProperties();

            var key = "testKey";
            var value = "testValue";

            // Act
            this.target.ResetChanged();

            // Assert
            Assert.False(this.target.Changed);
        }

        private InternalDeviceProperties GetEmptyProperties()
        {
            return new InternalDeviceProperties();
        }

        private InternalDeviceProperties GetTestProperties()
        {
            return new InternalDeviceProperties(GetTestChillerModel());
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
