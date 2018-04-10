// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Moq;
using Services.Test.helpers;
using Xunit;
using Xunit.Abstractions;

namespace Services.Test.Concurrency
{
    public class LoopSettingsTest
    {
        private readonly ITestOutputHelper log;
        private readonly Mock<IRateLimitingConfig> rateLimitingConfig;
        private readonly PropertiesLoopSettings propertiesTarget;

        private const int TWIN_WRITES_PER_SECOND = 10;

        public LoopSettingsTest(ITestOutputHelper logger)
        {
            this.log = logger;
            this.rateLimitingConfig = new Mock<IRateLimitingConfig>();
            this.propertiesTarget = new PropertiesLoopSettings(this.rateLimitingConfig.Object);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Should_SetTaggingLimit_When_NewLoopCreated()
        {
            // Arrange
            this.SetupRateLimitingConfig();

            // Act
            this.propertiesTarget.NewLoop();

            // Assert
            // In order for other threads to be able to schedule twin opertations,
            // value should be at least 1 but less than the limit per second.
            Assert.True(this.propertiesTarget.SchedulableTaggings >= 1);
            Assert.True(this.propertiesTarget.SchedulableTaggings < TWIN_WRITES_PER_SECOND);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void Should_UseTwinWrites_When_NewLoopCreated()
        {
            // Arrange
            this.SetupRateLimitingConfig();

            // Act
            this.propertiesTarget.NewLoop();

            // Assert
            // ensure twin writes were accessed and no other
            // config values to calculate properties limits
            this.rateLimitingConfig.VerifyGet(x => x.TwinWritesPerSecond);
            this.rateLimitingConfig.VerifyNoOtherCalls();
        }

        private void SetupRateLimitingConfig()
        {
            this.rateLimitingConfig.Setup(x => x.TwinWritesPerSecond).Returns(TWIN_WRITES_PER_SECOND);
        }
    }
}
