// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Exceptions;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Moq;
using Services.Test.helpers;
using Xunit;

namespace Services.Test.Runtime
{
    public class ConfigDataTest
    {
        private readonly ConfigData target;
        private readonly Mock<ILogger> logger;
        private readonly Mock<IConfigurationProvider> cfgProvider;

        public ConfigDataTest()
        {
            // Mocking ConfigurationRoot internals because of its internal static methods 
            this.cfgProvider = new Mock<IConfigurationProvider>();
            this.cfgProvider.Setup(x => x.GetReloadToken()).Returns(NullChangeToken.Singleton);
            var cfg = new ConfigurationRoot(new List<IConfigurationProvider> { this.cfgProvider.Object });

            this.logger = new Mock<ILogger>();

            this.target = new ConfigData(
                cfg,
                this.logger.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReturnsAString()
        {
            // Arrange
            this.CfgContains("foo", "bar");

            // Act
            var result = this.target.GetString("foo", "-");

            // Assert
            Assert.Equal("bar", result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReturnsAStringDefault()
        {
            // Arrange
            this.CfgDoesntContain("foo");

            // Act
            var result = this.target.GetString("foo", "defaultValue");

            // Assert
            Assert.Equal("defaultValue", result);
        }

        [Theory, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        [InlineData("true", true, false)]
        [InlineData("TRUE", true, false)]
        [InlineData("t", true, false)]
        [InlineData("T", true, false)]
        [InlineData("yes", true, false)]
        [InlineData("YES", true, false)]
        [InlineData("y", true, false)]
        [InlineData("Y", true, false)]
        [InlineData("1", true, false)]
        [InlineData("-1", true, false)]
        [InlineData("false", false, true)]
        [InlineData("FALSE", false, true)]
        [InlineData("f", false, true)]
        [InlineData("F", false, true)]
        [InlineData("no", false, true)]
        [InlineData("NO", false, true)]
        [InlineData("n", false, true)]
        [InlineData("N", false, true)]
        [InlineData("0", false, true)]
        [InlineData("", false, true)]
        public void ReturnsABoolean(string value, bool expected, bool @default)
        {
            // Arrange
            this.CfgContains("foo", value);

            // Act
            var result = this.target.GetBool("foo", @default);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReturnsABooleanDefault()
        {
            // Arrange
            this.CfgDoesntContain("foo");

            // Act
            var result1 = this.target.GetBool("foo", true);
            var result2 = this.target.GetBool("foo", false);

            // Assert
            Assert.True(result1);
            Assert.False(result2);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReturnsAnInteger()
        {
            // Arrange
            this.CfgContains("foo", "-1234");

            // Act
            var result = this.target.GetInt("foo", 888);

            // Assert
            Assert.Equal(-1234, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReturnsAnIntegerDefault()
        {
            // Arrange
            this.CfgDoesntContain("foo");

            // Act
            var result = this.target.GetInt("foo", -10);

            // Assert
            Assert.Equal(-10, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReturnsAnUnsignedInteger()
        {
            // Arrange
            this.CfgContains("foo", "1234");

            // Act
            var result = this.target.GetUInt("foo", 888);

            // Assert
            Assert.Equal((uint) 1234, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReturnsAnUnsignedIntegerDefault()
        {
            // Arrange
            this.CfgDoesntContain("foo");

            // Act
            var result = this.target.GetUInt("foo", 10);

            // Assert
            Assert.Equal((uint) 10, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReturnsAnOptionalUnsignedInteger()
        {
            // Arrange
            this.CfgContains("foo", "1234");

            // Act
            var result = this.target.GetOptionalUInt("foo");

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal((uint) 1234, result.Value);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ReturnsAnOptionalUnsignedIntegerDefault()
        {
            // Arrange
            this.CfgDoesntContain("foo");

            // Act
            var result = this.target.GetOptionalUInt("foo");

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void FailsWithInvalidIntegers()
        {
            // Arrange
            this.CfgContains("foo", "x");

            // Act + Assert
            Assert.Throws<InvalidConfigurationException>(() => this.target.GetInt("foo"));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void FailsWithInvalidBoolean()
        {
            // Arrange
            this.CfgContains("foo", "x");

            // Act + Assert
            Assert.Throws<InvalidConfigurationException>(() => this.target.GetBool("foo"));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void FailsWithInvalidUnsignedIntegers()
        {
            // Arrange
            this.CfgContains("foo", "-1");

            // Act + Assert
            Assert.Throws<InvalidConfigurationException>(() => this.target.GetUInt("foo"));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void FailsWithInvalidOptionalUnsignedIntegers()
        {
            // Arrange
            this.CfgContains("foo", "-1");

            // Act + Assert
            Assert.Throws<InvalidConfigurationException>(() => this.target.GetOptionalUInt("foo"));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ProcessesEnvVarsForStrings()
        {
            // Arrange
            this.CfgContains("foo", "${?SOMETHING}");

            // Act
            var result = this.target.GetString("foo", "default");

            // Assert
            Assert.Equal("default", result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ProcessesEnvVarsForIntegers()
        {
            // Arrange
            this.CfgContains("foo", "${?SOMETHING}");

            // Act
            var result = this.target.GetInt("foo", -123);

            // Assert
            Assert.Equal(-123, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ProcessesEnvVarsForUnsignedIntegers()
        {
            // Arrange
            this.CfgContains("foo", "${?SOMETHING}");

            // Act
            var result = this.target.GetUInt("foo", 123);

            // Assert
            Assert.Equal((uint) 123, result);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void ProcessesEnvVarsForBooleans()
        {
            // Arrange
            this.CfgContains("foo", "${?SOMETHING}");

            // Act
            var result1 = this.target.GetBool("foo", true);
            var result2 = this.target.GetBool("foo", false);

            // Assert
            Assert.True(result1);
            Assert.False(result2);
        }

        private void CfgContains(string key, string value)
        {
            this.cfgProvider.Setup(x => x.TryGet(key, out value)).Returns(true);
        }

        private void CfgDoesntContain(string foo)
        {
            var any = Guid.NewGuid().ToString();
            this.cfgProvider.Setup(x => x.TryGet(foo, out any)).Returns(false);
        }
    }
}
