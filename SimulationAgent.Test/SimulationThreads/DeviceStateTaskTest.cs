// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Concurrency;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.Services.Diagnostics;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.DeviceState;
using Microsoft.Azure.IoTSolutions.DeviceSimulation.SimulationAgent.SimulationThreads;
using Moq;
using Xunit;

namespace SimulationAgent.Test.SimulationThreads
{
    public class DeviceStateTaskTest
    {
        private const int NUM_ACTORS = 9;

        private Mock<ILogger> mockLogger;
        private Mock<IAppConcurrencyConfig> mockAppConcurrencyConfig;
        private ConcurrentDictionary<string, Mock<IDeviceStateActor>> mockDeviceStateActors;
        private ConcurrentDictionary<string, IDeviceStateActor> mockDeviceStateActorObjects;

        private DeviceStateTask target;

        public DeviceStateTaskTest()
        {
            this.mockDeviceStateActors = new ConcurrentDictionary<string, Mock<IDeviceStateActor>>();
            this.mockDeviceStateActorObjects = new ConcurrentDictionary<string, IDeviceStateActor>();

            this.mockAppConcurrencyConfig = new Mock<IAppConcurrencyConfig>();
            this.mockLogger = new Mock<ILogger>();

            this.target = new DeviceStateTask(
                this.mockAppConcurrencyConfig.Object,
                this.mockLogger.Object);
        }

        [Fact]
        public void ItCallsRunOnAllDevices()
        {
            // Arrange
            // build a list of mock device state actors
            this.BuildMockDeviceStateActors(
                this.mockDeviceStateActors,
                this.mockDeviceStateActorObjects,
                NUM_ACTORS);

            // Request cancellation so that the target will only loop once
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act
            this.target.Run(this.mockDeviceStateActorObjects, cancellationTokenSource.Token);

            // Assert
            // Verify that 'Run' was called on each actor
            foreach (var actor in this.mockDeviceStateActors)
            {
                actor.Value.Verify(x => x.Run(), Times.Once);
            }
        }

        /*
         * Because the target accepts a collection of IDeviceStateActor type, when we pass
         * this dictionary to the target, we will not be able to reference the mocks later
         * (because we're passing a collection of typed objects, not the mocks). This method
         * will generate two collections: one of mocks and one of objects. The second one will
         * be passed to the target, and later, the first one will be used to validate usage.
         */
        private void BuildMockDeviceStateActors(
            ConcurrentDictionary<string, Mock<IDeviceStateActor>> mockDictionary,
            ConcurrentDictionary<string, IDeviceStateActor> objectDictionary,
            int count)
        {
            mockDictionary.Clear();
            objectDictionary.Clear();

            for (int i = 0; i < count; i++)
            {
                var deviceName = $"device_{i}";
                var mockDeviceStateActor = new Mock<IDeviceStateActor>();
                mockDictionary.TryAdd(deviceName, mockDeviceStateActor);
                objectDictionary.TryAdd(deviceName, mockDeviceStateActor.Object);
            }
        }
    }
}
