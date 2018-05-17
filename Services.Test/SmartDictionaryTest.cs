// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Models.DeviceModel;

namespace Services.Test
{
    public class SmartDictionaryTest
    {
        private ISmartDictionary target;

        public SmartDictionaryTest(ITestOutputHelper log)
        {
            // Initialize device properties
            this.target = this.GetTestProperties();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Should_Be_Empty_On_Get_All_When_No_Properties_Added()
        {
            // Arrange
            this.target = this.GetEmptyProperties();

            // Act
            var props = this.target.GetAll();

            // Assert
            Assert.Empty(props);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Should_Return_All_Test_Properties_On_Get_All_When_Initialized_With_Device_Model()
        {
            // Arrange
            this.target = this.GetTestProperties();
            var expectedCount = this.GetTestChillerModel().Properties.Count;

            // Act
            var props = this.target.GetAll();

            // Assert
            Assert.NotEmpty(props);
            Assert.Equal(props.Count, expectedCount);
            Assert.Equal("TestChiller", props["Type"]);
            Assert.Equal("1.0", props["Firmware"]);
            Assert.Equal("TestCH101", props["Model"]);
            Assert.Equal("TestBuilding 2", props["Location"]);
            Assert.Equal(47.640792, props["Latitude"]);
            Assert.Equal(-122.126258, props["Longitude"]);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Should_Return_Value_When_Calling_Get_And_Property_Exists()
        {
            // Arrange
            this.target = this.GetTestProperties();
            var expectedCount = this.GetTestChillerModel().Properties.Count;

            // Act
            var props = this.target.GetAll();

            // Assert
            Assert.NotEmpty(props);
            Assert.Equal(props.Count, expectedCount);
            Assert.Equal("TestChiller", props["Type"]);
            Assert.Equal("1.0", props["Firmware"]);
            Assert.Equal("TestCH101", props["Model"]);
            Assert.Equal("TestBuilding 2", props["Location"]);
            Assert.Equal(47.640792, props["Latitude"]);
            Assert.Equal(-122.126258, props["Longitude"]);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Should_Throw_On_Get_When_Key_Does_Not_Exist()
        {
            // Arrange
            this.target = this.GetTestProperties();
            const string KEY = "KeyThatDoesNotExist";

            // Act and Assert
            Assert.Throws<KeyNotFoundException>(() => this.target.Get(KEY));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Should_Return_Copy_From_Get_All()
        {
            // Arrange

            // test values
            const string KEY1 = "key1";
            const string VALUE1 = "value1";
            const string NEW_VALUE = "newvalue";

            var dictionary = new Dictionary<string,object>
            {
                { KEY1, VALUE1 }
            };

            this.target = new SmartDictionary(dictionary);

            // Act

            // modify copy
            var dictionaryCopy = this.target.GetAll();
            dictionaryCopy[KEY1] = NEW_VALUE;

            // check original
            var result = this.target.Get(KEY1);

            // Assert
            Assert.Equal(result, VALUE1);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Should_Add_Value_When_Set()
        {
            // Arrange
            this.target = this.GetEmptyProperties();
            const string KEY = "testSetKey";
            const string VALUE = "testSetValue";

            // Act
            this.target.Set(KEY, VALUE);
            var result = this.target.Get(KEY);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result, VALUE);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Should_Set_Value_With_Key()
        {
            // Arrange
            this.target = this.GetEmptyProperties();
            const string KEY = "testSetKey";
            const string VALUE = "testSetValue";
            this.target.Set(KEY, VALUE);

            // Act
            var result = this.target.Get(KEY);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result, VALUE);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Has_Should_Return_True_When_Property_Exists()
        {
            // Arrange
            this.target = this.GetEmptyProperties();
            const string KEY = "testHasKey";
            const string VALUE = "testHasValue";
            this.target.Set(KEY, VALUE);

            // Act
            var result = this.target.Has(KEY);

            // Assert
            Assert.True(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Has_Should_Return_False_When_Property_Does_Not_Exist()
        {
            // Arrange
            this.target = this.GetEmptyProperties();
            const string KEY = "KeyThatDoesNotExist";

            // Act
            var result = this.target.Has(KEY);

            // Assert
            Assert.False(result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Changed_Should_Return_True_When_New_Property_Added()
        {
            // Arrange
            this.target = this.GetEmptyProperties();
            Assert.False(this.target.Changed);

            const string KEY = "testKey";
            const string VALUE = "testValue";

            // Act
            this.target.Set(KEY, VALUE);

            // Assert
            Assert.True(this.target.Changed);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Changed_Should_Be_False_When_Reset()
        {
            // Arrange
            this.target = this.GetTestProperties();

            const string KEY = "testKey";
            const string VALUE = "testValue";
            this.target.Set(KEY, VALUE);

            // Act
            this.target.ResetChanged();

            // Assert
            Assert.False(this.target.Changed);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ItCanBeInitializedWithNullValue()
        {
            // Act
            this.target = new SmartDictionary(null);

            // Assert
            Assert.True(this.target.Changed);
        }

        private SmartDictionary GetEmptyProperties()
        {
            return new SmartDictionary();
        }

        private SmartDictionary GetTestProperties()
        {
            return new SmartDictionary(this.GetTestChillerModel().Properties);
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
