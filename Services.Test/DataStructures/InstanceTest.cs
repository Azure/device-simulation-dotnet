// Copyright (c) Microsoft. All rights reserved.

using System;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.DataStructures;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Moq;
using Xunit;

namespace Services.Test.DataStructures
{
    public class InstanceTest
    {
        private Instance target;
        private readonly Mock<ILogger> mockLogger;

        public InstanceTest()
        {
            this.mockLogger = new Mock<ILogger>();
            this.target = new Instance(this.mockLogger.Object);
        }

        [Fact]
        public void ItThrowsIfInitOnceIsCalledAfterItIsInitialized()
        {
            // Arrange
            this.target = new Instance(this.mockLogger.Object);
            this.target.InitOnce();
            this.target.InitComplete();

            // Act, Assert
            Assert.Throws<ApplicationException>(
                () => this.target.InitOnce());
        }

        [Fact]
        public void ItDoesNotThrowIfInitOnceIsCalledBeforeItIsInitialized()
        {
            // Arrange
            this.target = new Instance(this.mockLogger.Object);

            // Act
            this.target.InitOnce();
        }

        [Fact]
        public void ItThrowsIfInitRequiredIsCalledBeforeInitialization()
        {
            // Arrange
            this.target = new Instance(this.mockLogger.Object);

            // Act, Assert
            Assert.Throws<ApplicationException>(
                () => this.target.InitRequired());
        }
    }
}
